using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles UI input during harvest interaction mode.
/// Listens for the "Interact" action (E key) in UI mode to trigger harvest progress.
/// 
/// SETUP:
/// 1. Attach to the harvest canvas GameObject (or a child).
/// 2. Assign the progress Image, progress Material, and InventoryItem.
/// 3. Ensure the Input System's UI action map has an "Interact" action bound to "E".
/// </summary>
public class HarvestUIController : MonoBehaviour
{
    [Header("Harvest Settings")]
    [Tooltip("UI Image that will display the radial progress")]
    [SerializeField] private Image progressImage;
    
    [Tooltip("Duration to fill the progress bar")]
    [SerializeField] private float fillDuration = 2f;
    
    [Tooltip("Item to grant upon completion")]
    [SerializeField] private InventoryItem item;
    
    [Header("Input Action")]
    [Tooltip("Reference to the UI Interact action (should be bound to 'E')")]
    [SerializeField] private InputActionReference interactAction;
    
    private Material instanceMaterial;
    private bool isHarvesting = false;
    
    // Shader property ID
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private void Awake()
    {
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

    private void OnEnable()
    {
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
        // Only start harvest if not already harvesting
        if (!isHarvesting && HarvestManager.Instance != null && HarvestManager.Instance.IsInteracting)
        {
            StartCoroutine(HarvestRoutine());
        }
    }

    private IEnumerator HarvestRoutine()
    {
        if (progressImage == null || instanceMaterial == null)
        {
            Debug.LogError("Progress image or material not assigned!");
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

        // Grant the item
        if (item != null && InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.AddItem(item, 1))
            {
                Debug.Log($"Picked up 1x {item.itemName}");
            }
        }
        else
        {
            Debug.LogWarning("Item or InventoryManager not available!");
        }

        // Reset for next use
        ResetProgress();
        
        // Exit harvest interaction
        if (HarvestManager.Instance != null)
        {
            HarvestManager.Instance.ExitInteraction();
        }
        
        isHarvesting = false;
    }

    private void ResetProgress()
    {
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(Progress, 0f);
        }
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