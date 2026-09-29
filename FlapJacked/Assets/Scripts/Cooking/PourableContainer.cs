using UnityEngine;

public class PourableContainer : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] private IngredientKind ingredient = IngredientKind.Flour;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float remaining = 24f;
    [SerializeField] private float amountPerDrop = 1f;

    [Header("Pour")]
    [SerializeField] private Transform pourPoint;
    [SerializeField] private float pourDownDot = 0.45f;
    [SerializeField] private float dropsPerSecond = 8f;
    [SerializeField] private float dropSpeed = 0.6f;
    [SerializeField] private Vector3 pourLocalAxis = Vector3.up;

    private float pourCarry;

    private void Awake()
    {
        if (pourPoint == null)
        {
            pourPoint = transform;
        }
    }

    private void Update()
    {
        if (remaining <= 0f || dropPrefab == null || pourPoint == null)
        {
            return;
        }

        Vector3 mouthAxis = pourLocalAxis.sqrMagnitude > 0f ? pourLocalAxis.normalized : Vector3.up;
        float mouthDown = Vector3.Dot(pourPoint.TransformDirection(mouthAxis), Vector3.down);
        if (mouthDown < pourDownDot)
        {
            pourCarry = 0f;
            return;
        }

        pourCarry += dropsPerSecond * Time.deltaTime;
        while (pourCarry >= 1f && remaining > 0f)
        {
            pourCarry -= 1f;
            SpawnDrop();
        }
    }

    private void SpawnDrop()
    {
        float spawnAmount = Mathf.Min(amountPerDrop, remaining);
        remaining -= spawnAmount;

        GameObject drop = Instantiate(dropPrefab, pourPoint.position, Quaternion.identity);
        IngredientBit bit = drop.GetComponent<IngredientBit>();
        if (bit == null)
        {
            bit = drop.AddComponent<IngredientBit>();
        }

        bit.Configure(ingredient, spawnAmount, 1f);

        Rigidbody body = drop.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = drop.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        Vector3 pourDir = pourPoint.TransformDirection(pourLocalAxis.sqrMagnitude > 0f ? -pourLocalAxis.normalized : Vector3.down);
        if (!body.isKinematic)
        {
            body.linearVelocity = pourDir * dropSpeed;
        }
    }
}
