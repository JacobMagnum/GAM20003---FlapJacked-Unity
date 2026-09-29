using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    private PrefabSpawner spawner;
    private int slotIndex = -1;
    private bool consumed;
    private float readyTime;

    public bool IsConsumed => consumed;

    public void Bind(PrefabSpawner owner)
    {
        Initialize(owner, 0);
    }

    public void Bind(PrefabSpawner owner, int spawnSlot)
    {
        Initialize(owner, spawnSlot);
    }

    private void Awake()
    {
        if (NeedsCartonColliders())
        {
            EnsureWorldColliders();
        }
    }

    public void Initialize(PrefabSpawner owner, int spawnSlot)
    {
        spawner = owner;
        slotIndex = spawnSlot;
        consumed = false;
        readyTime = Time.time + 0.35f;
        if (NeedsCartonColliders())
        {
            EnsureWorldColliders();
        }
    }

    public void NotifyPickedUp()
    {
        Consume();
    }

    public void NotifyLeftArea()
    {
        if (Time.time < readyTime)
        {
            return;
        }

        Consume();
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded)
        {
            return;
        }

        Consume();
    }

    private void Consume()
    {
        if (consumed || spawner == null)
        {
            return;
        }

        consumed = true;
        spawner.RequestRespawn(slotIndex);
    }

    private bool NeedsCartonColliders()
    {
        return GetComponentInChildren<CartonEgg>(true) != null;
    }

    public static void EnsureWorldColliders(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        MeshCollider[] meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            ReplaceNonConvexMeshCollider(meshColliders[i]);
        }
    }

    private void EnsureWorldColliders()
    {
        EnsureWorldColliders(gameObject);
    }

    private static void ReplaceNonConvexMeshCollider(MeshCollider meshCollider)
    {
        if (meshCollider == null || meshCollider.isTrigger)
        {
            return;
        }

        Rigidbody body = meshCollider.attachedRigidbody;
        if (body == null)
        {
            body = meshCollider.GetComponentInParent<Rigidbody>();
        }

        if (body == null)
        {
            return;
        }

        if (meshCollider.convex)
        {
            return;
        }

        BoxCollider box = meshCollider.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = meshCollider.gameObject.AddComponent<BoxCollider>();
        }

        Mesh mesh = meshCollider.sharedMesh;
        if (mesh != null)
        {
            Bounds localBounds = mesh.bounds;
            box.center = localBounds.center;
            box.size = localBounds.size;
        }

        box.enabled = true;
        meshCollider.enabled = false;
    }
}
