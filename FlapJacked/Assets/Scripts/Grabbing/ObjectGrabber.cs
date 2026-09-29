using UnityEngine;
using UnityEngine.InputSystem;
public class ObjectGrabber : MonoBehaviour
{


    [Header("Grab Settings")]
    [SerializeField] private float grabDistance = 3f;
    [SerializeField] private Transform leftholdPoint;
    [SerializeField] private Transform rightholdPoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider playerCollider;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset playerControls;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string leftGrabActionName = "GrabLeft";
    [SerializeField] private string rightGrabActionName = "GrabRight";
    [SerializeField] private string leftThrowActionName = "ThrowLeft";
    [SerializeField] private string rightThrowActionName = "ThrowRight";
    [SerializeField] private string snapActionName = "Snap";
    [SerializeField] private string rotateHeldActionName = "RotateHeld";
    [SerializeField] private string lookActionName = "Look";

    [Header("Held Rotate")]
    [SerializeField] private float heldRotateDegreesPerPixel = 0.35f;

    [Header("Hand Reach")]
    [SerializeField] private float handReachPerScroll = 0.12f;
    [SerializeField] private float minHandReach;
    [SerializeField] private float maxHandReach = 1.25f;
    [SerializeField] private float dropTapTime = 0.2f;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform leftArmOrigin;
    [SerializeField] private Transform rightArmOrigin;
    [SerializeField] private float maxArmYStretch = 1.25f;

    [Header("Snap Settings")]
    [SerializeField] private float snapDistance = 3f;

    [Header("Throw Settings")]
    [SerializeField] private Vector3 throwVelocity = new Vector3(0f, 2f, 12f);
    [SerializeField] private Vector3 throwAngularVelocity = Vector3.zero;

    private InputAction leftGrabAction;
    private InputAction rightGrabAction;
    private InputAction leftThrowAction;
    private InputAction rightThrowAction;
    private InputAction snapAction;
    private InputAction rotateHeldAction;
    private InputAction lookAction;
    private bool createdSnapAction;
    private bool createdRotateHeldAction;
    private bool rotatingHeldItem;
    private bool draggingHeldRotation;
    private bool draggingLeftHeld;
    private CursorLockMode cursorLockBeforeRotate;
    private bool cursorVisibleBeforeRotate;
    private float lastHeldRotateToggleTime;

    private GameObject leftGrabbedObject;
    private Rigidbody leftGrabbedRb;
    private Collider leftGrabbedCollider;
    private bool isLeftGrabbing;
    private Vector3 leftGrabLocalPosition;
    private Quaternion leftGrabLocalRotation;

    private GameObject rightGrabbedObject;
    private Rigidbody rightGrabbedRb;
    private Collider rightGrabbedCollider;
    private bool isRightGrabbing;
    private Vector3 rightGrabLocalPosition;
    private Quaternion rightGrabLocalRotation;
    private StoveKnobInteractor stoveKnobInteractor;
    private bool leftDrivingKnob;
    private bool rightDrivingKnob;
    private Vector3 leftHoldRestLocal;
    private Vector3 rightHoldRestLocal;
    private float leftHandReach;
    private float rightHandReach;
    private bool leftManipulating;
    private bool rightManipulating;
    private float leftPressTime;
    private float rightPressTime;
    private bool leftScrolledThisPress;
    private bool rightScrolledThisPress;
    private float leftArmRestLength = 1f;
    private float rightArmRestLength = 1f;
    private Vector3 leftArmRestScale = Vector3.one;
    private Vector3 rightArmRestScale = Vector3.one;
    private Vector3 leftArmAttachLocal;
    private Vector3 rightArmAttachLocal;
    private Vector3 leftArmRestAttachParent;
    private Vector3 rightArmRestAttachParent;
    private Vector3 leftHoldOnArmLocal;
    private Vector3 rightHoldOnArmLocal;
    private bool leftHoldOnArmCached;
    private bool rightHoldOnArmCached;

    public bool IsRotatingHeldItem => rotatingHeldItem;

