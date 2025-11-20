using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BeaconWallGenerator : EditorWindow
{
    public float wallRadius = 10f;
    public float wallHeight = 5f;
    public int segmentsPerFullCircle = 64; // Resolution quality
    public Material wallMaterial;
    public string beaconTag = "Beacon";
    
    [MenuItem("Tools/Generate Beacon Walls")]
    static void Init()
    {
        BeaconWallGenerator window = (BeaconWallGenerator)EditorWindow.GetWindow(typeof(BeaconWallGenerator));
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Beacon Wall Generator", EditorStyles.boldLabel);
        
        wallRadius = EditorGUILayout.FloatField("Wall Radius", wallRadius);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        segmentsPerFullCircle = EditorGUILayout.IntSlider("Resolution", segmentsPerFullCircle, 16, 128);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false);
        beaconTag = EditorGUILayout.TextField("Beacon Tag", beaconTag);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Unified Wall"))
        {
            GenerateWalls();
        }
        
        if (GUILayout.Button("Clear Walls"))
        {
            ClearWalls();
        }
    }
    
    // Helper struct to manage angular ranges (in radians)
    struct AngleRange
    {
        public float start;
        public float end;
    }

    void GenerateWalls()
    {
        ClearWalls();

        GameObject[] beacons = GameObject.FindGameObjectsWithTag(beaconTag);
        if (beacons.Length == 0) return;

        // Prepare lists for the final combined mesh
        List<Vector3> allVertices = new List<Vector3>();
        List<int> allTriangles = new List<int>();
        List<Vector2> allUVs = new List<Vector2>();

        // 1. Process each beacon
        foreach (GameObject currentBeacon in beacons)
        {
            Vector3 center = currentBeacon.transform.position;
            
            // We work on the XZ plane, ignoring Y for intersection checks
            Vector2 center2D = new Vector2(center.x, center.z);

            // List of angular intervals that are INSIDE other circles (Blocked)
            List<AngleRange> blockedRanges = new List<AngleRange>();

            foreach (GameObject otherBeacon in beacons)
            {
                if (currentBeacon == otherBeacon) continue;

                Vector2 other2D = new Vector2(otherBeacon.transform.position.x, otherBeacon.transform.position.z);
                float dist = Vector2.Distance(center2D, other2D);

                // If circles overlap (distance < 2 * radius) and aren't identical
                if (dist < wallRadius * 2 && dist > 0.0001f)
                {
                    // Calculate the angle toward the other beacon
                    Vector2 dir = other2D - center2D;
                    float angleToOther = Mathf.Atan2(dir.y, dir.x); // Result is -PI to PI

                    // Calculate the spread of the intersection (Law of Cosines logic)
                    // This is half the angular width of the chord intersection
                    float intersectionSpread = Mathf.Acos(dist / (2 * wallRadius));

                    // The blocked range is centered on the neighbor, +/- the spread
                    float startAngle = angleToOther - intersectionSpread;
                    float endAngle = angleToOther + intersectionSpread;

                    blockedRanges.Add(new AngleRange { start = startAngle, end = endAngle });
                }
            }

            // 2. Invert Blocked Ranges to get Active Ranges (the walls we actually draw)
            List<AngleRange> activeRanges = GetActiveRanges(blockedRanges);

            // 3. Build geometry for this specific beacon's active arcs
            foreach (var range in activeRanges)
            {
                AddArcToMesh(center, range.start, range.end, allVertices, allTriangles, allUVs);
            }
        }

        // 4. Create the single combined object
        CreateMeshObject(allVertices, allTriangles, allUVs);
    }

    // Converts a list of "bad" angles into a list of "good" angles
    List<AngleRange> GetActiveRanges(List<AngleRange> blocked)
    {
        // Normalize all angles to 0 - 2PI range for easier sorting
        List<AngleRange> normalizedBlocked = new List<AngleRange>();
        foreach (var b in blocked)
        {
            float s = NormalizeAngle(b.start);
            float e = NormalizeAngle(b.end);

            if (s > e) // The range wraps around 0/360 (e.g. 350 to 10)
            {
                normalizedBlocked.Add(new AngleRange { start = s, end = Mathf.PI * 2 });
                normalizedBlocked.Add(new AngleRange { start = 0, end = e });
            }
            else
            {
                normalizedBlocked.Add(new AngleRange { start = s, end = e });
            }
        }

        // Sort by start angle
        normalizedBlocked = normalizedBlocked.OrderBy(x => x.start).ToList();

        // Merge overlapping blocked ranges
        List<AngleRange> mergedBlocked = new List<AngleRange>();
        if (normalizedBlocked.Count > 0)
        {
            var current = normalizedBlocked[0];
            for (int i = 1; i < normalizedBlocked.Count; i++)
            {
                if (normalizedBlocked[i].start < current.end) // Overlap
                {
                    current.end = Mathf.Max(current.end, normalizedBlocked[i].end);
                }
                else
                {
                    mergedBlocked.Add(current);
                    current = normalizedBlocked[i];
                }
            }
            mergedBlocked.Add(current);
        }

        // Now invert: Start with full circle, subtract blocked
        List<AngleRange> active = new List<AngleRange>();
        float currentAngle = 0f;

        foreach (var block in mergedBlocked)
        {
            if (currentAngle < block.start)
            {
                active.Add(new AngleRange { start = currentAngle, end = block.start });
            }
            currentAngle = Mathf.Max(currentAngle, block.end);
        }

        // Add remaining slice if any
        if (currentAngle < Mathf.PI * 2)
        {
            active.Add(new AngleRange { start = currentAngle, end = Mathf.PI * 2 });
        }

        return active;
    }

    void AddArcToMesh(Vector3 center, float startAngle, float endAngle, List<Vector3> verts, List<int> tris, List<Vector2> uvs)
    {
        float totalAngle = endAngle - startAngle;
        
        // Calculate how many segments we need based on arc length
        int segments = Mathf.CeilToInt(Mathf.Abs(totalAngle) / (Mathf.PI * 2) * segmentsPerFullCircle);
        segments = Mathf.Max(segments, 1); // At least one segment

        int startIndex = verts.Count;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            
            // Math functions use Radians
            float x = Mathf.Cos(angle) * wallRadius;
            float z = Mathf.Sin(angle) * wallRadius;

            Vector3 pointOnCircle = center + new Vector3(x, 0, z);
            
            // Bottom Vertex
            verts.Add(pointOnCircle - Vector3.up * (wallHeight / 2));
            // Top Vertex
            verts.Add(pointOnCircle + Vector3.up * (wallHeight / 2));

            // UVs (simple tiling based on angle)
            float u = angle * wallRadius / 2f; 
            uvs.Add(new Vector2(u, 0));
            uvs.Add(new Vector2(u, 1));

            // Add triangles (if not the last loop)
            if (i < segments)
            {
                int currentBase = startIndex + (i * 2);
                
                // --- FIXED WINDING ORDER HERE ---
                // Previously: currentBase, currentBase + 1, currentBase + 2
                // Now swapped to: currentBase, currentBase + 2, currentBase + 1
                
                // Triangle 1
                tris.Add(currentBase);
                tris.Add(currentBase + 2);
                tris.Add(currentBase + 1);

                // Triangle 2
                // Previously: currentBase + 1, currentBase + 3, currentBase + 2
                // Now swapped to: currentBase + 1, currentBase + 2, currentBase + 3
                tris.Add(currentBase + 1);
                tris.Add(currentBase + 2);
                tris.Add(currentBase + 3);
            }
        }
    }

    void CreateMeshObject(List<Vector3> verts, List<int> tris, List<Vector2> uvs)
    {
        GameObject wallParent = new GameObject("BeaconWalls_Unified");
        Undo.RegisterCreatedObjectUndo(wallParent, "Create Beacon Walls");

        Mesh mesh = new Mesh();
        // Handle > 65k vertices for large generated sets
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = wallParent.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = wallParent.AddComponent<MeshRenderer>();
        mr.sharedMaterial = wallMaterial;

        MeshCollider mc = wallParent.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    float NormalizeAngle(float angle)
    {
        angle = angle % (Mathf.PI * 2);
        if (angle < 0) angle += Mathf.PI * 2;
        return angle;
    }

    void ClearWalls()
    {
        GameObject existing = GameObject.Find("BeaconWalls_Unified");
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        
        // Also clean up old versions from previous script
        GameObject old = GameObject.Find("BeaconWalls");
        if (old != null) Undo.DestroyObjectImmediate(old);
    }
}