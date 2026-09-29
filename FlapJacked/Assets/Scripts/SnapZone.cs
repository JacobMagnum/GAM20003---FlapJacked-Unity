using UnityEngine;

public class SnapZone : MonoBehaviour
{
    [SerializeField] private SnapSurfaceType surfaceType = SnapSurfaceType.Burner;
    public string zoneId = "Burner";
    [SerializeField] private Transform snapTarget;
    [SerializeField] private bool parentPlacedItem = true;
    [SerializeField] private StoveKnob linkedKnob;

    public SnapSurfaceType SurfaceType
    {
        get { return surfaceType; }
    }

    public string ZoneId
    {
        get { return zoneId; }
    }

    public Transform SnapTarget
    {
        get { return snapTarget != null ? snapTarget : transform; }
    }

    public bool ParentPlacedItem
    {
        get { return parentPlacedItem; }
    }

    public StoveKnob LinkedKnob
    {
        get { return linkedKnob; }
    }

    public SnappableItem Occupant { get; private set; }

    public bool IsOccupied
    {
        get { return !HasEmptySlot(); }
    }

    public bool MatchesItem(SnappableItem item)
    {
        return item != null && item.CanSnapTo(this);
    }

    public bool MatchesAnyId(string[] ids)
    {
        return SnapSurface.IsMatch(surfaceType, zoneId, null, ids);
    }

    public bool CanAccept(SnappableItem item)
    {
        return item != null && item.CanSnapTo(this) && HasEmptySlot();
    }

    public bool HasEmptySlot()
    {
        SnapAnchor[] slots = GetSlots();
        if (slots.Length == 0)
        {
            return Occupant == null;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i].IsOccupied)
            {
                return true;
            }
        }

        return false;
    }

    public SnapAnchor FindEmptySlot(Vector3 preferPosition)
    {
        SnapAnchor[] slots = GetSlots();
        if (slots.Length == 0)
        {
            return null;
        }

        SnapAnchor best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            SnapAnchor slot = slots[i];
            if (slot == null || slot.IsOccupied)
            {
                continue;
            }

            float distance = (slot.transform.position - preferPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = slot;
            }
        }

        return best;
    }

    public Transform GetPlaceTarget(SnapAnchor slot)
    {
        if (slot != null)
        {
            return slot.transform;
        }

        return SnapTarget;
    }

    public bool TryOccupy(SnappableItem item, SnapAnchor slot)
    {
        if (item == null)
        {
            return false;
        }

        if (slot != null)
        {
            if (!slot.TryOccupy(item))
            {
                return false;
            }

            Occupant = item;
            return true;
        }

        if (Occupant != null)
        {
            return false;
        }

        Occupant = item;
        return true;
    }

    public void SetOccupant(SnappableItem item)
    {
        Occupant = item;
    }

    public void ClearOccupant(SnappableItem item, SnapAnchor slot)
    {
        if (slot != null)
        {
            slot.Vacate(item);
        }

        if (Occupant == item)
        {
            Occupant = HasAnyOccupant() ? Occupant : null;
            if (!HasAnyOccupant())
            {
                Occupant = null;
            }
        }
    }

    public void ClearOccupant(SnappableItem item)
    {
        ClearOccupant(item, null);
        SnapAnchor[] slots = GetSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Vacate(item);
            }
        }
    }

    private bool HasAnyOccupant()
    {
        SnapAnchor[] slots = GetSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].IsOccupied)
            {
                return true;
            }
        }

        return Occupant != null;
    }

    private SnapAnchor[] GetSlots()
    {
        SnapAnchor[] anchors = GetComponentsInChildren<SnapAnchor>(true);
        int count = 0;
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].IsSlot)
            {
                count++;
            }
        }

        SnapAnchor[] slots = new SnapAnchor[count];
        int index = 0;
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].IsSlot)
            {
                slots[index] = anchors[i];
                index++;
            }
        }

        return slots;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            zoneId = surfaceType.ToString();
        }
    }
}
