using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Added for cleaner Regex usage

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
            
            // --- FIX START ---
            // Original: Replace(objectName, @"_LOD\d+$", "")
            // New: We remove the $ anchor so it removes _LOD0 regardless of where it is in the string
            // Example: "T1|t1_LOD0|Dupli|" becomes "T1|t1|Dupli|"
            string baseName = Regex.Replace(objectName, @"_LOD\d+.*", "");
            // --- FIX END ---
            
            if (!lodMeshes.ContainsKey(baseName))
                lodMeshes[baseName] = new List<Renderer>();
                
            lodMeshes[baseName].Add(renderer);
        }

        // Create LOD Groups for each set
        foreach (var lodSet in lodMeshes)
        {
            // Check if we have legitimate LODs (LOD0, LOD1, etc)
            // We filter here to ensure we don't group unrelated objects that just happened to match names
            if (lodSet.Value.Count > 1)
            {
                CreateLODGroup(lodSet.Value, lodSet.Key);
            }
        }
    }

    static void CreateLODGroup(List<Renderer> renderers, string baseName)
    {
        // Sort by LOD level
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
        
        Debug.Log($"Created LOD Group for '{baseName}' with {renderers.Count} levels on object '{parent.name}'");
    }

    static int ExtractLODLevel(string gameObjectName)
    {
        // --- FIX START ---
        // Original: Match(gameObjectName, @"_LOD(\d+)$")
        // New: Removed $ to find _LOD(number) anywhere in the name
        var match = Regex.Match(gameObjectName, @"_LOD(\d+)");
        // --- FIX END ---
        
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
        
        // Start with the first renderer's parent
        Transform current = renderers[0].transform.parent;
        
        while (current != null)
        {
            bool allIsChild = true;
            foreach(var r in renderers)
            {
                if (!r.transform.IsChildOf(current))
                {
                    allIsChild = false;
                    break;
                }
            }
            
            if(allIsChild) return current;
            
            current = current.parent;
        }
        
        return null;
    }

    static float CalculateLODHeight(int lodLevel, int totalLODs)
    {
        // Customize these values based on your needs
        switch (lodLevel)
        {
            case 0: return 0.6f; // LOD0 typically visible until 60%
            case 1: return 0.3f; // LOD1
            case 2: return 0.1f; // LOD2
            case 3: return 0.02f; // LOD3
            default: return 1.0f / (lodLevel + 2);
        }
    }
}