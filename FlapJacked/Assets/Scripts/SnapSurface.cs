using UnityEngine;

public enum SnapSurfaceType
{
    Any,
    Burner,
    Benchtop
}

public static class SnapSurface
{
    public static bool IsMatch(SnapSurfaceType zoneType, string zoneId, SnapSurfaceType[] acceptedTypes, string[] acceptedIds)
    {
        if (zoneType == SnapSurfaceType.Any)
        {
            return true;
        }

        if (acceptedTypes != null)
        {
            for (int i = 0; i < acceptedTypes.Length; i++)
            {
                SnapSurfaceType accepted = acceptedTypes[i];
                if (accepted == SnapSurfaceType.Any || accepted == zoneType)
                {
                    return true;
                }
            }
        }

        if (acceptedIds != null)
        {
            for (int i = 0; i < acceptedIds.Length; i++)
            {
                if (IdsMatch(zoneType, zoneId, acceptedIds[i]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IdsMatch(SnapSurfaceType zoneType, string zoneId, string acceptedId)
    {
        string accepted = Normalize(acceptedId);
        if (string.IsNullOrEmpty(accepted))
        {
            return false;
        }

        if (accepted == "any" || accepted == "*")
        {
            return true;
        }

        if (IsBenchtop(accepted) && (zoneType == SnapSurfaceType.Benchtop || IsBenchtop(zoneId)))
        {
            return true;
        }

        if (IsBurner(accepted) && (zoneType == SnapSurfaceType.Burner || IsBurner(zoneId)))
        {
            return true;
        }

        return accepted == Normalize(zoneId) || accepted == Normalize(zoneType.ToString());
    }

    private static bool IsBenchtop(string value)
    {
        string id = Normalize(value);
        return id == "benchtop" || id == "bench" || id == "counter" || id == "countertop" || id == "table";
    }

    private static bool IsBurner(string value)
    {
        string id = Normalize(value);
        return id == "burner" || id == "stove" || id == "hob";
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
