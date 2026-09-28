using UnityEngine;

public class SnapAnchor : MonoBehaviour
{
    public enum AnchorKind
    {
        Auto,
        Item,
        Slot
    }

    [SerializeField] private AnchorKind kind = AnchorKind.Auto;
    [SerializeField] private SnapSurfaceType surfaceType = SnapSurfaceType.Any;

    public SnappableItem Occupant { get; private set; }

    public SnapSurfaceType SurfaceType
    {
        get { return surfaceType; }
    }

    public bool IsOccupied
    {
        get { return Occupant != null; }
    }

    public bool IsSlot
    {
        get
        {
            if (kind == AnchorKind.Slot)
            {
                return true;
            }
            if (kind == AnchorKind.Item)
            {
                return false;
            }
            return GetComponentInParent<SnapZone>() != null && GetComponentInParent<SnappableItem>() == null;
        }
    }

    public bool IsItemAnchor
    {
        get
        {
            if (kind == AnchorKind.Item)
            {
                return true;
            }
            if (kind == AnchorKind.Slot)
            {
                return false;
            }
            return GetComponentInParent<SnappableItem>() != null;
        }
    }

    public bool MatchesSurface(SnapSurfaceType zoneSurface)
    {
        return surfaceType == SnapSurfaceType.Any || zoneSurface == SnapSurfaceType.Any || surfaceType == zoneSurface;
    }

    public bool TryOccupy(SnappableItem item)
    {
        if (item == null || IsOccupied)
        {
            return false;
        }

        Occupant = item;
        return true;
    }

    public void Vacate(SnappableItem item)
    {
        if (Occupant == item)
        {
            Occupant = null;
        }
    }
}
