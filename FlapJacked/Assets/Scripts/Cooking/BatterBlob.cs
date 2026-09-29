using UnityEngine;

public class BatterBlob : MonoBehaviour
{
    [SerializeField] private float mass = 0.04f;
    [SerializeField] private float drag = 4f;
    [SerializeField] private float angularDrag = 6f;
    [SerializeField] private float cohesionRadius = 0.12f;
    [SerializeField] private float cohesionForce = 8f;
    [SerializeField] private float maxSpeed = 1.5f;

    private Rigidbody body;
    private static PhysicsMaterial sharedBatterMaterial;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.mass = mass;
        body.linearDamping = drag;
        body.angularDamping = angularDrag;
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.maxLinearVelocity = maxSpeed;
        body.maxDepenetrationVelocity = 1f;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.sharedMaterial = GetBatterMaterial();
        }
    }

    private void FixedUpdate()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, cohesionRadius, ~0, QueryTriggerInteraction.Ignore);
        Vector3 pull = Vector3.zero;
        int count = 0;
        for (int i = 0; i < nearby.Length; i++)
        {
            BatterBlob other = nearby[i].GetComponentInParent<BatterBlob>();
            if (other == null || other == this)
            {
                continue;
            }

            Vector3 toOther = other.transform.position - transform.position;
            float dist = toOther.magnitude;
            if (dist < 0.001f)
            {
                continue;
            }

            pull += toOther.normalized * (1f - dist / cohesionRadius);
            count++;
        }

        if (count > 0)
        {
            body.AddForce(pull / count * cohesionForce, ForceMode.Acceleration);
        }
    }

    private static PhysicsMaterial GetBatterMaterial()
    {
        if (sharedBatterMaterial != null)
        {
            return sharedBatterMaterial;
        }

        sharedBatterMaterial = new PhysicsMaterial("BatterGoo");
        sharedBatterMaterial.bounciness = 0f;
        sharedBatterMaterial.staticFriction = 0.12f;
        sharedBatterMaterial.dynamicFriction = 0.18f;
        sharedBatterMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        sharedBatterMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
        return sharedBatterMaterial;
    }
}