    private void Awake()
    {
        stoveKnobInteractor = GetComponent<StoveKnobInteractor>();
        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        if (leftholdPoint != null)
        {
            leftHoldRestLocal = leftholdPoint.localPosition;
        }
        if (rightholdPoint != null)
        {
            rightHoldRestLocal = rightholdPoint.localPosition;
        }

        CacheArmRest(leftArm, leftArmOrigin, leftholdPoint, ref leftArmRestLength, ref leftArmRestScale, ref leftArmAttachLocal, ref leftArmRestAttachParent);
        CacheArmRest(rightArm, rightArmOrigin, rightholdPoint, ref rightArmRestLength, ref rightArmRestScale, ref rightArmAttachLocal, ref rightArmRestAttachParent);
        leftHoldOnArmCached = CacheHoldOnArm(leftArm, leftholdPoint, out leftHoldOnArmLocal);
        rightHoldOnArmCached = CacheHoldOnArm(rightArm, rightholdPoint, out rightHoldOnArmLocal);
    }
    private void OnEnable()
    {
        leftGrabAction = FindGrabAction(leftGrabActionName);
        rightGrabAction = FindGrabAction(rightGrabActionName);
        leftThrowAction = FindGrabAction(leftThrowActionName);
        rightThrowAction = FindGrabAction(rightThrowActionName);
        snapAction = FindOrCreateSnapAction();
        rotateHeldAction = FindOrCreateRotateHeldAction();
        lookAction = FindGrabAction(lookActionName);
        if (leftGrabAction != null)
        {
            leftGrabAction.Enable();
            leftGrabAction.started += OnLeftGrabStarted;
            leftGrabAction.performed += OnLeftGrabPerformed;
            leftGrabAction.canceled += OnLeftGrabCanceled;
        }
        if (rightGrabAction != null)
        {
            rightGrabAction.Enable();
            rightGrabAction.started += OnRightGrabStarted;
            rightGrabAction.performed += OnRightGrabPerformed;
            rightGrabAction.canceled += OnRightGrabCanceled;
        }
        if (leftThrowAction != null)
        {
            leftThrowAction.Enable();
            leftThrowAction.performed += OnLeftThrowPerformed;
        }
        if (rightThrowAction != null)
        {
            rightThrowAction.Enable();
            rightThrowAction.performed += OnRightThrowPerformed;
        }
        if (snapAction != null)
        {
            snapAction.Enable();
            snapAction.performed += OnSnapPerformed;
        }
        if (rotateHeldAction != null)
        {
            rotateHeldAction.Enable();
            rotateHeldAction.performed += OnRotateHeldPerformed;
        }
    }
    private void OnDisable()
    {
        if (leftGrabAction != null)
        {
            leftGrabAction.started -= OnLeftGrabStarted;
            leftGrabAction.performed -= OnLeftGrabPerformed;
            leftGrabAction.canceled -= OnLeftGrabCanceled;
            leftGrabAction.Disable();
        }
        if (rightGrabAction != null)
        {
            rightGrabAction.started -= OnRightGrabStarted;
            rightGrabAction.performed -= OnRightGrabPerformed;
            rightGrabAction.canceled -= OnRightGrabCanceled;
            rightGrabAction.Disable();
        }
        if (leftThrowAction != null)
        {
            leftThrowAction.performed -= OnLeftThrowPerformed;
            leftThrowAction.Disable();
        }
        if (rightThrowAction != null)
        {
            rightThrowAction.performed -= OnRightThrowPerformed;
            rightThrowAction.Disable();
        }
        if (snapAction != null)
        {
            snapAction.performed -= OnSnapPerformed;
            snapAction.Disable();
            if (createdSnapAction)
            {
                snapAction.Dispose();
                snapAction = null;
                createdSnapAction = false;
            }
        }
        if (rotateHeldAction != null)
        {
            rotateHeldAction.performed -= OnRotateHeldPerformed;
            rotateHeldAction.Disable();
            if (createdRotateHeldAction)
            {
                rotateHeldAction.Dispose();
                rotateHeldAction = null;
                createdRotateHeldAction = false;
            }
        }
        EndHeldRotateMode();
    }
    private void OnLeftGrabStarted(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (stoveKnobInteractor != null && stoveKnobInteractor.TryBeginHold(isLeftGrabbing))
        {
            leftDrivingKnob = true;
            return;
        }

        BeginManipulate(true);
    }
    private void OnRightGrabStarted(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (stoveKnobInteractor != null && stoveKnobInteractor.TryBeginHold(isRightGrabbing))
        {
            rightDrivingKnob = true;
            return;
        }

        BeginManipulate(false);
    }
    private void OnLeftGrabPerformed(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (leftDrivingKnob || (stoveKnobInteractor != null && stoveKnobInteractor.IsHoldingKnob))
        {
            return;
        }

        if (!isLeftGrabbing)
        {
            GrabObject(true);
        }
    }
    private void OnRightGrabPerformed(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (rightDrivingKnob || (stoveKnobInteractor != null && stoveKnobInteractor.IsHoldingKnob))
        {
            return;
        }

        if (!isRightGrabbing)
        {
            GrabObject(false);
        }
    }
    private void OnLeftGrabCanceled(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (leftDrivingKnob)
        {
            leftDrivingKnob = false;
            if (stoveKnobInteractor != null)
            {
                stoveKnobInteractor.ReleaseKnob();
            }
        }

        EndManipulate(true);
    }
    private void OnRightGrabCanceled(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        if (rightDrivingKnob)
        {
            rightDrivingKnob = false;
            if (stoveKnobInteractor != null)
            {
                stoveKnobInteractor.ReleaseKnob();
            }
        }

        EndManipulate(false);
    }
    private void OnLeftThrowPerformed(InputAction.CallbackContext context)
    {
        if (isLeftGrabbing)
        {
            ThrowObject(true);
        }
    }
    private void OnRightThrowPerformed(InputAction.CallbackContext context)
    {
        if (isRightGrabbing)
        {
            ThrowObject(false);
        }
    }
    private void OnSnapPerformed(InputAction.CallbackContext context)
    {
        if (rotatingHeldItem)
        {
            return;
        }

        TrySnapHeldItem();
    }

