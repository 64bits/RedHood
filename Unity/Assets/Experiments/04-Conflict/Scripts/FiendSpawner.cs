using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiendSpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnData
    {
        [Tooltip("The GameObject prefab to spawn")]
        public GameObject prefab;
        
        [Tooltip("Position relative to the spawner's transform")]
        public Vector3 relativePosition;
        
        [Tooltip("Delay in seconds before spawning this object")]
        public float delay;
        
        [Tooltip("Time to live in seconds (when the object will be destroyed)")]
        public float ttl;
    }

    [Header("Spawn Configuration")]
    [SerializeField] private SpawnData[] spawnDataArray;
    
    [Header("Playback Settings")]
    [SerializeField] private bool playOnAwake = true;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Coroutine spawnRoutine;
    private bool isPlaying = false;

    private void Awake()
    {
        if (playOnAwake)
        {
            TriggerSpawn();
        }
    }

    /// <summary>
    /// Manually trigger the spawn sequence
    /// </summary>
    public void TriggerSpawn()
    {
        if (isPlaying)
        {
            Debug.LogWarning("FiendSpawner: Spawn sequence is already running!");
            return;
        }

        if (spawnDataArray == null || spawnDataArray.Length == 0)
        {
            Debug.LogWarning("FiendSpawner: No spawn data configured!");
            return;
        }

        spawnRoutine = StartCoroutine(SpawnSequence());
    }

    /// <summary>
    /// Stop the current spawn sequence and clean up spawned objects
    /// </summary>
    public void StopSpawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        CleanupSpawnedObjects();
        isPlaying = false;
    }

    private IEnumerator SpawnSequence()
    {
        isPlaying = true;
        CleanupSpawnedObjects();

        foreach (SpawnData data in spawnDataArray)
        {
            if (data.prefab == null)
            {
                Debug.LogWarning("FiendSpawner: Null prefab in spawn data, skipping...");
                continue;
            }

            // Wait for the delay
            if (data.delay > 0)
            {
                yield return new WaitForSeconds(data.delay);
            }

            // Spawn the object at the relative position
            Vector3 worldPosition = transform.position + transform.TransformDirection(data.relativePosition);
            GameObject spawnedObj = Instantiate(data.prefab, worldPosition, transform.rotation);
            spawnedObjects.Add(spawnedObj);

            // Start despawn coroutine for this object
            if (data.ttl > 0)
            {
                StartCoroutine(DespawnAfterTTL(spawnedObj, data.ttl));
            }
        }

        isPlaying = false;
    }

    private IEnumerator DespawnAfterTTL(GameObject obj, float ttl)
    {
        yield return new WaitForSeconds(ttl);

        if (obj != null)
        {
            spawnedObjects.Remove(obj);
            Destroy(obj);
        }
    }

    private void CleanupSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    private void OnDestroy()
    {
        CleanupSpawnedObjects();
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnDataArray == null) return;

        Gizmos.color = Color.yellow;
        foreach (SpawnData data in spawnDataArray)
        {
            if (data.prefab != null)
            {
                Vector3 worldPosition = transform.position + transform.TransformDirection(data.relativePosition);
                Gizmos.DrawWireSphere(worldPosition, 0.3f);
                Gizmos.DrawLine(transform.position, worldPosition);
            }
        }
    }
}