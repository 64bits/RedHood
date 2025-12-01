using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnManager : MonoBehaviour
{
    [Header("Grid Bounds")]
    [SerializeField] private float minX = -10f;
    [SerializeField] private float minZ = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float maxZ = 10f;
    
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float padding = 1f;
    
    [Header("Gizmo Settings")]
    [SerializeField] private float gizmoRadius = 0.5f;
    [SerializeField] private Color enabledColor = Color.green;
    [SerializeField] private Color disabledColor = Color.red;
    
    [Header("Spawn Points")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    
    private int previousGridSize;

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 position;
        public bool enabled = true;
        
        public SpawnPoint(Vector3 pos, bool isEnabled = true)
        {
            position = pos;
            enabled = isEnabled;
        }
    }

    private void OnValidate()
    {
        // Regenerate grid if grid size changes
        if (previousGridSize != gridSize)
        {
            GenerateSpawnPoints();
            previousGridSize = gridSize;
        }
    }

    [ContextMenu("Generate Spawn Points")]
    public void GenerateSpawnPoints()
    {
        spawnPoints.Clear();
        
        if (gridSize <= 0)
        {
            Debug.LogWarning("Grid size must be greater than 0");
            return;
        }
        
        // Calculate the usable area after padding
        float usableWidth = (maxX - minX) - (2 * padding);
        float usableDepth = (maxZ - minZ) - (2 * padding);
        
        if (usableWidth <= 0 || usableDepth <= 0)
        {
            Debug.LogWarning("Padding is too large for the given bounds");
            return;
        }
        
        // Calculate spacing between points
        float spacingX = gridSize > 1 ? usableWidth / (gridSize - 1) : 0;
        float spacingZ = gridSize > 1 ? usableDepth / (gridSize - 1) : 0;
        
        // Generate grid points
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                float posX = minX + padding + (x * spacingX);
                float posZ = minZ + padding + (z * spacingZ);
                
                Vector3 spawnPos = new Vector3(posX, transform.position.y, posZ);
                spawnPoints.Add(new SpawnPoint(spawnPos, true));
            }
        }
        
        Debug.Log($"Generated {spawnPoints.Count} spawn points in a {gridSize}x{gridSize} grid");
    }

    public Vector3 GetRandomEnabledSpawnPoint()
    {
        List<Vector3> enabledPositions = new List<Vector3>();
        
        foreach (var sp in spawnPoints)
        {
            if (sp.enabled)
                enabledPositions.Add(sp.position);
        }
        
        if (enabledPositions.Count == 0)
        {
            Debug.LogWarning("No enabled spawn points available");
            return transform.position;
        }
        
        return enabledPositions[Random.Range(0, enabledPositions.Count)];
    }

    public List<Vector3> GetAllEnabledSpawnPoints()
    {
        List<Vector3> enabledPositions = new List<Vector3>();
        
        foreach (var sp in spawnPoints)
        {
            if (sp.enabled)
                enabledPositions.Add(sp.position);
        }
        
        return enabledPositions;
    }

    private void OnDrawGizmos()
    {
        // Draw boundary box
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2, transform.position.y, (minZ + maxZ) / 2);
        Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);
        
        // Draw padding boundary
        Gizmos.color = Color.cyan;
        Vector3 paddedCenter = center;
        Vector3 paddedSize = new Vector3(size.x - (2 * padding), 0.1f, size.z - (2 * padding));
        Gizmos.DrawWireCube(paddedCenter, paddedSize);
        
        // Draw spawn points
        foreach (var sp in spawnPoints)
        {
            Gizmos.color = sp.enabled ? enabledColor : disabledColor;
            Gizmos.DrawSphere(sp.position, gizmoRadius);
            
            // Draw a smaller wire sphere for better visibility
            Gizmos.DrawWireSphere(sp.position, gizmoRadius);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpawnManager))]
public class SpawnManagerEditor : Editor
{
    private SpawnManager spawnManager;
    
    private void OnEnable()
    {
        spawnManager = (SpawnManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Generate Spawn Points", GUILayout.Height(30)))
        {
            spawnManager.GenerateSpawnPoints();
        }
        
        EditorGUILayout.Space(10);
        
        // Get spawn points via reflection since it's private
        SerializedProperty spawnPointsProp = serializedObject.FindProperty("spawnPoints");
        SerializedProperty gridSizeProp = serializedObject.FindProperty("gridSize");
        
        int gridSize = gridSizeProp.intValue;
        
        if (spawnPointsProp.arraySize > 0)
        {
            EditorGUILayout.LabelField("Spawn Point Grid", EditorStyles.boldLabel);
            
            // Add buttons for enable/disable all
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All"))
            {
                for (int i = 0; i < spawnPointsProp.arraySize; i++)
                {
                    spawnPointsProp.GetArrayElementAtIndex(i).FindPropertyRelative("enabled").boolValue = true;
                }
                serializedObject.ApplyModifiedProperties();
            }
            if (GUILayout.Button("Disable All"))
            {
                for (int i = 0; i < spawnPointsProp.arraySize; i++)
                {
                    spawnPointsProp.GetArrayElementAtIndex(i).FindPropertyRelative("enabled").boolValue = false;
                }
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Draw grid of checkboxes
            int expectedCount = gridSize * gridSize;
            if (spawnPointsProp.arraySize == expectedCount)
            {
                for (int z = gridSize - 1; z >= 0; z--) // Start from top for visual consistency
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    for (int x = 0; x < gridSize; x++)
                    {
                        int index = z * gridSize + x;
                        SerializedProperty spawnPoint = spawnPointsProp.GetArrayElementAtIndex(index);
                        SerializedProperty enabledProp = spawnPoint.FindPropertyRelative("enabled");
                        
                        bool newValue = EditorGUILayout.Toggle(enabledProp.boolValue, GUILayout.Width(20));
                        enabledProp.boolValue = newValue;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Generate spawn points to see the grid", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Total Spawn Points: {spawnPointsProp.arraySize}");
            
            // Count enabled points
            int enabledCount = 0;
            for (int i = 0; i < spawnPointsProp.arraySize; i++)
            {
                if (spawnPointsProp.GetArrayElementAtIndex(i).FindPropertyRelative("enabled").boolValue)
                    enabledCount++;
            }
            EditorGUILayout.LabelField($"Enabled: {enabledCount} | Disabled: {spawnPointsProp.arraySize - enabledCount}");
        }
    }
}
#endif