    private void OnRotateHeldPerformed(InputAction.CallbackContext context)
    {
        ToggleHeldRotateMode();
    }
    private void GrabObject(bool leftHand)
    {
        Transform holdPoint = leftHand ? leftholdPoint : rightholdPoint;
        if (playerCamera == null || holdPoint == null)
        {
            return;
        }
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, grabDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        RaycastHit? eggHit = null;
        RaycastHit? fallbackHit = null;
        foreach (RaycastHit hit in hits)
        {
            if (playerCollider != null && hit.collider == playerCollider)
            {
                continue;
            }
            if (hit.collider.GetComponentInParent<StoveKnob>() != null)
            {
                continue;
            }
            CartonEgg hitEgg = hit.collider.GetComponentInParent<CartonEgg>();
            MixingBowl hitBowl = hit.collider.GetComponentInParent<MixingBowl>();
            bool grabbable = IsGrabbableCollider(hit.collider) || hitEgg != null || hitBowl != null;
            if (hit.collider.transform.root == transform.root && !grabbable)
            {
                continue;
            }
            if (!grabbable)
            {
                continue;
            }
            if (hitEgg != null)
            {
                eggHit = hit;
                break;
            }
            if (!fallbackHit.HasValue)
            {
                fallbackHit = hit;
            }
        }
        RaycastHit? chosenHit = eggHit ?? fallbackHit;
        if (!chosenHit.HasValue)
        {
            return;
        }
        RaycastHit grabHit = chosenHit.Value;
        CartonEgg grabbedEgg = grabHit.collider.GetComponentInParent<CartonEgg>();
        MixingBowl grabbedBowl = grabHit.collider.GetComponentInParent<MixingBowl>();
        Rigidbody targetRb;
        GameObject target;
        if (grabbedEgg != null)
        {
            grabbedEgg.Unlock();
            targetRb = grabbedEgg.EnsureRigidbody();
            target = grabbedEgg.gameObject;
        }
        else if (grabbedBowl != null)
        {
            targetRb = grabbedBowl.GetComponent<Rigidbody>();
            if (targetRb == null)
            {
                targetRb = grabbedBowl.GetComponentInParent<Rigidbody>();
            }
            target = grabbedBowl.gameObject;
        }
        else
        {
            targetRb = grabHit.collider.GetComponentInParent<Rigidbody>();
            target = targetRb != null ? targetRb.gameObject : null;
        }
        if (targetRb == null || target == null)
        {
            return;
        }
        if (target == leftGrabbedObject || target == rightGrabbedObject)
        {
            return;
        }
        if (!targetRb.isKinematic)
        {
            targetRb.linearVelocity = Vector3.zero;
            targetRb.angularVelocity = Vector3.zero;
        }
        targetRb.useGravity = false;
        targetRb.isKinematic = true;
        targetRb.interpolation = RigidbodyInterpolation.None;
        NotifySpawnedPrefabPickedUp(target);
        NotifySnappablePickedUp(target);
        targetRb.detectCollisions = false;
        IgnorePlayerCollisions(target, true);
        Transform grabAnchor = FindEmptyGrabAnchor(target.transform, grabHit.collider.transform, leftHand);
        bool matchHoldRotation = ShouldMatchHoldRotation(grabAnchor);
        if (!matchHoldRotation)
        {
            SnapItemUpright(target.transform, holdPoint);
        }
        GetGrabOffset(target.transform, grabAnchor, holdPoint, matchHoldRotation, out Vector3 localPosition, out Quaternion localRotation);
        target.transform.SetParent(holdPoint, true);
        AlignObjectToHoldPoint(target.transform, holdPoint, localPosition, localRotation);
        if (leftHand)
        {
            leftGrabbedObject = target;
            leftGrabbedRb = targetRb;
            leftGrabbedCollider = grabHit.collider;
            leftGrabLocalPosition = localPosition;
            leftGrabLocalRotation = localRotation;
            isLeftGrabbing = true;
        }
        else
        {
            rightGrabbedObject = target;
            rightGrabbedRb = targetRb;
            rightGrabbedCollider = grabHit.collider;
            rightGrabLocalPosition = localPosition;
            rightGrabLocalRotation = localRotation;
            isRightGrabbing = true;
        }
    }

    public void NotifyHeldObjectDestroyed(GameObject destroyedObject)
    {
        if (destroyedObject == null)
        {
            return;
        }

        if (destroyedObject == leftGrabbedObject)
        {
            ClearGrabState(true);
        }

        if (destroyedObject == rightGrabbedObject)
        {
            ClearGrabState(false);
        }
    }

    private void ClearMissingGrabs()
    {
        if (isLeftGrabbing && leftGrabbedObject == null)
        {
            ClearGrabState(true);
        }

        if (isRightGrabbing && rightGrabbedObject == null)
        {
            ClearGrabState(false);
        }
    }

    private void ClearGrabState(bool leftHand)
    {
        if (leftHand)
        {
            leftGrabbedObject = null;
            leftGrabbedRb = null;
            leftGrabbedCollider = null;
            isLeftGrabbing = false;
            leftManipulating = false;
        }
        else
        {
            rightGrabbedObject = null;
            rightGrabbedRb = null;
            rightGrabbedCollider = null;
            isRightGrabbing = false;
            rightManipulating = false;
        }

        if (!isLeftGrabbing && !isRightGrabbing)
        {
            EndHeldRotateMode();
        }
    }

