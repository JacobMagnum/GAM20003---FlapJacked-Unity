using UnityEngine;

public class CrackableEgg : MonoBehaviour
{
    [Header("Break")]
    [SerializeField] private float crackSpeed = 3.5f;
    [SerializeField] private float smashSpeed = 7f;
    [SerializeField] private GameObject yolkPrefab;
    [SerializeField] private Transform spillPoint;
    [SerializeField] private float yolkAmount = 1f;

    private Rigidbody body;
    private CartonEgg cartonEgg;
    private Vector3 lastPosition;
    private bool cracked;
    private bool smashed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        cartonEgg = GetComponent<CartonEgg>();
        lastPosition = transform.position;
        if (spillPoint == null)
        {
            spillPoint = transform;
        }
    }

    private void FixedUpdate()
    {
        if (smashed)
        {
            return;
        }

        if (cartonEgg != null && cartonEgg.IsLocked)
        {
            lastPosition = transform.position;
            return;
        }

        Vector3 velocity;
        if (body != null && !body.isKinematic)
        {
            velocity = body.linearVelocity;
        }
        else
        {
            float dt = Time.fixedDeltaTime;
            velocity = dt > 0f ? (transform.position - lastPosition) / dt : Vector3.zero;
        }

        lastPosition = transform.position;
        float speed = velocity.magnitude;

        if (speed >= smashSpeed)
        {
            Smash(velocity);
            return;
        }

        if (!cracked && speed >= crackSpeed)
        {
            Crack(velocity);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (smashed || collision == null || collision.collider == null)
        {
            return;
        }

        if (cartonEgg != null && cartonEgg.IsLocked)
        {
            return;
        }

        Collider other = collision.collider;
        if (IsOwnCollider(other) || IsIgnoredContact(other))
        {
            return;
        }

        Vector3 velocity = collision.relativeVelocity;
        if (velocity.magnitude >= smashSpeed)
        {
            Smash(velocity);
            return;
        }

        if (!cracked)
        {
            Crack(velocity);
        }
    }

    private bool IsOwnCollider(Collider other)
    {
        return other.transform == transform || other.transform.IsChildOf(transform);
    }

    private static bool IsIgnoredContact(Collider other)
    {
        if (other.GetComponentInParent<IngredientBit>() != null)
        {
            return true;
        }

        if (other.GetComponent<CharacterController>() != null)
        {
            return true;
        }

        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void Crack(Vector3 velocity)
    {
        cracked = true;
        SpawnYolk(velocity * 0.25f);
    }

    private void Smash(Vector3 velocity)
    {
        smashed = true;
        NotifyGrabbersEggDestroyed();
        SpawnYolk(velocity * 0.2f);
        Destroy(gameObject);
    }

    private void NotifyGrabbersEggDestroyed()
    {
        ObjectGrabber[] grabbers = FindObjectsByType<ObjectGrabber>(FindObjectsSortMode.None);
        for (int i = 0; i < grabbers.Length; i++)
        {
            if (grabbers[i] != null)
            {
                grabbers[i].NotifyHeldObjectDestroyed(gameObject);
            }
        }
    }

    private void SpawnYolk(Vector3 inheritedVelocity)
    {
        if (yolkPrefab == null)
        {
            return;
        }

        GameObject drop = Instantiate(yolkPrefab, spillPoint.position, Quaternion.identity);
        IngredientBit bit = drop.GetComponent<IngredientBit>();
        if (bit == null)
        {
            bit = drop.AddComponent<IngredientBit>();
        }

        bit.Configure(IngredientKind.EggYolk, yolkAmount, 1f);

        Rigidbody dropBody = drop.GetComponent<Rigidbody>();
        if (dropBody == null)
        {
            dropBody = drop.AddComponent<Rigidbody>();
            dropBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (!dropBody.isKinematic)
        {
            dropBody.linearVelocity = inheritedVelocity;
        }
    }
}
