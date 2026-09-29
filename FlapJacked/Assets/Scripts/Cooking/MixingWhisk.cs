using UnityEngine;

public class MixingWhisk : MonoBehaviour
{
    [SerializeField] private float velocitySampleRate = 0.02f;
    [SerializeField] private float mixCheckRadius = 0.8f;

    private MixingBowl bowlInContact;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float sampleTimer;

    private void Awake()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void Update()
    {
        bowlInContact = FindBowlNearby();
        if (bowlInContact == null)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            return;
        }

        sampleTimer += Time.deltaTime;
        float dt = Mathf.Max(velocitySampleRate, Time.deltaTime);
        if (sampleTimer < velocitySampleRate)
        {
            return;
        }

        float moveSpeed = (transform.position - lastPosition).magnitude / dt;
        float turnSpeed = Quaternion.Angle(lastRotation, transform.rotation) / dt;
        float speed = moveSpeed + turnSpeed * 0.02f;
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        sampleTimer = 0f;
        bowlInContact.NotifyStir(speed);
    }

    private MixingBowl FindBowlNearby()
    {
        Vector3 center = transform.position;
        float radius = mixCheckRadius;
        Renderer visual = GetComponentInChildren<Renderer>();
        if (visual != null)
        {
            center = visual.bounds.center;
            radius = Mathf.Max(mixCheckRadius, visual.bounds.extents.magnitude + 0.15f);
        }

        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            MixingBowl bowl = hits[i].GetComponentInParent<MixingBowl>();
            if (bowl != null)
            {
                return bowl;
            }
        }

        return null;
    }
}
