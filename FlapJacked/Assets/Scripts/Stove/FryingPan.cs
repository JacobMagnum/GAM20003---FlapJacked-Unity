using System.Collections.Generic;
using UnityEngine;

public class FryingPan : MonoBehaviour
{
    [Header("Batter")]
    [SerializeField] private float batterNeeded = 30f;
    [SerializeField] private GameObject pancakePrefab;
    [SerializeField] private Transform cookPoint;

    [Header("Heat")]
    [SerializeField] private StoveKnob heatKnob;
    [SerializeField] private float heatDetectDistance = 0.75f;
    [SerializeField] private float heatSearchRadius = 0.55f;

    public float BatterOnPan { get; private set; }
    public bool HasButter => butterApplied;
    public Pancake CurrentPancake { get; private set; }

    private readonly List<IngredientBit> batterBits = new List<IngredientBit>();
    private bool butterApplied;

    private void Awake()
    {
        if (cookPoint == null)
        {
            cookPoint = transform;
        }
    }

    private void FixedUpdate()
    {
        CollectContents();
        if (CurrentPancake == null && BatterOnPan >= batterNeeded)
        {
            SpawnPancake();
        }
    }

    public float GetHeatNormalized()
    {
        StoveKnob knob = ResolveHeatKnob();
        return knob != null ? knob.HeatNormalized : 0f;
    }

    public float GetMaxHeat()
    {
        StoveKnob knob = ResolveHeatKnob();
        return knob != null ? knob.MaxHeat : 1f;
    }

    private StoveKnob ResolveHeatKnob()
    {
        if (heatKnob != null)
        {
            return heatKnob;
        }

        Vector3 origin = cookPoint != null ? cookPoint.position : transform.position;
        origin += Vector3.up * 0.03f;

        RaycastHit[] rayHits = Physics.RaycastAll(origin, Vector3.down, heatDetectDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(rayHits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < rayHits.Length; i++)
        {
            StoveKnob fromRay = KnobFromCollider(rayHits[i].collider);
            if (fromRay != null)
            {
                return fromRay;
            }
        }

        Vector3 overlapCenter = origin + Vector3.down * (heatDetectDistance * 0.35f);
        Vector3 overlapSize = new Vector3(heatSearchRadius, heatDetectDistance * 0.5f, heatSearchRadius);
        Collider[] overlaps = Physics.OverlapBox(overlapCenter, overlapSize, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        StoveKnob fromOverlap = ClosestKnobFromColliders(overlaps, origin);
        if (fromOverlap != null)
        {
            return fromOverlap;
        }

        return FindNearestBurnerKnob(origin);
    }

    private static StoveKnob KnobFromCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        SnapZone zone = hitCollider.GetComponentInParent<SnapZone>();
        if (zone != null && zone.LinkedKnob != null)
        {
            return zone.LinkedKnob;
        }

        return hitCollider.GetComponentInParent<StoveKnob>();
    }

    private StoveKnob ClosestKnobFromColliders(Collider[] colliders, Vector3 origin)
    {
        StoveKnob best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            StoveKnob knob = KnobFromCollider(colliders[i]);
            if (knob == null)
            {
                continue;
            }

            SnapZone zone = colliders[i].GetComponentInParent<SnapZone>();
            Vector3 point = zone != null ? zone.SnapTarget.position : knob.transform.position;
            float distance = (point - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = knob;
            }
        }

        return best;
    }

    private StoveKnob FindNearestBurnerKnob(Vector3 origin)
    {
        SnapZone[] zones = FindObjectsByType<SnapZone>(FindObjectsSortMode.None);
        StoveKnob best = null;
        float bestDistance = heatSearchRadius * heatSearchRadius * 4f;
        for (int i = 0; i < zones.Length; i++)
        {
            SnapZone zone = zones[i];
            if (zone == null || zone.LinkedKnob == null)
            {
                continue;
            }

            if (zone.SurfaceType != SnapSurfaceType.Burner && zone.SurfaceType != SnapSurfaceType.Any)
            {
                continue;
            }

            float distance = (zone.SnapTarget.position - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = zone.LinkedKnob;
            }
        }

        return best;
    }

    public void NotifyPancakeRemoved(Pancake pancake)
    {
        if (CurrentPancake == pancake)
        {
            CurrentPancake = null;
            butterApplied = false;
        }
    }

    private void CollectContents()
    {
        batterBits.Clear();
        BatterOnPan = 0f;

        Bounds bounds = GetWorldBounds();
        Vector3 extents = bounds.extents;
        extents.x = Mathf.Max(extents.x, 0.08f);
        extents.y = Mathf.Max(extents.y, 0.06f);
        extents.z = Mathf.Max(extents.z, 0.08f);
        Collider[] hits = Physics.OverlapBox(bounds.center, extents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            IngredientBit bit = hits[i].GetComponentInParent<IngredientBit>();
            if (bit == null || IsHeld(bit.transform))
            {
                continue;
            }

            if (bit.Kind == IngredientKind.Butter)
            {
                butterApplied = true;
                Destroy(bit.gameObject);
                continue;
            }

            if (bit.Kind == IngredientKind.Batter && CurrentPancake == null)
            {
                if (!batterBits.Contains(bit))
                {
                    batterBits.Add(bit);
                    BatterOnPan += bit.Amount;
                }
            }
        }
    }

    private void SpawnPancake()
    {
        if (pancakePrefab == null)
        {
            return;
        }

        for (int i = 0; i < batterBits.Count; i++)
        {
            if (batterBits[i] != null)
            {
                Destroy(batterBits[i].gameObject);
            }
        }

        batterBits.Clear();
        BatterOnPan = 0f;

        GameObject pancakeObject = Instantiate(pancakePrefab, cookPoint.position, cookPoint.rotation, transform);
        CurrentPancake = pancakeObject.GetComponent<Pancake>();
        if (CurrentPancake == null)
        {
            CurrentPancake = pancakeObject.AddComponent<Pancake>();
        }

        CurrentPancake.Initialize(this);
    }

    private Bounds GetWorldBounds()
    {
        Renderer visual = GetComponentInChildren<Renderer>();
        if (visual != null)
        {
            return visual.bounds;
        }

        Collider col = GetComponentInChildren<Collider>();
        return col != null ? col.bounds : new Bounds(transform.position, Vector3.one * 0.2f);
    }

    private static bool IsHeld(Transform target)
    {
        return target.GetComponentInParent<ObjectGrabber>() != null;
    }
}
