using UnityEngine;
using UnityEngine.InputSystem;

public class StoveKnobInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float mouseDegreesPerPixel = 0.35f;
    [SerializeField] private float turnSpeed = 2f;

    public bool IsHoldingKnob => heldKnob != null;
    public StoveKnob HeldKnob => heldKnob;

    private StoveKnob heldKnob;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
    }

    private void Update()
    {
        if (heldKnob == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        float degrees = (delta.x + delta.y) * mouseDegreesPerPixel * Mathf.Max(0f, turnSpeed);
        if (Mathf.Abs(degrees) > 0.0001f)
        {
            heldKnob.AddTurn(degrees);
        }
    }

    private void OnDisable()
    {
        ReleaseKnob();
    }

    public bool TryBeginHold(bool handAlreadyHoldingObject)
    {
        if (IsHoldingKnob || handAlreadyHoldingObject)
        {
            return false;
        }

        return TryGrabLookedAtKnob();
    }

    public bool TryHandleGrab(bool handAlreadyHoldingObject)
    {
        return TryBeginHold(handAlreadyHoldingObject);
    }

    public bool TryGrabLookedAtKnob()
    {
        StoveKnob knob = FindLookedAtKnob();
        if (knob == null)
        {
            return false;
        }

        heldKnob = knob;
        return true;
    }

    public void ReleaseKnob()
    {
        heldKnob = null;
    }

    private StoveKnob FindLookedAtKnob()
    {
        if (playerCamera == null)
        {
            return null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (playerCollider != null && hit.collider == playerCollider)
            {
                continue;
            }

            StoveKnob knob = hit.collider.GetComponentInParent<StoveKnob>();
            if (knob != null && knob.OwnsCollider(hit.collider))
            {
                return knob;
            }
        }

        return null;
    }
}
