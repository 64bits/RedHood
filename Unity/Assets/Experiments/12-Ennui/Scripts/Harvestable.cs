using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Harvestable : MonoBehaviour
{
    [Header("Harvest Settings")]
    [SerializeField] private Image progressImage;
    [SerializeField] private Material progressMaterial;
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private ProximityInteractionTrigger trigger;
    [SerializeField] private InteractableObject interactable;
    [SerializeField] private InventoryItem item;
    
    [Header("Time of Day Restrictions")]
    [SerializeField] private bool harvestableDuringDay = true;
    [SerializeField] private bool harvestableDuringDusk = true;
    [SerializeField] private bool harvestableDuringNight = false;
    [SerializeField] private bool harvestableDuringDawn = true;
    [SerializeField] private float xMarkDisplayDuration = 1f;

    private Material originalMaterial;
    private Material instanceMaterial;
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int ShowXMark = Shader.PropertyToID("_ShowXMark");
    
    private DayTimeManager dayTimeManager;

    private void Awake()
    {
        if (progressImage != null)
        {
            // Store the original material
            originalMaterial = progressImage.material;
            
            // Create a unique instance of the progress material
            if (progressMaterial != null)
            {
                instanceMaterial = new Material(progressMaterial);
                instanceMaterial.SetFloat(FillAmount, 0f);
                instanceMaterial.SetFloat(ShowXMark, 0f);
            }
            
            progressImage.enabled = false;
        }
        
        // Find the DayTimeManager in the scene
        dayTimeManager = FindFirstObjectByType<DayTimeManager>();
        if (dayTimeManager == null)
        {
            Debug.LogWarning("DayTimeManager not found in scene. Harvestable will work at all times.");
        }
    }

    private void OnEnable()
    {
        if (dayTimeManager != null)
        {
            dayTimeManager.OnTimeOfDayChanged += OnTimeOfDayChanged;
        }
    }

    private void OnDisable()
    {
        if (dayTimeManager != null)
        {
            dayTimeManager.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }
    }

    private void OnTimeOfDayChanged(DayTimeManager.TimeOfDay newTimeOfDay)
    {
        // Optional: Add visual feedback or update UI when time changes
    }

    public void StartProgress()
    {
        // Check if harvesting is allowed at current time of day
        if (!CanHarvestNow())
        {
            StartCoroutine(ShowXMarkRoutine());
            return;
        }
        
        trigger.enabled = false;
        progressImage.enabled = true;
        interactable.HideIndicator();
        StartCoroutine(FillProgressBar());
    }

    private bool CanHarvestNow()
    {
        if (dayTimeManager == null)
        {
            return true; // If no manager, allow harvesting anytime
        }
        
        DayTimeManager.TimeOfDay currentTime = dayTimeManager.GetCurrentTimeOfDay();
        
        switch (currentTime)
        {
            case DayTimeManager.TimeOfDay.Day:
                return harvestableDuringDay;
            case DayTimeManager.TimeOfDay.Dusk:
                return harvestableDuringDusk;
            case DayTimeManager.TimeOfDay.Night:
                return harvestableDuringNight;
            case DayTimeManager.TimeOfDay.Dawn:
                return harvestableDuringDawn;
            default:
                return true;
        }
    }

    private IEnumerator ShowXMarkRoutine()
    {
        if (progressImage == null || instanceMaterial == null)
            yield break;
        
        progressImage.enabled = true;
        progressImage.material = instanceMaterial;
        
        // Show the X mark
        instanceMaterial.SetFloat(ShowXMark, 1f);
        
        yield return new WaitForSeconds(xMarkDisplayDuration);
        
        // Hide the X mark
        instanceMaterial.SetFloat(ShowXMark, 0f);
        progressImage.material = originalMaterial;
        progressImage.enabled = false;
    }

    private IEnumerator FillProgressBar()
    {
        if (progressImage == null || instanceMaterial == null)
            yield break;

        // Switch to progress material
        progressImage.material = instanceMaterial;
        instanceMaterial.SetFloat(ShowXMark, 0f);

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fillDuration);
            
            // Update the shader's fill amount
            instanceMaterial.SetFloat(FillAmount, progress);
            yield return null;
        }

        // Ensure it's fully filled
        instanceMaterial.SetFloat(FillAmount, 1f);

        // Brief pause at full
        yield return new WaitForSeconds(0.1f);

        // Switch back to original material
        progressImage.material = originalMaterial;

        // Give me the item
        if (InventoryManager.Instance.AddItem(item, 1))
        {
            Debug.Log($"Picked up 1x {item.itemName}");
        }

        // Reset for next use
        instanceMaterial.SetFloat(FillAmount, 0f);
        progressImage.enabled = false;
    }

    private void OnDestroy()
    {
        // Clean up the material instance
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}