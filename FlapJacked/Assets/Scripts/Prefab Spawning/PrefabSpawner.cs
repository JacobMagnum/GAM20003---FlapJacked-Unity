using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int spawnPointsPerPrefab = 2;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool randomYaw;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;

    private GameObject[] slotInstances;
    private bool[] respawnPending;

    private void Start()
    {
        int slotCount = SlotCount;
        slotInstances = new GameObject[slotCount];
        respawnPending = new bool[slotCount];

        if (!spawnOnStart)
        {
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            SpawnAtSlot(i);
        }
    }

    public void NotifyInstanceRemoved()
    {
        RequestRespawn(0);
    }
    public void NotifyInstanceRemoved(int slotIndex)
    {
        {
            RequestRespawn(slotIndex);
        }
    }

    public void RequestRespawn(int slotIndex)
    {
        if (slotInstances == null || respawnPending == null)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex >= slotInstances.Length)
        {
            return;
        }

        if (respawnPending[slotIndex])
        {
            return;
        }

        slotInstances[slotIndex] = null;
        respawnPending[slotIndex] = true;
        StartCoroutine(RespawnSlotAfterDelay(slotIndex));
    }
    public void SpawnAtSlot(int slotIndex)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }
        ensureArrays();
        if (slotIndex < 0 || slotIndex >= slotInstances.Length)
        {
            return;
        }

        if (slotInstances[slotIndex] != null)
        {
            return;
        }

        GameObject prefab = PrefabForSlot(slotIndex);
        if (prefab == null)
        {
            return;
        }

        Transform point = SpawnPointForSlot(slotIndex);
        if (point == null)
        {
           point = transform;
        }
        Quaternion rotation = randomYaw
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * point.rotation
            : point.rotation;

        GameObject instance = Instantiate(prefab, point.position, rotation);
        SpawnedPrefab[] spawnedItems = instance.GetComponentsInChildren<SpawnedPrefab>(true);
        if (spawnedItems.Length == 0)
        {
            spawnedItems = new[] { instance.AddComponent<SpawnedPrefab>() };
        }
        bool consumedDuringSpawn = false;
        for (int i = 0; i < spawnedItems.Length; i++)
        {
           spawnedItems[i].Initialize(this, slotIndex);
            if (spawnedItems[i].gameObject == null)
            {
                consumedDuringSpawn = true;
            }
        }

        if (!consumedDuringSpawn)
        {
            slotInstances[slotIndex] = instance;
            respawnPending[slotIndex] = false;
        }
    }

    private IEnumerator RespawnSlotAfterDelay(int slotIndex)
    {
        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        respawnPending[slotIndex] = false;
        SpawnAtSlot(slotIndex);
    }
    private void ensureArrays()
    {
        int slotCount = SlotCount;
        if (slotInstances != null && slotInstances.Length == slotCount)
        {
            return;
        }
        slotInstances = new GameObject[slotCount];
        respawnPending = new bool[slotCount];
    }

    private int SlotCount
    {
        get
        {
            int points = spawnPoints != null ? spawnPoints.Length : 0;
            if (points > 0)
            {
                return points;
            }

            return prefabs != null ? prefabs.Length : 0;
        }
    }

    private GameObject PrefabForSlot(int slotIndex)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }

        int perPrefab = Mathf.Max(1, spawnPointsPerPrefab);
        int prefabIndex = slotIndex / perPrefab;
        if (prefabIndex >= prefabs.Length)
        {
            prefabIndex %= prefabs.Length;
        }

        return prefabs[prefabIndex];
    }

    private Transform SpawnPointForSlot(int slotIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform;
        }

        if (slotIndex < 0 || slotIndex >= spawnPoints.Length)
        {
            return transform;
        }

        Transform point = spawnPoints[slotIndex];
        return point != null ? point : transform;
    }
}
