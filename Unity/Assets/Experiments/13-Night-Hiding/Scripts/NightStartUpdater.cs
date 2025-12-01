using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NightStartUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayTimeManager dayTimeManager;
    [SerializeField] private SpawnManager spawnManager;
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private int numberOfSpawns = 2;
    [SerializeField] private int minDistanceBetweenSpawns = 2; // Grid cells apart
    
    [Header("Spawned Objects Tracking")]
    [SerializeField] private List<GameObject> spawnedObjects = new List<GameObject>();
    
    private void Start()
    {
        // Find managers if not assigned
        if (dayTimeManager == null)
            dayTimeManager = FindObjectOfType<DayTimeManager>();
        
        if (spawnManager == null)
            spawnManager = FindObjectOfType<SpawnManager>();
        
        // Validate references
        if (dayTimeManager != null)
        {
            dayTimeManager.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        }
        else
        {
            Debug.LogError("DayTimeManager not found in scene!");
        }
        
        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager not found in scene!");
        }
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No prefab assigned to spawn!");
        }
    }
    
    private void OnDestroy()
    {
        if (dayTimeManager != null)
        {
            dayTimeManager.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        }
    }
    
    private void HandleTimeOfDayChanged(DayTimeManager.TimeOfDay newTimeOfDay)
    {
        if (newTimeOfDay == DayTimeManager.TimeOfDay.Night)
        {
            Debug.Log("Night Started");
            SpawnPrefabsAtNonConsecutivePoints();
        }
    }
    
    private void SpawnPrefabsAtNonConsecutivePoints()
    {
        if (spawnManager == null || prefabToSpawn == null)
        {
            Debug.LogWarning("Cannot spawn: Missing SpawnManager or Prefab reference");
            return;
        }
        
        // Clear any previously spawned objects
        ClearSpawnedObjects();
        
        // Get all enabled spawn points
        List<Vector3> enabledPoints = spawnManager.GetAllEnabledSpawnPoints();
        
        if (enabledPoints.Count == 0)
        {
            Debug.LogWarning("No enabled spawn points available");
            return;
        }
        
        // Get non-consecutive spawn positions
        List<Vector3> selectedPositions = GetNonConsecutiveSpawnPoints(enabledPoints, numberOfSpawns);
        
        // Spawn prefabs at selected positions
        foreach (Vector3 position in selectedPositions)
        {
            GameObject spawned = Instantiate(prefabToSpawn, position, Quaternion.identity);
            spawnedObjects.Add(spawned);
        }
        
        Debug.Log($"Spawned {spawnedObjects.Count} objects at non-consecutive spawn points");
    }
    
    private List<Vector3> GetNonConsecutiveSpawnPoints(List<Vector3> availablePoints, int count)
    {
        List<Vector3> selectedPoints = new List<Vector3>();
        List<Vector3> remainingPoints = new List<Vector3>(availablePoints);
        
        // Clamp count to available points
        count = Mathf.Min(count, availablePoints.Count);
        
        while (selectedPoints.Count < count && remainingPoints.Count > 0)
        {
            // Pick a random point from remaining
            int randomIndex = Random.Range(0, remainingPoints.Count);
            Vector3 selectedPoint = remainingPoints[randomIndex];
            
            selectedPoints.Add(selectedPoint);
            
            // Remove the selected point and nearby points based on minimum distance
            remainingPoints.RemoveAll(point => 
                Vector3.Distance(point, selectedPoint) < minDistanceBetweenSpawns);
        }
        
        if (selectedPoints.Count < count)
        {
            Debug.LogWarning($"Could only find {selectedPoints.Count} non-consecutive spawn points (requested {count})");
        }
        
        return selectedPoints;
    }
    
    private void ClearSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
    
    [ContextMenu("Test Spawn")]
    public void TestSpawn()
    {
        SpawnPrefabsAtNonConsecutivePoints();
    }
    
    [ContextMenu("Clear Spawned Objects")]
    public void TestClear()
    {
        ClearSpawnedObjects();
    }
}