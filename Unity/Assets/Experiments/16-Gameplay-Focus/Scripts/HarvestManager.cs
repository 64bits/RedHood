using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Singleton manager that handles entering/exiting the harvest interaction state.
/// Now includes integrated UI controller logic for handling harvest progress.
/// </summary>
public class HarvestManager : MonoBehaviour
{
    public static HarvestManager Instance { get; private set; }
    
    [Header("Input")]
    [SerializeField] private InputMapSwitcher inputMapSwitcher;
    [SerializeField] private InputActionReference interactAction;
    
    [Header("UI")]
    [SerializeField] private Canvas harvestCanvas;
    [SerializeField] private TMP_Text harvestTitle;
    [SerializeField] private TMP_Text harvestStatus;
    [SerializeField] private Image harvestIcon;
    [SerializeField] private Image progressImage;
    
    [Header("Harvest Settings")]
    [Tooltip("Duration to fill the progress bar")]
    [SerializeField] private float fillDuration = 2f;
    
    [Header("Events")]
    public UnityEvent onEnterInteraction;
    public UnityEvent onExitInteraction;
    
    // State
    private bool isInteracting;
    private bool isHarvesting;
    private HarvestPoint currentHarvestPoint;
    private HarvestPopulation currentHarvestPopulation;
    private Material instanceMaterial;
    
    // Shader property ID
    private static readonly int Progress = Shader.PropertyToID("_Progress");
    
    public bool IsInteracting => isInteracting;
    
    /// <summary>
    /// Provides access to the data of the resource currently being harvested.
    /// </summary>
    public HarvestPoint CurrentHarvestPoint => currentHarvestPoint;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        // Clone the existing material on the image to create a unique instance
        if (progressImage != null && progressImage.material != null)
        {
            instanceMaterial = new Material(progressImage.material);
            progressImage.material = instanceMaterial;
            instanceMaterial.SetFloat(Progress, 0f);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Progress image or its material is not assigned!", this);
        }
    }
    
    private void Start()
    {
        if (inputMapSwitcher == null)
            inputMapSwitcher = FindFirstObjectByType<InputMapSwitcher>();
        
        SetHarvestCanvasActive(false);
    }
    
    private void OnEnable()
    {
        InputMapSwitcher.OnExitUIMode += HandleUIModeClosed;
        
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Interact InputActionReference is not assigned or invalid!", this);
        }
    }
    
    private void OnDisable()
    {
        InputMapSwitcher.OnExitUIMode -= HandleUIModeClosed;
        
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
        }
        
        // Stop any ongoing harvest when disabled
        if (isHarvesting)
        {
            StopAllCoroutines();
            ResetProgress();
        }
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Only start harvest if not already harvesting and we're in interaction mode
        if (!isHarvesting && isInteracting)
        {
            StartCoroutine(HarvestRoutine());
        }
    }

    public void SetHarvestPopulation(HarvestPopulation population)
    {
        currentHarvestPopulation = population;

        harvestStatus.text = population.getPopulationValueText();
    }
    
    /// <summary>
    /// Enters the harvest interaction state using specific resource data.
    /// </summary>
    /// <param name="point">The ScriptableObject containing title, icon, and item data.</param>
    public void EnterInteraction(HarvestPoint point)
    {
        if (isInteracting || point == null) return;
        
        isInteracting = true;
        currentHarvestPoint = point;
        harvestTitle.text = point.harvestTitle;
        harvestIcon.sprite = point.harvestIcon;
        
        ResetProgress();
        SetHarvestCanvasActive(true);
        
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToUIMap();
        }
        
        onEnterInteraction?.Invoke();
        Debug.Log($"Started harvesting: {point.harvestTitle}");
    }
    
    public void ExitInteraction()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        currentHarvestPoint = null;
        
        // Stop any ongoing harvest
        if (isHarvesting)
        {
            StopAllCoroutines();
            isHarvesting = false;
            ResetProgress();
        }
        
        SetHarvestCanvasActive(false);
        
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToPlayerMap();
        }
        
        onExitInteraction?.Invoke();
    }
    
    private void HandleUIModeClosed()
    {
        if (isInteracting)
        {
            ExitInteraction();
        }
    }
    
    private IEnumerator HarvestRoutine()
    {
        if (progressImage == null || instanceMaterial == null || currentHarvestPoint == null)
        {
            Debug.LogError("Progress image, material, or harvest point not available!");
            yield break;
        }

        isHarvesting = true;
        
        // Reset progress
        instanceMaterial.SetFloat(Progress, 0f);

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fillDuration);
            
            // Update the shader's progress
            instanceMaterial.SetFloat(Progress, progress);
            yield return null;
        }

        // Ensure it's fully filled
        instanceMaterial.SetFloat(Progress, 1f);

        // Brief pause at full
        yield return new WaitForSeconds(0.1f);

        // Grant the item from the current harvest point
        if (currentHarvestPoint.itemToDispense != null && InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.AddItem(currentHarvestPoint.itemToDispense, 1))
            {
                Debug.Log($"Picked up 1x {currentHarvestPoint.itemToDispense.itemName}");
            }
        }
        else
        {
            Debug.LogWarning("Item or InventoryManager not available!");
        }

        // Reset for next use
        ResetProgress();
        isHarvesting = false;

        currentHarvestPopulation.populationAbundance -= 1;
        currentHarvestPopulation.UpdatePopulationVisibility();
        harvestStatus.text = currentHarvestPopulation.getPopulationValueText();
        
        // Exit harvest interaction after completion
        // ExitInteraction();
    }
    
    private void ResetProgress()
    {
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(Progress, 0f);
        }
    }
    
    private void SetHarvestCanvasActive(bool active)
    {
        if (harvestCanvas != null) harvestCanvas.gameObject.SetActive(active);
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