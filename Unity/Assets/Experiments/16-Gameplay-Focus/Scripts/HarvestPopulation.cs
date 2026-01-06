using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class HarvestPopulation : MonoBehaviour
{
    [Header("Abundance Settings")]
    [Range(0, 3)]
    [SerializeField] public int populationAbundance = 3;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float radiusOffset = 0f;
    [Tooltip("Amount subtracted from radius at the poles (max height), 0 at the equator.")]
    [SerializeField] private float insetFactor = 0f;

    [Header("Height Settings")]
    [SerializeField] private float cardinalHeight = 0f;
    [SerializeField] private float intermediateHeight = 0f;
    [SerializeField] private float ultraIntermediateHeight = 0f;

    private SphereCollider sphereCollider;

    // Lists to track spawned objects for runtime toggling
    private List<GameObject> cardinalInstances = new List<GameObject>();
    private List<GameObject> intermediateInstances = new List<GameObject>();
    private List<GameObject> ultraInstances = new List<GameObject>();

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        if (prefabToSpawn == null) return;
        SpawnAllPoints();
        UpdatePopulationVisibility();
    }

    // Allows the slider to update the scene immediately while the game is running or in editor
    private void OnValidate()
    {
        if (Application.isPlaying && cardinalInstances.Count > 0)
        {
            UpdatePopulationVisibility();
        }
    }

    public string getPopulationValueText()
    {
        return populationAbundance switch
        {
            0 => "Depleted",
            1 => "Sparse",
            2 => "Abundant",
            3 => "Flourishing",
            _ => "Unknown"
        };
    }

    public void UpdatePopulationVisibility()
    {
        bool showCardinal = populationAbundance >= 1;
        bool showIntermediate = populationAbundance == 3;
        bool showUltra = populationAbundance >= 2;

        ToggleGroup(cardinalInstances, showCardinal);
        ToggleGroup(intermediateInstances, showIntermediate);
        ToggleGroup(ultraInstances, showUltra);
    }

    private void ToggleGroup(List<GameObject> instances, bool state)
    {
        foreach (var obj in instances)
        {
            if (obj != null) obj.SetActive(state);
        }
    }

    private void SpawnAllPoints()
    {
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float baseRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float totalRadius = baseRadius + radiusOffset;

        SpawnGroup(center, totalRadius, GetCardinalDirections(), cardinalHeight, cardinalInstances);
        SpawnGroup(center, totalRadius, GetIntermediateDirections(), intermediateHeight, intermediateInstances);
        SpawnGroup(center, totalRadius, GetUltraIntermediateDirections(), ultraIntermediateHeight, ultraInstances);
    }

    private void SpawnGroup(Vector3 center, float totalRadius, Vector3[] directions, float heightOffset, List<GameObject> registry)
    {
        foreach (Vector3 dir in directions)
        {
            Vector3 spawnPos = CalculatePosition(center, totalRadius, dir, heightOffset);
            Vector3 lookDirection = (spawnPos - center).normalized;
            Quaternion rotation = (lookDirection != Vector3.zero) ? Quaternion.LookRotation(lookDirection) : Quaternion.identity;

            GameObject instance = Instantiate(prefabToSpawn, spawnPos, rotation, transform);
            registry.Add(instance);
        }
    }

    private Vector3 CalculatePosition(Vector3 center, float totalRadius, Vector3 direction, float heightOffset)
    {
        float t = Mathf.Clamp01(Mathf.Abs(heightOffset) / totalRadius);
        float effectiveRadius = totalRadius - (t * insetFactor);
        float clampedHeight = Mathf.Clamp(heightOffset, -effectiveRadius, effectiveRadius);
        float horizontalRadius = Mathf.Sqrt(Mathf.Max(0, (effectiveRadius * effectiveRadius) - (clampedHeight * clampedHeight)));

        return center + (direction * horizontalRadius) + (Vector3.up * clampedHeight);
    }

    #region Direction Definitions
    private Vector3[] GetCardinalDirections() => new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    private Vector3[] GetIntermediateDirections() => new Vector3[] {
        (Vector3.forward + Vector3.right).normalized, (Vector3.back + Vector3.right).normalized,
        (Vector3.back + Vector3.left).normalized, (Vector3.forward + Vector3.left).normalized
    };

    private Vector3[] GetUltraIntermediateDirections()
    {
        Vector3 n = Vector3.forward; Vector3 e = Vector3.right;
        Vector3 s = Vector3.back; Vector3 w = Vector3.left;
        Vector3 ne = (n + e).normalized; Vector3 se = (s + e).normalized;
        Vector3 sw = (s + w).normalized; Vector3 nw = (n + w).normalized;
        return new Vector3[] {
            (n + ne).normalized, (e + ne).normalized, (e + se).normalized, (s + se).normalized,
            (s + sw).normalized, (w + sw).normalized, (w + nw).normalized, (n + nw).normalized
        };
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null) return;

        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float baseRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float totalRadius = baseRadius + radiusOffset;

        Gizmos.color = Color.red;
        DrawDirectionalGizmos(center, totalRadius, GetCardinalDirections(), cardinalHeight);

        Gizmos.color = Color.yellow;
        DrawDirectionalGizmos(center, totalRadius, GetIntermediateDirections(), intermediateHeight);

        Gizmos.color = Color.cyan;
        DrawDirectionalGizmos(center, totalRadius, GetUltraIntermediateDirections(), ultraIntermediateHeight);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, totalRadius);
    }

    private void DrawDirectionalGizmos(Vector3 center, float totalRadius, Vector3[] directions, float heightOffset)
    {
        foreach (Vector3 dir in directions)
        {
            Gizmos.DrawSphere(CalculatePosition(center, totalRadius, dir, heightOffset), 0.1f);
        }
    }
}