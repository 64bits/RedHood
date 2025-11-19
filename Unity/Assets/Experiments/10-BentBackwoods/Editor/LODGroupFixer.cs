using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LODGroupFixer : EditorWindow
{
    [MenuItem("Tools/Fix LOD Groups")]
    public static void ShowWindow()
    {
        GetWindow<LODGroupFixer>("LOD Group Fixer");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Fix Selected Objects LOD Groups"))
        {
            FixLODGroups();
        }
    }

    [MenuItem("GameObject/Fix LOD Groups", false, 0)]
    static void FixLODGroups()
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            FixLODGroupForObject(selected);
        }
    }

    static void FixLODGroupForObject(GameObject parentObject)
    {
        // Find all LOD meshes under this parent
        Dictionary<string, List<Renderer>> lodMeshes = new Dictionary<string, List<Renderer>>();
        
        // Collect all renderers and group by base name
        Renderer[] allRenderers = parentObject.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            string objectName = renderer.gameObject.name;
            
            // Extract base name (remove LOD suffix)
            string baseName = System.Text.RegularExpressions.Regex.Replace(objectName, @"_LOD\d+$", "");
            
            if (!lodMeshes.ContainsKey(baseName))
                lodMeshes[baseName] = new List<Renderer>();
                
            lodMeshes[baseName].Add(renderer);
        }

        // Create LOD Groups for each set
        foreach (var lodSet in lodMeshes)
        {
            if (lodSet.Value.Count > 1)
            {
                CreateLODGroup(lodSet.Value, lodSet.Key);
            }
        }
    }

    static void CreateLODGroup(List<Renderer> renderers, string baseName)
    {
        // Sort by LOD level (assumes naming convention with _LOD0, _LOD1, etc.)
        renderers.Sort((a, b) => 
        {
            int lodA = ExtractLODLevel(a.gameObject.name);
            int lodB = ExtractLODLevel(b.gameObject.name);
            return lodA.CompareTo(lodB);
        });

        // Find common parent or use first renderer's parent
        Transform parent = FindCommonParent(renderers);
        
        if (parent == null) return;

        // Remove existing LOD Group
        LODGroup existingLOD = parent.GetComponent<LODGroup>();
        if (existingLOD != null)
            DestroyImmediate(existingLOD);

        // Create new LOD Group
        LODGroup lodGroup = parent.gameObject.AddComponent<LODGroup>();
        
        // Create LOD levels
        LOD[] lods = new LOD[renderers.Count];
        
        for (int i = 0; i < renderers.Count; i++)
        {
            float screenRelativeHeight = CalculateLODHeight(i, renderers.Count);
            lods[i] = new LOD(screenRelativeHeight, new Renderer[] { renderers[i] });
        }
        
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
        
        Debug.Log($"Created LOD Group for {baseName} with {renderers.Count} levels");
    }

    static int ExtractLODLevel(string gameObjectName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(gameObjectName, @"_LOD(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    static Transform FindCommonParent(List<Renderer> renderers)
    {
        if (renderers.Count == 0) return null;
        
        Transform commonParent = renderers[0].transform.parent;
        foreach (var renderer in renderers)
        {
            if (renderer.transform.parent != commonParent)
            {
                // If not all share same parent, use the highest common parent
                return FindHighestCommonParent(renderers);
            }
        }
        return commonParent;
    }

    static Transform FindHighestCommonParent(List<Renderer> renderers)
    {
        if (renderers.Count == 0) return null;
        
        List<Transform> parents = new List<Transform>();
        foreach (var renderer in renderers)
        {
            parents.Add(renderer.transform.parent);
        }
        
        // This is simplified - you might want more sophisticated common parent finding
        return parents[0];
    }

    static float CalculateLODHeight(int lodLevel, int totalLODs)
    {
        // Customize these values based on your needs
        switch (lodLevel)
        {
            case 0: return 0.5f; // LOD0: 50% screen height
            case 1: return 0.3f; // LOD1: 30% screen height  
            case 2: return 0.15f; // LOD2: 15% screen height
            case 3: return 0.05f; // LOD3: 5% screen height
            default: return 1.0f / (lodLevel + 2);
        }
    }
}