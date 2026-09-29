using UnityEngine;

public class Spatula : MonoBehaviour
{
    [SerializeField] private float pressRadius = 0.15f;

    private bool wasPressing;

    private void Update()
    {
        Pancake pancake = FindPressedPancake();
        bool pressing = pancake != null;
        if (pressing && !wasPressing)
        {
            pancake.TryUseSpatula();
        }

        wasPressing = pressing;
    }

    private Pancake FindPressedPancake()
    {
        Vector3 center = transform.position;
        float radius = pressRadius;
        Renderer visual = GetComponentInChildren<Renderer>();
        if (visual != null)
        {
            center = visual.bounds.center;
            radius = Mathf.Max(pressRadius, visual.bounds.extents.magnitude);
        }

        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
        Pancake best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Pancake pancake = hits[i].GetComponentInParent<Pancake>();
            if (pancake == null)
            {
                continue;
            }

            float distance = (pancake.transform.position - center).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = pancake;
            }
        }

        return best;
    }
}
