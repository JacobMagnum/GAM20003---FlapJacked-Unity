using UnityEngine;

public class CartonEgg : MonoBehaviour
{
    [SerializeField] private Transform carton;
    [SerializeField] private bool lockOnAwake = true;
    [SerializeField] private float unlockedMass = 0.2f;

    private Rigidbody body;
    private Vector3 lockedLocalPosition;
    private Quaternion lockedLocalRotation;
    private bool locked;
    private float savedMass = 0.2f;

    public bool IsLocked => locked;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (carton == null)
        {
            carton = transform.parent;
        }

        if (lockOnAwake)
        {
            LockToCarton();
        }
    }

    private void Start()
    {
        if (locked)
        {
            IgnoreCartonCollisions(true);
        }
    }

    private void LateUpdate()
    {
        if (!locked || carton == null)
        {
            return;
        }

        if (transform.parent != carton)
        {
            Unlock();
            return;
        }

        transform.localPosition = lockedLocalPosition;
        transform.localRotation = lockedLocalRotation;
    }

    public void LockToCarton()
    {
        if (carton == null)
        {
            carton = transform.parent;
        }

        if (carton != null && transform.parent != carton)
        {
            transform.SetParent(carton, true);
        }

        lockedLocalPosition = transform.localPosition;
        lockedLocalRotation = transform.localRotation;
        locked = true;
        StripRigidbody();
        IgnoreCartonCollisions(true);
    }

    public void Unlock()
    {
        if (!locked)
        {
            EnsureRigidbody();
            return;
        }

        locked = false;
        transform.SetParent(null, true);
        EnsureRigidbody();
        IgnoreCartonCollisions(true);

        if (body != null)
        {
            body.detectCollisions = true;
            SpawnedPrefab.EnsureWorldColliders(body.gameObject);
        }
    }

    public Rigidbody EnsureRigidbody()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
            body.mass = savedMass > 0.01f ? savedMass : unlockedMass;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.None;
        }

        return body;
    }

    private void StripRigidbody()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        savedMass = body.mass > 0.01f ? body.mass : unlockedMass;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Destroy(body);
        body = null;
    }

    private void IgnoreCartonCollisions(bool ignore)
    {
        if (carton == null)
        {
            return;
        }

        Collider[] eggColliders = GetComponentsInChildren<Collider>(true);
        Collider[] cartonColliders = carton.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < eggColliders.Length; i++)
        {
            Collider eggCollider = eggColliders[i];
            if (eggCollider == null)
            {
                continue;
            }

            for (int j = 0; j < cartonColliders.Length; j++)
            {
                Collider other = cartonColliders[j];
                if (other == null || other == eggCollider)
                {
                    continue;
                }

                if (other.transform == transform || other.transform.IsChildOf(transform))
                {
                    continue;
                }

                Physics.IgnoreCollision(eggCollider, other, ignore);
            }
        }
    }
}
