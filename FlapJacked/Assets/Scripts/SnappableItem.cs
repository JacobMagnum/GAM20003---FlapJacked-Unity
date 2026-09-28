using UnityEngine;

public class SnappableItem : MonoBehaviour
{
    [SerializeField] private SnapSurfaceType[] acceptedSurfaces = { SnapSurfaceType.Benchtop };
    [SerializeField] private string[] extraAcceptedIds;
    [SerializeField] private Transform snapAnchor;

    public SnapZone CurrentZone { get; private set; }
    public SnapAnchor CurrentSlot { get; private set; }

    public bool CanSnapTo(SnapZone zone)
    {
        if (zone == null)
        {
            return false;
        }

        return SnapSurface.IsMatch(zone.SurfaceType, zone.ZoneId, acceptedSurfaces, extraAcceptedIds);
    }

    public Transform GetSnapAnchor(SnapZone zone)
    {
        SnapAnchor[] anchors = GetComponentsInChildren<SnapAnchor>(true);
        SnapAnchor typed = null;
        SnapAnchor firstItemAnchor = null;
        for (int i = 0; i < anchors.Length; i++)
        {
            SnapAnchor anchor = anchors[i];
            if (anchor == null || !anchor.IsItemAnchor)
            {
                continue;
            }

            if (firstItemAnchor == null)
            {
                firstItemAnchor = anchor;
            }

            if (zone != null && anchor.MatchesSurface(zone.SurfaceType))
            {
                typed = anchor;
                break;
            }
        }

        if (typed != null)
        {
            return typed.transform;
        }
        if (firstItemAnchor != null)
        {
            return firstItemAnchor.transform;
        }
        if (snapAnchor != null)
        {
            return snapAnchor;
        }

        return transform;
    }

    public Transform GetSnapAnchor()
    {
        return GetSnapAnchor(CurrentZone);
    }

    public bool AttachToZone(SnapZone zone, SnapAnchor slot)
    {
        if (zone == null)
        {
            return false;
        }

        LeaveZone();
        if (!zone.TryOccupy(this, slot))
        {
            return false;
        }

        CurrentZone = zone;
        CurrentSlot = slot;
        return true;
    }

    public void AttachToZone(SnapZone zone)
    {
        AttachToZone(zone, zone != null ? zone.FindEmptySlot(transform.position) : null);
    }

    public void LeaveZone()
    {
        if (CurrentZone == null)
        {
            CurrentSlot = null;
            return;
        }

        CurrentZone.ClearOccupant(this, CurrentSlot);
        CurrentZone = null;
        CurrentSlot = null;
    }

    private void OnDestroy()
    {
        LeaveZone();
    }
}
