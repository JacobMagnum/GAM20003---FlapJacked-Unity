using UnityEngine;
public class SpawnedPrefab : MonoBehaviour
{
    private PrefabSpawner spawner;
    private int slotIndex =-1;
    private bool consumed;
    private float readyTime;

    public bool isConsumed => consumed;

    public void Bind(PrefabSpawner owner)
    {
        Initialize(owner, 0);
    }
    
    public void Bind(PrefabSpawner owner, int spawnSlot)
    {
        Initialize(owner, spawnSlot);
    }

    public void Initialize(PrefabSpawner owner, int spawnSlot)
    {
        spawner = owner;
        slotIndex = spawnSlot;
        consumed = false;
        readyTime = Time.time + 0.15f;
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
}