    private static bool IsGrabbableCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        Transform current = hitCollider.transform;
        while (current != null)
        {
            if (current.CompareTag("Grabbable"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void NotifySpawnedPrefabPickedUp(GameObject target)
    {
        SpawnedPrefab spawned = target.GetComponent<SpawnedPrefab>();
        if (spawned == null)
        {
            spawned = target.GetComponentInParent<SpawnedPrefab>();
        }
        if (spawned == null)
        {
            spawned = target.GetComponentInChildren<SpawnedPrefab>(true);
        }
        if (spawned != null)
        {
            spawned.NotifyPickedUp();
        }
    }

    private static void NotifySnappablePickedUp(GameObject target)
    {
        SnappableItem item = target.GetComponentInParent<SnappableItem>();
        if (item == null)
        {
            item = target.GetComponentInChildren<SnappableItem>(true);
        }
        if (item != null)
        {
            item.LeaveZone();
        }
    }

    private void IgnorePlayerCollisions(GameObject root, bool ignore)
    {
        if (playerCollider == null || root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider other = colliders[i];
            if (other == null || other == playerCollider)
            {
                continue;
            }

            Physics.IgnoreCollision(playerCollider, other, ignore);
        }
    }

    private void TrySnapHeldItem()
    {
        if (!isLeftGrabbing && !isRightGrabbing)
        {
            return;
        }

        SnapZone zone = FindLookedAtSnapZone();
        if (zone != null)
        {
            if (isRightGrabbing && TryPlaceHeldItem(false, zone))
            {
                return;
            }
            if (isLeftGrabbing && TryPlaceHeldItem(true, zone))
            {
                return;
            }
        }

        if (isRightGrabbing && TryPlaceHeldItem(false, FindNearestSnapZone(rightGrabbedObject)))
        {
            return;
        }
        if (isLeftGrabbing)
        {
            TryPlaceHeldItem(true, FindNearestSnapZone(leftGrabbedObject));
        }
    }

    private bool TryPlaceHeldItem(bool leftHand, SnapZone zone)
    {
        GameObject grabbedObject = leftHand ? leftGrabbedObject : rightGrabbedObject;
        if (zone == null || grabbedObject == null)
        {
            return false;
        }

        SnappableItem item = grabbedObject.GetComponentInParent<SnappableItem>();
        if (item == null)
        {
            item = grabbedObject.GetComponentInChildren<SnappableItem>(true);
        }
        if (item == null || !zone.CanAccept(item))
        {
            return false;
        }

        Vector3 preferPosition = grabbedObject.transform.position;
        if (playerCamera != null)
        {
            preferPosition = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
        }

        SnapAnchor slot = zone.FindEmptySlot(preferPosition);
        PlaceHeldItem(leftHand, item, zone, slot);
        return true;
    }

    private void PlaceHeldItem(bool leftHand, SnappableItem item, SnapZone zone)
    {
        Vector3 preferPosition = item.transform.position;
        PlaceHeldItem(leftHand, item, zone, zone != null ? zone.FindEmptySlot(preferPosition) : null);
    }

    private void PlaceHeldItem(bool leftHand, SnappableItem item, SnapZone zone, SnapAnchor slot)
    {
        GameObject grabbedObject = leftHand ? leftGrabbedObject : rightGrabbedObject;
        Rigidbody grabbedRb = leftHand ? leftGrabbedRb : rightGrabbedRb;
        Collider grabbedCollider = leftHand ? leftGrabbedCollider : rightGrabbedCollider;
        if (grabbedObject == null || grabbedRb == null || zone == null)
        {
            return;
        }

        if (!item.AttachToZone(zone, slot))
        {
            return;
        }

        grabbedObject.transform.SetParent(null, true);
        IgnorePlayerCollisions(grabbedObject, false);

        grabbedRb.detectCollisions = true;
        grabbedRb.linearVelocity = Vector3.zero;
        grabbedRb.angularVelocity = Vector3.zero;
        grabbedRb.useGravity = false;
        grabbedRb.isKinematic = true;
        grabbedRb.interpolation = RigidbodyInterpolation.None;

        Transform placeTarget = zone.GetPlaceTarget(slot);
        Transform itemAnchor = item.GetSnapAnchor(zone);
        GetGrabOffset(item.transform, itemAnchor, out Vector3 localPosition, out Quaternion localRotation);
        AlignObjectToHoldPoint(item.transform, placeTarget, localPosition, localRotation);

        if (zone.ParentPlacedItem)
        {
            grabbedObject.transform.SetParent(placeTarget, true);
        }

        if (leftHand)
        {
            leftGrabbedObject = null;
            leftGrabbedRb = null;
            leftGrabbedCollider = null;
            isLeftGrabbing = false;
        }
        else
        {
            rightGrabbedObject = null;
            rightGrabbedRb = null;
            rightGrabbedCollider = null;
            isRightGrabbing = false;
        }

        if (!isLeftGrabbing && !isRightGrabbing)
        {
            EndHeldRotateMode();
        }
    }

    private SnapZone FindLookedAtSnapZone()
    {
        if (playerCamera == null)
        {
            return null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, snapDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (playerCollider != null && hit.collider == playerCollider)
            {
                continue;
            }

            SnapZone zone = hit.collider.GetComponentInParent<SnapZone>();
            if (zone != null && zone.HasEmptySlot())
            {
                return zone;
            }
        }

        return null;
    }

    private SnapZone FindNearestSnapZone(GameObject grabbedObject)
    {
        if (grabbedObject == null || playerCamera == null)
        {
            return null;
        }

        SnappableItem item = grabbedObject.GetComponentInParent<SnappableItem>();
        if (item == null)
        {
            item = grabbedObject.GetComponentInChildren<SnappableItem>(true);
        }
        if (item == null)
        {
            return null;
        }

        SnapZone[] zones = FindObjectsByType<SnapZone>(FindObjectsSortMode.None);
        SnapZone best = null;
        float bestScore = float.MaxValue;
        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;
        for (int i = 0; i < zones.Length; i++)
        {
            SnapZone zone = zones[i];
            if (!zone.CanAccept(item))
            {
                continue;
            }

            SnapAnchor slot = zone.FindEmptySlot(origin + forward);
            Vector3 targetPosition = slot != null ? slot.transform.position : zone.SnapTarget.position;
            Vector3 toZone = targetPosition - origin;
            float distance = toZone.magnitude;
            if (distance > snapDistance || distance < 0.01f)
            {
                continue;
            }

            float facing = Vector3.Dot(forward, toZone / distance);
            if (facing < 0.35f)
            {
                continue;
            }

            float score = distance + (1f - facing);
            if (score < bestScore)
            {
                bestScore = score;
                best = zone;
            }
        }

        return best;
    }
    private void Update()
    {
        ClearMissingGrabs();
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleHeldRotateMode();
        }

        UpdateHeldRotationDrag();
        UpdateHandReachFromScroll();
    }

    private void LateUpdate()
    {
        StretchArm(leftArm, leftArmOrigin, leftArmAttachLocal, leftArmRestAttachParent, leftArmRestLength, leftArmRestScale, leftHandReach, maxArmYStretch);
        StretchArm(rightArm, rightArmOrigin, rightArmAttachLocal, rightArmRestAttachParent, rightArmRestLength, rightArmRestScale, rightHandReach, maxArmYStretch);
        AttachHoldToArm(leftholdPoint, leftArm, leftHoldOnArmCached, leftHoldOnArmLocal, leftHoldRestLocal, leftHandReach);
        AttachHoldToArm(rightholdPoint, rightArm, rightHoldOnArmCached, rightHoldOnArmLocal, rightHoldRestLocal, rightHandReach);

        if (isLeftGrabbing && leftGrabbedObject != null && leftholdPoint != null)
        {
            AlignObjectToHoldPoint(leftGrabbedObject.transform, leftholdPoint, leftGrabLocalPosition, leftGrabLocalRotation);
        }
        if (isRightGrabbing && rightGrabbedObject != null && rightholdPoint != null)
        {
            AlignObjectToHoldPoint(rightGrabbedObject.transform, rightholdPoint, rightGrabLocalPosition, rightGrabLocalRotation);
        }
    }

    private void UpdateHandReachFromScroll()
    {
        if (rotatingHeldItem)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || playerCamera == null)
        {
            return;
        }

        bool leftHeld = mouse.leftButton.isPressed;
        bool rightHeld = mouse.rightButton.isPressed;
        if (!leftHeld && !rightHeld)
        {
            return;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        float step = Mathf.Sign(scroll) * handReachPerScroll;
        if (leftHeld)
        {
            leftHandReach = Mathf.Clamp(leftHandReach + step, minHandReach, maxHandReach);
            if (isLeftGrabbing)
            {
                leftScrolledThisPress = true;
            }
        }
        if (rightHeld)
        {
            rightHandReach = Mathf.Clamp(rightHandReach + step, minHandReach, maxHandReach);
            if (isRightGrabbing)
            {
                rightScrolledThisPress = true;
            }
        }
    }

    private void BeginManipulate(bool leftHand)
    {
        if (leftHand)
        {
            if (!isLeftGrabbing)
            {
                return;
            }

            leftManipulating = true;
            leftPressTime = Time.time;
            leftScrolledThisPress = false;
            return;
        }

        if (!isRightGrabbing)
        {
            return;
        }

        rightManipulating = true;
        rightPressTime = Time.time;
        rightScrolledThisPress = false;
    }

    private void EndManipulate(bool leftHand)
    {
        bool manipulating = leftHand ? leftManipulating : rightManipulating;
        bool grabbing = leftHand ? isLeftGrabbing : isRightGrabbing;
        float pressTime = leftHand ? leftPressTime : rightPressTime;
        bool scrolled = leftHand ? leftScrolledThisPress : rightScrolledThisPress;
        if (leftHand)
        {
            leftManipulating = false;
        }
        else
        {
            rightManipulating = false;
        }

        if (!manipulating || !grabbing || scrolled)
        {
            return;
        }

        if (Time.time - pressTime <= dropTapTime)
        {
            ReleaseObject(leftHand);
        }
    }

    private static void CacheArmRest(Transform arm, Transform origin, Transform holdPoint, ref float restLength, ref Vector3 restScale, ref Vector3 attachLocal, ref Vector3 restAttachParent)
    {
        if (arm == null)
        {
            return;
        }

        restScale = arm.localScale;
        Transform start = origin != null && !origin.IsChildOf(arm) ? origin : arm;
        Transform end = holdPoint != null ? holdPoint : arm;
        restLength = Vector3.Distance(start.position, end.position);
        if (restLength < 0.01f)
        {
            restLength = 1f;
        }

        if (origin != null && !origin.IsChildOf(arm))
        {
            attachLocal = arm.InverseTransformPoint(origin.position);
        }
        else
        {
            MeshFilter filter = arm.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = arm.GetComponentInChildren<MeshFilter>();
            }
            if (filter != null && filter.sharedMesh != null)
            {
                Vector3 meshAttach = new Vector3(filter.sharedMesh.bounds.min.x, filter.sharedMesh.bounds.center.y, filter.sharedMesh.bounds.center.z);
                attachLocal = arm.InverseTransformPoint(filter.transform.TransformPoint(meshAttach));
            }
            else
            {
                attachLocal = Vector3.zero;
            }
        }

        if (arm.parent != null)
        {
            restAttachParent = arm.parent.InverseTransformPoint(arm.TransformPoint(attachLocal));
        }
        else
        {
            restAttachParent = arm.TransformPoint(attachLocal);
        }
    }

    private static void StretchArm(Transform arm, Transform origin, Vector3 attachLocal, Vector3 restAttachParent, float restLength, Vector3 restScale, float reach, float maxYStretch)
    {
        if (arm == null)
        {
            return;
        }

        float stretch = 1f + Mathf.Max(0f, reach) / Mathf.Max(0.01f, restLength);
        float yStretch = Mathf.Clamp(stretch, 1f, Mathf.Max(1f, maxYStretch));
        arm.localScale = new Vector3(restScale.x, restScale.y * yStretch, restScale.z);

        Vector3 desiredAttach = origin != null && !origin.IsChildOf(arm)
            ? origin.position
            : (arm.parent != null ? arm.parent.TransformPoint(restAttachParent) : restAttachParent);
        Vector3 currentAttach = arm.TransformPoint(attachLocal);
        arm.position += desiredAttach - currentAttach;
    }

    private static bool CacheHoldOnArm(Transform arm, Transform holdPoint, out Vector3 holdOnArmLocal)
    {
        holdOnArmLocal = Vector3.zero;
        if (arm == null || holdPoint == null)
        {
            return false;
        }

        holdOnArmLocal = arm.InverseTransformPoint(holdPoint.position);
        return true;
    }

    private void AttachHoldToArm(Transform holdPoint, Transform arm, bool holdOnArmCached, Vector3 holdOnArmLocal, Vector3 restLocal, float reach)
    {
        if (holdPoint == null)
        {
            return;
        }

        if (arm != null && holdOnArmCached)
        {
            holdPoint.position = arm.TransformPoint(holdOnArmLocal);
            return;
        }

        ApplyHandReach(holdPoint, restLocal, reach);
    }

    private void ApplyHandReach(Transform holdPoint, Vector3 restLocal, float reach)
    {
        if (holdPoint == null || playerCamera == null)
        {
            return;
        }

        Vector3 restWorld = holdPoint.parent != null
            ? holdPoint.parent.TransformPoint(restLocal)
            : restLocal;
        holdPoint.position = restWorld + playerCamera.transform.forward * reach;
    }

    private static Transform FindEmptyGrabAnchor(Transform target, Transform hitTransform, bool leftHand)
    {
        Transform marker = FindHeldGrabPointMarker(target, hitTransform, leftHand);
        if (marker != null)
        {
            return marker;
        }

        Transform named = FindNamedGrabAnchor(target, hitTransform, leftHand);
        if (named != null)
        {
            return named;
        }

        return FindBestEmptyChild(target, hitTransform);
    }

    private static Transform FindHeldGrabPointMarker(Transform target, Transform hitTransform, bool leftHand)
    {
        Transform match = null;
        Transform any = null;
        HeldGrabPoint[] markers = target.GetComponentsInChildren<HeldGrabPoint>(true);
        foreach (HeldGrabPoint marker in markers)
        {
            if (IsHandMatch(marker, leftHand))
            {
                return marker.transform;
            }
            if (marker.GrabHand == HeldGrabPoint.Hand.Any && any == null)
            {
                any = marker.transform;
            }
        }

        if (hitTransform != null && hitTransform != target)
        {
            markers = hitTransform.GetComponentsInChildren<HeldGrabPoint>(true);
            foreach (HeldGrabPoint marker in markers)
            {
                if (IsHandMatch(marker, leftHand))
                {
                    return marker.transform;
                }
                if (marker.GrabHand == HeldGrabPoint.Hand.Any && any == null)
                {
                    any = marker.transform;
                }
            }
        }

        match = FindSiblingHeldGrabPoint(target, leftHand, out Transform siblingAny);
        if (match != null)
        {
            return match;
        }
        return any != null ? any : siblingAny;
    }

    private static Transform FindSiblingHeldGrabPoint(Transform target, bool leftHand, out Transform any)
    {
        any = null;
        Transform parent = target.parent;
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == target)
            {
                continue;
            }

            HeldGrabPoint marker = sibling.GetComponent<HeldGrabPoint>();
            if (marker == null)
            {
                continue;
            }
            if (IsHandMatch(marker, leftHand))
            {
                return sibling;
            }
            if (marker.GrabHand == HeldGrabPoint.Hand.Any && any == null)
            {
                any = sibling;
            }
        }
        return null;
    }

    private static bool IsHandMatch(HeldGrabPoint marker, bool leftHand)
    {
        return leftHand
            ? marker.GrabHand == HeldGrabPoint.Hand.Left
            : marker.GrabHand == HeldGrabPoint.Hand.Right;
    }

    private static Transform FindNamedGrabAnchor(Transform target, Transform hitTransform, bool leftHand)
    {
        Transform named = FindNamedGrabAnchorUnder(target, leftHand);
        if (named != null)
        {
            return named;
        }
        if (hitTransform != null && hitTransform != target)
        {
            named = FindNamedGrabAnchorUnder(hitTransform, leftHand);
            if (named != null)
            {
                return named;
            }
        }
        return FindNamedSibling(target, leftHand);
    }

    private static Transform FindNamedGrabAnchorUnder(Transform root, bool leftHand)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        Transform fallback = null;
        foreach (Transform child in children)
        {
            if (child == root)
            {
                continue;
            }

            if (!TryParseGrabName(child.name, out bool isLeft, out bool isRight, out bool isGeneric))
            {
                continue;
            }
            if (leftHand && isLeft)
            {
                return child;
            }
            if (!leftHand && isRight)
            {
                return child;
            }
            if (isGeneric && fallback == null)
            {
                fallback = child;
            }
        }
        return fallback;
    }

    private static Transform FindNamedSibling(Transform target, bool leftHand)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            return null;
        }

        Transform fallback = null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == target)
            {
                continue;
            }
            if (!TryParseGrabName(sibling.name, out bool isLeft, out bool isRight, out bool isGeneric))
            {
                continue;
            }
            if (leftHand && isLeft)
            {
                return sibling;
            }
            if (!leftHand && isRight)
            {
                return sibling;
            }
            if (isGeneric && fallback == null)
            {
                fallback = sibling;
            }
        }
        return fallback;
    }

    private static bool TryParseGrabName(string objectName, out bool isLeft, out bool isRight, out bool isGeneric)
    {
        string name = objectName.ToLowerInvariant();
        isLeft = name.Contains("left");
        isRight = name.Contains("right");
        isGeneric = name.Contains("hold") || name.Contains("grab") || name.Contains("hand")
            || name.Contains("grip") || name.Contains("handle") || name.Contains("pickup");
        return isLeft || isRight || isGeneric;
    }

    private static Transform FindBestEmptyChild(Transform target, Transform hitTransform)
    {
        Transform best = null;
        float bestDistance = -1f;
        ConsiderEmptyChildren(target, target, ref best, ref bestDistance);
        if (hitTransform != null && hitTransform != target)
        {
            ConsiderEmptyChildren(hitTransform, target, ref best, ref bestDistance);
        }

        Transform parent = target.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling == target)
                {
                    continue;
                }
                ConsiderEmpty(sibling, target, ref best, ref bestDistance);
            }
        }
        return best;
    }

    private static void ConsiderEmptyChildren(Transform root, Transform target, ref Transform best, ref float bestDistance)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            ConsiderEmpty(root.GetChild(i), target, ref best, ref bestDistance);
        }
    }

    private static void ConsiderEmpty(Transform candidate, Transform target, ref Transform best, ref float bestDistance)
    {
        if (!IsEmptyGrabChild(candidate))
        {
            return;
        }

        float distance = (candidate.position - target.position).sqrMagnitude;
        if (best == null || distance > bestDistance)
        {
            best = candidate;
            bestDistance = distance;
        }
    }

    private static bool IsEmptyGrabChild(Transform child)
    {
        return child.GetComponent<Renderer>() == null
            && child.GetComponent<Collider>() == null
            && child.GetComponent<Rigidbody>() == null
            && child.GetComponent<MeshFilter>() == null;
    }

    private static bool ShouldMatchHoldRotation(Transform grabAnchor)
    {
        if (grabAnchor == null)
        {
            return false;
        }

        HeldGrabPoint grabPoint = grabAnchor.GetComponent<HeldGrabPoint>();
        return grabPoint != null && grabPoint.MatchHoldRotation;
    }

    private static void SnapItemUpright(Transform objectTransform, Transform holdPoint)
    {
        Vector3 forward = Vector3.ProjectOnPlane(objectTransform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f && holdPoint != null)
        {
            forward = Vector3.ProjectOnPlane(holdPoint.forward, Vector3.up);
        }
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        objectTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private static void GetGrabOffset(Transform objectTransform, Transform grabAnchor, out Vector3 localPosition, out Quaternion localRotation)
    {
        GetGrabOffset(objectTransform, grabAnchor, null, true, out localPosition, out localRotation);
    }

    private static void GetGrabOffset(Transform objectTransform, Transform grabAnchor, Transform holdPoint, bool matchAnchorRotation, out Vector3 localPosition, out Quaternion localRotation)
    {
        Vector3 anchorWorld = grabAnchor != null ? grabAnchor.position : objectTransform.position;
        localPosition = objectTransform.InverseTransformPoint(anchorWorld);

        if (matchAnchorRotation && grabAnchor != null)
        {
            localRotation = Quaternion.Inverse(objectTransform.rotation) * grabAnchor.rotation;
            return;
        }

        if (holdPoint != null)
        {
            localRotation = Quaternion.Inverse(objectTransform.rotation) * holdPoint.rotation;
            return;
        }

        localRotation = Quaternion.identity;
    }

    private static void AlignObjectToHoldPoint(Transform objectTransform, Transform holdPoint, Vector3 localAnchorPosition, Quaternion localAnchorRotation)
    {
        Quaternion targetRotation = holdPoint.rotation * Quaternion.Inverse(localAnchorRotation);
        Vector3 targetPosition = holdPoint.position - targetRotation * localAnchorPosition;
        objectTransform.SetPositionAndRotation(targetPosition, targetRotation);
    }
    private void ReleaseObject(bool leftHand)
    {
        GameObject grabbedObject = leftHand ? leftGrabbedObject : rightGrabbedObject;
        Rigidbody grabbedRb = leftHand ? leftGrabbedRb : rightGrabbedRb;
        Collider grabbedCollider = leftHand ? leftGrabbedCollider : rightGrabbedCollider;
        if (grabbedObject == null)
        {
            return;
        }
        grabbedObject.transform.SetParent(null, true);
        IgnorePlayerCollisions(grabbedObject, false);
        grabbedRb.detectCollisions = true;
        MixingBowl bowl = grabbedObject.GetComponent<MixingBowl>();
        if (bowl != null)
        {
            bowl.EnableDroppedPhysics();
        }
        else
        {
            grabbedRb.isKinematic = false;
            grabbedRb.useGravity = true;
            grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;
            grabbedRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        if (leftHand)
        {
            leftGrabbedObject = null;
            leftGrabbedRb = null;
            leftGrabbedCollider = null;
            isLeftGrabbing = false;
        }
        else
        {
            rightGrabbedObject = null;
            rightGrabbedRb = null;
            rightGrabbedCollider = null;
            isRightGrabbing = false;
        }

        if (!isLeftGrabbing && !isRightGrabbing)
        {
            EndHeldRotateMode();
        }
    }
    private void ThrowObject(bool leftHand)
    {
        bool isGrabbing = leftHand ? isLeftGrabbing : isRightGrabbing;
        Rigidbody grabbedRb = leftHand ? leftGrabbedRb : rightGrabbedRb;
        if (!isGrabbing || grabbedRb == null)
        {
            return;
        }

        ReleaseObject(leftHand);

        MixingBowl bowl = grabbedRb.GetComponent<MixingBowl>();
        if (bowl != null)
        {
            return;
        }

        Vector3 linear = throwVelocity;
        Vector3 angular = throwAngularVelocity;
        if (playerCamera != null)
        {
            linear = playerCamera.transform.TransformDirection(throwVelocity);
            angular = playerCamera.transform.TransformDirection(throwAngularVelocity);
        }

        grabbedRb.linearVelocity = linear;
        grabbedRb.angularVelocity = angular;
    }
    private InputAction FindOrCreateSnapAction()
    {
        InputAction action = FindGrabAction(snapActionName);
        if (action != null)
        {
            return action;
        }

        action = new InputAction(snapActionName, InputActionType.Button);
        action.AddBinding("<Keyboard>/f");
        createdSnapAction = true;
        return action;
    }

    private InputAction FindOrCreateRotateHeldAction()
    {
        InputAction action = FindGrabAction(rotateHeldActionName);
        if (action != null)
        {
            return action;
        }

        action = new InputAction(rotateHeldActionName, InputActionType.Button);
        action.AddBinding("<Keyboard>/r");
        createdRotateHeldAction = true;
        return action;
    }

    private void ToggleHeldRotateMode()
    {
        if (Time.unscaledTime - lastHeldRotateToggleTime < 0.2f)
        {
            return;
        }

        lastHeldRotateToggleTime = Time.unscaledTime;
        if (rotatingHeldItem)
        {
            EndHeldRotateMode();
            return;
        }

        if (!isLeftGrabbing && !isRightGrabbing)
        {
            return;
        }

        BeginHeldRotateMode();
    }

    private void BeginHeldRotateMode()
    {
        if (rotatingHeldItem)
        {
            return;
        }

        rotatingHeldItem = true;
        draggingHeldRotation = false;
        cursorLockBeforeRotate = Cursor.lockState;
        cursorVisibleBeforeRotate = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (lookAction != null)
        {
            lookAction.Disable();
        }
    }

    private void EndHeldRotateMode()
    {
        if (!rotatingHeldItem)
        {
            return;
        }

        rotatingHeldItem = false;
        draggingHeldRotation = false;
        Cursor.lockState = cursorLockBeforeRotate;
        Cursor.visible = cursorVisibleBeforeRotate;
        if (lookAction != null && playerControls != null)
        {
            lookAction.Enable();
        }
    }

    private void UpdateHeldRotationDrag()
    {
        if (!rotatingHeldItem)
        {
            return;
        }

        if (!isLeftGrabbing && !isRightGrabbing)
        {
            EndHeldRotateMode();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || playerCamera == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            draggingHeldRotation = TryBeginHeldRotationDrag(mouse);
        }

        if (!mouse.leftButton.isPressed)
        {
            draggingHeldRotation = false;
            return;
        }

        if (!draggingHeldRotation)
        {
            return;
        }

        if (draggingLeftHeld && (!isLeftGrabbing || leftGrabbedObject == null))
        {
            draggingHeldRotation = false;
            return;
        }

        if (!draggingLeftHeld && (!isRightGrabbing || rightGrabbedObject == null))
        {
            draggingHeldRotation = false;
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude < 0.0001f)
        {
            return;
        }

        RotateHeldItem(draggingLeftHeld, delta);
    }

    private bool TryBeginHeldRotationDrag(Mouse mouse)
    {
        Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
        float maxDistance = grabDistance + maxHandReach + 2f;
        float bestDistance = maxDistance;
        int choice = 0;

        if (isLeftGrabbing && TryRaycastHeldObject(leftGrabbedObject, ray, maxDistance, out float leftDistance) && leftDistance <= bestDistance)
        {
            bestDistance = leftDistance;
            choice = 1;
        }

        if (isRightGrabbing && TryRaycastHeldObject(rightGrabbedObject, ray, maxDistance, out float rightDistance) && rightDistance < bestDistance)
        {
            bestDistance = rightDistance;
            choice = 2;
        }

        if (choice == 0)
        {
            return false;
        }

        draggingLeftHeld = choice == 1;
        return true;
    }

    private static bool TryRaycastHeldObject(GameObject root, Ray ray, float maxDistance, out float distance)
    {
        distance = maxDistance;
        if (root == null)
        {
            return false;
        }

        bool hitAny = false;
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled)
            {
                continue;
            }

            if (col.Raycast(ray, out RaycastHit hit, maxDistance) && hit.distance <= distance)
            {
                distance = hit.distance;
                hitAny = true;
            }
        }

        return hitAny;
    }

    private void RotateHeldItem(bool leftHand, Vector2 mouseDelta)
    {
        Transform holdPoint = leftHand ? leftholdPoint : rightholdPoint;
        GameObject grabbedObject = leftHand ? leftGrabbedObject : rightGrabbedObject;
        if (holdPoint == null || grabbedObject == null || playerCamera == null)
        {
            return;
        }

        float yaw = -mouseDelta.x * heldRotateDegreesPerPixel;
        float pitch = mouseDelta.y * heldRotateDegreesPerPixel;
        Quaternion spin = Quaternion.AngleAxis(yaw, playerCamera.transform.up)
            * Quaternion.AngleAxis(pitch, playerCamera.transform.right);
        grabbedObject.transform.rotation = spin * grabbedObject.transform.rotation;

        Quaternion localRotation = Quaternion.Inverse(grabbedObject.transform.rotation) * holdPoint.rotation;
        if (leftHand)
        {
            leftGrabLocalRotation = localRotation;
        }
        else
        {
            rightGrabLocalRotation = localRotation;
        }
    }

    private InputAction FindGrabAction(string actionName)
    {
        if (playerControls == null)
        {
            return null;
        }
        InputActionMap map = playerControls.FindActionMap(actionMapName);
        if (map == null)
        {
            return null;
        }
        map.Enable();
        return map.FindAction(actionName);
    }
}
