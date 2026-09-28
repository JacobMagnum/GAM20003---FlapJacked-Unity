using UnityEngine;
public class PrefabSpawnArea : MonoBehaviour
{
    private void Reset()
    {
        Collider area = GetComponent<Collider>();
        if (area != null)
        {
            area.isTrigger = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        SpawnedPrefab spawned = other.GetComponentInParent<SpawnedPrefab>();
        if (spawned == null && other.attachedRigidbody != null)
        {
            spawned = other.attachedRigidbody.GetComponentInParent<SpawnedPrefab>();
        }
        if (spawned != null)
        {
            spawned.NotifyLeftArea();
        }
    }
}