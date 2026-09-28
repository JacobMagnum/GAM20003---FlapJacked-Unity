using UnityEngine;
public class StoveKnob : MonoBehaviour
{
    [Header("Knob")]
    [SerializeField] private Transform knob;
    [SerializeField] private Collider knobCollider;
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
    [SerializeField] private float minAngle;
    [SerializeField] private float maxAngle = 160f;
    [SerializeField] private bool invertTurn;
    [Header("Heat")]
    [SerializeField] private float maxHeat = 10f;
    [SerializeField][Range(0f, 1f)] private float startHeat;
    [Header("Burner")]
    [SerializeField] private Light burnerLight;
    [SerializeField] private float minLightIntensity;
    [SerializeField] private float maxLightIntensity = 4f;
    [SerializeField] private ParticleSystem flame;
    [SerializeField] private float minEmission;
    [SerializeField] private float maxEmission = 24f;
    [SerializeField] private AudioSource burnerAudio;
    [SerializeField] private float maxAudioVolume = 0.5f;
    public float Heat { get; private set; }
    public float HeatNormalized { get; private set; }
    public float MaxHeat => maxHeat;
    public Collider KnobCollider => knobCollider;
    public bool InvertTurn => invertTurn;
    private float currentAngle;
    private Quaternion knobRestRotation;
    private ParticleSystem.EmissionModule flameEmission;
    private void Awake()
    {
        if (knob == null)
        {
            knob = transform;
        }
        if (knobCollider == null)
        {
            knobCollider = GetComponent<Collider>();
        }
        knobRestRotation = knob.localRotation;
        currentAngle = Mathf.Lerp(minAngle, maxAngle, startHeat);
        ApplyKnobAndBurner();
    }
    public bool OwnsCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }
        if (knobCollider != null && hitCollider == knobCollider)
        {
            return true;
        }
        return hitCollider.GetComponentInParent<StoveKnob>() == this;
    }
    public void AddTurn(float degrees)
    {
        if (invertTurn)
        {
            degrees = -degrees;
        }
        currentAngle = Mathf.Clamp(currentAngle + degrees, minAngle, maxAngle);
        ApplyKnobAndBurner();
    }
    public void SetHeatNormalized(float normalized)
    {
        HeatNormalized = Mathf.Clamp01(normalized);
        currentAngle = Mathf.Lerp(minAngle, maxAngle, HeatNormalized);
        ApplyKnobAndBurner();
    }
    private void ApplyKnobAndBurner()
    {
        HeatNormalized = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
        Heat = HeatNormalized * maxHeat;
        Vector3 axis = localRotationAxis.sqrMagnitude > 0f ? localRotationAxis.normalized : Vector3.forward;
        knob.localRotation = knobRestRotation * Quaternion.AngleAxis(currentAngle, axis);
        if (burnerLight != null)
        {
            burnerLight.enabled = HeatNormalized > 0.01f;
            burnerLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, HeatNormalized);
        }
        if (flame != null)
        {
            if (HeatNormalized > 0.01f)
            {
                if (!flame.isPlaying)
                {
                    flame.Play();
                }
                flameEmission = flame.emission;
                flameEmission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, HeatNormalized);
            }
            else if (flame.isPlaying)
            {
                flame.Stop();
            }
        }
        if (burnerAudio != null)
        {
            if (HeatNormalized > 0.01f)
            {
                if (!burnerAudio.isPlaying)
                {
                    burnerAudio.Play();
                }
                burnerAudio.volume = HeatNormalized * maxAudioVolume;
            }
            else if (burnerAudio.isPlaying)
            {
                burnerAudio.Stop();
            }
        }
    }
}