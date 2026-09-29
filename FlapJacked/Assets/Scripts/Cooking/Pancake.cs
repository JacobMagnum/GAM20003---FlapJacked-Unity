using UnityEngine;

public class Pancake : MonoBehaviour
{
    [SerializeField] private float perfectCook = 8f;
    [SerializeField] private Color rawColor = new Color(0.97f, 0.9f, 0.7f);
    [SerializeField] private Color cookedColor = new Color(0.72f, 0.45f, 0.18f);
    [SerializeField] private Color burntColor = new Color(0.18f, 0.1f, 0.05f);
    [SerializeField] private float flipCooldown = 0.6f;

    public float CookScore { get; private set; }
    public bool IsFlipped { get; private set; }

    private FryingPan pan;
    private float cookAmount;
    private float lastSpatulaTime = -10f;
    private bool cooking = true;
    private Renderer[] renderers;
    private Color[] baseColors;

    public void Initialize(FryingPan owner)
    {
        pan = owner;
        CacheColors();
        ApplyColor();
    }

    private void Awake()
    {
        CacheColors();
    }

    private void OnDestroy()
    {
        if (pan != null)
        {
            pan.NotifyPancakeRemoved(this);
        }
    }

    private void Update()
    {
        if (pan == null)
        {
            pan = GetComponentInParent<FryingPan>();
        }

        if (pan == null || !cooking)
        {
            return;
        }

        float heat = pan.GetHeatNormalized() * pan.GetMaxHeat();
        cookAmount += heat * Time.deltaTime;
        UpdateScore();
        ApplyColor();
    }

    public bool TryUseSpatula()
    {
        if (Time.time - lastSpatulaTime < flipCooldown)
        {
            return false;
        }

        if (!IsFlipped)
        {
            return TryFlip();
        }

        return DetachFromPan();
    }

    public bool TryFlip()
    {
        if (pan == null || !pan.HasButter || !cooking)
        {
            return false;
        }

        lastSpatulaTime = Time.time;
        IsFlipped = true;
        transform.Rotate(Vector3.right, 180f, Space.Self);
        return true;
    }

    public bool DetachFromPan()
    {
        if (!cooking)
        {
            return false;
        }

        lastSpatulaTime = Time.time;
        cooking = false;
        transform.SetParent(null, true);
        if (!CompareTag("Grabbable"))
        {
            gameObject.tag = "Grabbable";
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.detectCollisions = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (pan != null)
        {
            pan.NotifyPancakeRemoved(this);
            pan = null;
        }

        return true;
    }

    private void UpdateScore()
    {
        if (cookAmount <= perfectCook)
        {
            CookScore = 100f * (cookAmount / Mathf.Max(0.01f, perfectCook));
            return;
        }

        float over = cookAmount - perfectCook;
        CookScore = Mathf.Max(0f, 100f * (1f - over / Mathf.Max(0.01f, perfectCook)));
    }

    private void CacheColors()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                baseColors[i] = renderers[i].material.color;
            }
        }
    }

    private void ApplyColor()
    {
        if (renderers == null)
        {
            return;
        }

        Color cookColor = cookAmount <= perfectCook
            ? Color.Lerp(rawColor, cookedColor, cookAmount / Mathf.Max(0.01f, perfectCook))
            : Color.Lerp(cookedColor, burntColor, Mathf.Clamp01((cookAmount - perfectCook) / Mathf.Max(0.01f, perfectCook)));

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].material == null)
            {
                continue;
            }

            renderers[i].material.color = cookColor;
        }
    }
}
