using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MixingBowl : MonoBehaviour
{
    [Header("Recipe")]
    [SerializeField] private float requiredFlour = 8f;
    [SerializeField] private float requiredMilk = 8f;
    [SerializeField] private float requiredEgg = 2f;

    [Header("Batter")]
    [SerializeField] private GameObject batterPrefab;
    [SerializeField] private Transform batterSpawnPoint;
    [SerializeField] private int batterPieces = 6;
    [SerializeField] private float batterYield = 30f;
    [SerializeField] private float stirSpeed = 0.05f;
    [SerializeField] private float stirTime = 0.2f;

    [Header("Contain")]
    [SerializeField] private Transform opening;
    [SerializeField] private float openingRadius;
    [SerializeField] private float wallPadding = 0.02f;
    [SerializeField] private Vector3 openingLocalAxis = Vector3.up;
    [SerializeField] private float pourOutDot = 0.4f;

    private readonly List<IngredientBit> bitsInBowl = new List<IngredientBit>();
    private readonly HashSet<IngredientBit> capturedBits = new HashSet<IngredientBit>();
    private float stirProgress;
    private bool mixed;

    public bool Mixed => mixed;
    public float LastMixQuality { get; private set; }

    public float FlourAmount => AmountOf(IngredientKind.Flour);
    public float MilkAmount => AmountOf(IngredientKind.Milk);
    public float EggAmount => AmountOf(IngredientKind.EggWhite) + AmountOf(IngredientKind.EggYolk);

    private void Awake()
    {
        if (batterSpawnPoint == null)
        {
            batterSpawnPoint = transform;
        }

        Transform leftover = transform.Find("BowlSolidColliders");
        if (leftover == null)
        {
            Renderer visual = GetComponentInChildren<Renderer>();
            if (visual != null)
            {
                leftover = visual.transform.Find("BowlSolidColliders");
            }
        }
        if (leftover != null)
        {
            Destroy(leftover.gameObject);
        }

        KeepKinematicInWorld();
    }

    private void OnEnable()
    {
        KeepKinematicInWorld();
    }

    private void FixedUpdate()
    {
        RefreshBitsFromOverlap();
        CaptureNearbyBits();
    }

    private void LateUpdate()
    {
        ContainCapturedBits();
    }

    public void KeepKinematicInWorld()
    {
        Rigidbody body = GetOrCreateBody();
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.detectCollisions = true;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.maxDepenetrationVelocity = 0.01f;
    }

    public void EnableDroppedPhysics()
    {
        KeepKinematicInWorld();
        DropOntoSurfaceBelow();
    }

    private void DropOntoSurfaceBelow()
    {
        Bounds bounds = GetVisualBounds();
        Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + 0.05f, bounds.center.z);
        float maxDistance = 3f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsOwnCollider(hit.collider) || IsPlayerCollider(hit.collider))
            {
                continue;
            }

            float desiredY = transform.position.y + (hit.point.y + 0.01f - bounds.min.y);
            if (desiredY >= transform.position.y)
            {
                return;
            }

            Vector3 position = transform.position;
            position.y = desiredY;
            transform.position = position;
            return;
        }
    }

    private Rigidbody GetOrCreateBody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        return body;
    }

    private Bounds GetVisualBounds()
    {
        Renderer visual = GetComponentInChildren<Renderer>();
        if (visual != null)
        {
            return visual.bounds;
        }

        return GetWorldBounds();
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return true;
        }

        Transform hitTransform = hitCollider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private static bool IsPlayerCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.GetComponent<CharacterController>() != null)
        {
            return true;
        }

        Transform current = hitCollider.transform;
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

    private Bounds GetWorldBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled || col.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds)
        {
            Renderer visual = GetComponentInChildren<Renderer>();
            if (visual != null)
            {
                return visual.bounds;
            }
        }

        return bounds;
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientBit bit = other.GetComponentInParent<IngredientBit>();
        if (bit == null || bit.Kind == IngredientKind.Batter)
        {
            return;
        }

        if (!bitsInBowl.Contains(bit))
        {
            bitsInBowl.Add(bit);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IngredientBit bit = other.GetComponentInParent<IngredientBit>();
        if (bit != null)
        {
            bitsInBowl.Remove(bit);
        }
    }

    private void CaptureNearbyBits()
    {
        if (IsPouringOut())
        {
            return;
        }

        for (int i = 0; i < bitsInBowl.Count; i++)
        {
            if (bitsInBowl[i] != null)
            {
                capturedBits.Add(bitsInBowl[i]);
            }
        }

        Bounds bounds = GetVisualBounds();
        Vector3 extents = bounds.extents * 0.9f;
        extents.x = Mathf.Max(extents.x, 0.08f);
        extents.y = Mathf.Max(extents.y, 0.08f);
        extents.z = Mathf.Max(extents.z, 0.08f);
        Collider[] hits = Physics.OverlapBox(bounds.center, extents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            IngredientBit bit = hits[i].GetComponentInParent<IngredientBit>();
            if (bit != null)
            {
                capturedBits.Add(bit);
            }
        }
    }

    private void ContainCapturedBits()
    {
        capturedBits.RemoveWhere(bit => bit == null);
        if (IsPouringOut())
        {
            foreach (IngredientBit bit in capturedBits)
            {
                if (bit != null && !IsHeldByPlayer(bit.transform))
                {
                    DetachFromBowl(bit);
                }
            }

            capturedBits.Clear();
            return;
        }

        List<IngredientBit> released = null;
        foreach (IngredientBit bit in capturedBits)
        {
            if (IsHeldByPlayer(bit.transform))
            {
                DetachFromBowl(bit);
                continue;
            }

            if (ContainBit(bit))
            {
                continue;
            }

            if (released == null)
            {
                released = new List<IngredientBit>();
            }

            released.Add(bit);
        }

        if (released == null)
        {
            return;
        }

        for (int i = 0; i < released.Count; i++)
        {
            DetachFromBowl(released[i]);
            capturedBits.Remove(released[i]);
        }
    }

    private bool ContainBit(IngredientBit bit)
    {
        GetBowlInterior(out Vector3 localCenter, out float rimY, out float floorY, out float radius);
        Vector3 local = transform.InverseTransformPoint(bit.transform.position);
        Vector2 fromCenter = new Vector2(local.x - localCenter.x, local.z - localCenter.z);
        float horizontal = fromCenter.magnitude;
        bool aboveRim = local.y > rimY;
        bool inOpening = horizontal <= radius;

        if (aboveRim && inOpening)
        {
            return false;
        }

        AttachToBowl(bit);
        local = bit.transform.localPosition;

        fromCenter = new Vector2(local.x - localCenter.x, local.z - localCenter.z);
        horizontal = fromCenter.magnitude;
        if (horizontal > radius && fromCenter.sqrMagnitude > 0.0001f)
        {
            Vector2 clamped = fromCenter.normalized * radius;
            local.x = localCenter.x + clamped.x;
            local.z = localCenter.z + clamped.y;
        }

        local.y = Mathf.Clamp(local.y, floorY, rimY);
        bit.transform.localPosition = local;
        return true;
    }

    private void AttachToBowl(IngredientBit bit)
    {
        if (bit.transform.parent != transform)
        {
            bit.transform.SetParent(transform, true);
        }

        Rigidbody body = bit.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;
    }

    private void DetachFromBowl(IngredientBit bit)
    {
        if (bit == null)
        {
            return;
        }

        if (bit.transform.parent == transform)
        {
            bit.transform.SetParent(null, true);
        }

        Rigidbody body = bit.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.detectCollisions = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void GetBowlInterior(out Vector3 localCenter, out float rimY, out float floorY, out float radius)
    {
        Bounds localBounds = GetLocalVisualBounds();
        localCenter = localBounds.center;
        rimY = localBounds.max.y - wallPadding;
        floorY = localBounds.min.y + wallPadding;
        radius = openingRadius > 0.01f
            ? openingRadius
            : Mathf.Min(localBounds.extents.x, localBounds.extents.z) * 0.72f;

        if (opening != null)
        {
            Vector3 openingLocal = transform.InverseTransformPoint(opening.position);
            localCenter.x = openingLocal.x;
            localCenter.z = openingLocal.z;
            rimY = openingLocal.y;
        }
    }

    private Bounds GetLocalVisualBounds()
    {
        Renderer visual = GetComponentInChildren<Renderer>();
        if (visual == null)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.2f);
        }

        Bounds meshLocal = visual.localBounds;
        Bounds bowlLocal = new Bounds(transform.InverseTransformPoint(visual.transform.TransformPoint(meshLocal.center)), Vector3.zero);
        Vector3 c = meshLocal.center;
        Vector3 e = meshLocal.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 world = visual.transform.TransformPoint(c + Vector3.Scale(e, new Vector3(x, y, z)));
                    bowlLocal.Encapsulate(transform.InverseTransformPoint(world));
                }
            }
        }

        return bowlLocal;
    }

    private bool IsPouringOut()
    {
        Vector3 mouth = opening != null
            ? opening.up
            : transform.TransformDirection(openingLocalAxis.sqrMagnitude > 0f ? openingLocalAxis.normalized : Vector3.up);
        return Vector3.Dot(mouth, Vector3.down) >= pourOutDot;
    }

    private bool IsHeldByPlayer(Transform bitTransform)
    {
        if (bitTransform.IsChildOf(transform) || bitTransform.parent == transform)
        {
            return false;
        }

        return bitTransform.GetComponentInParent<ObjectGrabber>() != null;
    }

    public void NotifyStir(float speed)
    {
        RefreshBitsFromOverlap();
        if (mixed || !HasAllIngredients())
        {
            stirProgress = 0f;
            return;
        }

        // Any whisk motion, or the whisk simply sitting in the bowl, counts.
        if (speed < stirSpeed)
        {
            stirProgress += Time.deltaTime * 0.75f;
        }
        else
        {
            stirProgress += Time.deltaTime;
        }
        if (stirProgress >= stirTime)
        {
            Mix();
        }
    }

    public bool HasAllIngredients()
    {
        RefreshBitsFromOverlap();
        return FlourAmount > 0.01f && MilkAmount > 0.01f && EggAmount > 0.01f;
    }

    public void Mix()
    {
        RefreshBitsFromOverlap();
        if (mixed || !HasAllIngredients())
        {
            return;
        }

        LastMixQuality = ComputeQuality(FlourAmount, MilkAmount, EggAmount);
        mixed = true;

        for (int i = bitsInBowl.Count - 1; i >= 0; i--)
        {
            IngredientBit bit = bitsInBowl[i];
            if (bit != null)
            {
                Destroy(bit.gameObject);
            }
        }

        bitsInBowl.Clear();

        int pieces = Mathf.Max(1, batterPieces);
        for (int i = 0; i < pieces; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.05f;
            offset.y = Mathf.Abs(offset.y);
            GameObject batter = batterPrefab != null
                ? Instantiate(batterPrefab, batterSpawnPoint.position + offset, Random.rotation)
                : CreateFallbackBatter(batterSpawnPoint.position + offset);
            IngredientBit bit = batter.GetComponent<IngredientBit>();
            if (bit == null)
            {
                bit = batter.AddComponent<IngredientBit>();
            }

            bit.Configure(IngredientKind.Batter, batterYield / pieces, LastMixQuality);
            if (batter.GetComponent<BatterBlob>() == null)
            {
                batter.AddComponent<BatterBlob>();
            }
        }
    }

    private static GameObject CreateFallbackBatter(Vector3 position)
    {
        GameObject batter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        batter.name = "Batter";
        batter.transform.position = position;
        batter.transform.localScale = Vector3.one * 0.04f;
        Rigidbody body = batter.AddComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        return batter;
    }

    private void RefreshBitsFromOverlap()
    {
        bitsInBowl.Clear();
        Bounds bounds = GetVisualBounds();
        Vector3 extents = bounds.extents * 0.9f;
        extents.x = Mathf.Max(extents.x, 0.08f);
        extents.y = Mathf.Max(extents.y, 0.08f);
        extents.z = Mathf.Max(extents.z, 0.08f);

        Collider[] hits = Physics.OverlapBox(bounds.center, extents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            IngredientBit bit = hits[i].GetComponentInParent<IngredientBit>();
            if (bit == null || bit.Kind == IngredientKind.Batter || bitsInBowl.Contains(bit))
            {
                continue;
            }

            bitsInBowl.Add(bit);
        }
    }

    private float ComputeQuality(float flour, float milk, float egg)
    {
        float flourScore = 1f - Mathf.Clamp01(Mathf.Abs(flour - requiredFlour) / Mathf.Max(requiredFlour, 0.01f));
        float milkScore = 1f - Mathf.Clamp01(Mathf.Abs(milk - requiredMilk) / Mathf.Max(requiredMilk, 0.01f));
        float eggScore = 1f - Mathf.Clamp01(Mathf.Abs(egg - requiredEgg) / Mathf.Max(requiredEgg, 0.01f));
        return (flourScore + milkScore + eggScore) / 3f;
    }

    private float AmountOf(IngredientKind kind)
    {
        float total = 0f;
        for (int i = 0; i < bitsInBowl.Count; i++)
        {
            IngredientBit bit = bitsInBowl[i];
            if (bit != null && bit.Kind == kind)
            {
                total += bit.Amount;
            }
        }

        return total;
    }
}
