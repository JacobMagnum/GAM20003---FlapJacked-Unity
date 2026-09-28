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
    private bool createdSnapAction;

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
    }
    private void OnEnable()
    {
        leftGrabAction = FindGrabAction(leftGrabActionName);
        rightGrabAction = FindGrabAction(rightGrabActionName);
        leftThrowAction = FindGrabAction(leftThrowActionName);
        rightThrowAction = FindGrabAction(rightThrowActionName);
        snapAction = FindOrCreateSnapAction();
        if (leftGrabAction != null)
        {
            leftGrabAction.Enable();
            leftGrabAction.performed += OnLeftGrabPerformed;
        }
        if (rightGrabAction != null)
        {
            rightGrabAction.Enable();
            rightGrabAction.performed += OnRightGrabPerformed;
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
    }
    private void OnDisable()
    {
        if (leftGrabAction != null)
        {
            leftGrabAction.performed -= OnLeftGrabPerformed;
            leftGrabAction.Disable();
        }
        if (rightGrabAction != null)
        {
            rightGrabAction.performed -= OnRightGrabPerformed;
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
    }
    private void OnLeftGrabPerformed(InputAction.CallbackContext context)
    {
        if (stoveKnobInteractor != null && stoveKnobInteractor.TryHandleGrab(isLeftGrabbing))
        {
            return;
        }

        if (isLeftGrabbing)
        {
            ReleaseObject(true);
        }
        else
        {
            GrabObject(true);
        }
    }
    private void OnRightGrabPerformed(InputAction.CallbackContext context)
    {
        if (stoveKnobInteractor != null && stoveKnobInteractor.TryHandleGrab(isRightGrabbing))
        {
            return;
        }

        if (isRightGrabbing)
        {
            ReleaseObject(false);
        }
        else
        {
            GrabObject(false);
        }
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
        TrySnapHeldItem();
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
        RaycastHit? chosenHit = null;
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
            if (hit.collider.transform.root == transform.root && !hit.collider.CompareTag("Grabbable"))
            {
                continue;
            }
            if (!hit.collider.CompareTag("Grabbable"))
            {
                continue;
            }
            chosenHit = hit;
            break;
        }
        if (!chosenHit.HasValue)
        {
            return;
        }
        RaycastHit grabHit = chosenHit.Value;
        Rigidbody targetRb = grabHit.collider.GetComponentInParent<Rigidbody>();
        if (targetRb == null)
        {
            return;
        }
        GameObject target = targetRb.gameObject;
        if (target == leftGrabbedObject || target == rightGrabbedObject)
        {
            return;
        }
        targetRb.linearVelocity = Vector3.zero;
        targetRb.angularVelocity = Vector3.zero;
        targetRb.useGravity = false;
        targetRb.isKinematic = true;
        targetRb.interpolation = RigidbodyInterpolation.None;
        NotifySpawnedPrefabPickedUp(target);
        NotifySnappablePickedUp(target);
        targetRb.detectCollisions = false;
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, grabHit.collider, true);
        }
        Transform grabAnchor = FindEmptyGrabAnchor(target.transform, grabHit.collider.transform, leftHand);
        GetGrabOffset(target.transform, grabAnchor, out Vector3 localPosition, out Quaternion localRotation);
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

    private static void NotifySpawnedPrefabPickedUp(GameObject target)
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
        if (playerCollider != null && grabbedCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, grabbedCollider, false);
        }

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
    private void LateUpdate()
    {
        if (isLeftGrabbing && leftGrabbedObject != null && leftholdPoint != null)
        {
            AlignObjectToHoldPoint(leftGrabbedObject.transform, leftholdPoint, leftGrabLocalPosition, leftGrabLocalRotation);
        }
        if (isRightGrabbing && rightGrabbedObject != null && rightholdPoint != null)
        {
            AlignObjectToHoldPoint(rightGrabbedObject.transform, rightholdPoint, rightGrabLocalPosition, rightGrabLocalRotation);
        }
    }

    private static Transform FindEmptyGrabAnchor(Transform target, Transform hitTransform, bool leftHand)
    {
        Transform marker = FindGrabPointMarker(target, hitTransform, leftHand);
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

    private static Transform FindGrabPointMarker(Transform target, Transform hitTransform, bool leftHand)
    {
        Transform match = null;
        Transform any = null;
        GrabPoint[] markers = target.GetComponentsInChildren<GrabPoint>(true);
        foreach (GrabPoint marker in markers)
        {
            if (IsHandMatch(marker, leftHand))
            {
                return marker.transform;
            }
            if (marker.GrabHand == GrabPoint.Hand.Any && any == null)
            {
                any = marker.transform;
            }
        }

        if (hitTransform != null && hitTransform != target)
        {
            markers = hitTransform.GetComponentsInChildren<GrabPoint>(true);
            foreach (GrabPoint marker in markers)
            {
                if (IsHandMatch(marker, leftHand))
                {
                    return marker.transform;
                }
                if (marker.GrabHand == GrabPoint.Hand.Any && any == null)
                {
                    any = marker.transform;
                }
            }
        }

        match = FindSiblingGrabPoint(target, leftHand, out Transform siblingAny);
        if (match != null)
        {
            return match;
        }
        return any != null ? any : siblingAny;
    }

    private static Transform FindSiblingGrabPoint(Transform target, bool leftHand, out Transform any)
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

            GrabPoint marker = sibling.GetComponent<GrabPoint>();
            if (marker == null)
            {
                continue;
            }
            if (IsHandMatch(marker, leftHand))
            {
                return sibling;
            }
            if (marker.GrabHand == GrabPoint.Hand.Any && any == null)
            {
                any = sibling;
            }
        }
        return null;
    }

    private static bool IsHandMatch(GrabPoint marker, bool leftHand)
    {
        return leftHand
            ? marker.GrabHand == GrabPoint.Hand.Left
            : marker.GrabHand == GrabPoint.Hand.Right;
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

    private static void GetGrabOffset(Transform objectTransform, Transform grabAnchor, out Vector3 localPosition, out Quaternion localRotation)
    {
        if (grabAnchor == null)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return;
        }

        localPosition = objectTransform.InverseTransformPoint(grabAnchor.position);
        localRotation = Quaternion.Inverse(objectTransform.rotation) * grabAnchor.rotation;
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
        if (playerCollider != null && grabbedCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, grabbedCollider, false);
        }
        grabbedRb.detectCollisions = true;
        grabbedRb.isKinematic = false;
        grabbedRb.useGravity = true;
        grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;
        grabbedRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
