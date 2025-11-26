using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Harvestable : MonoBehaviour
{
    [SerializeField] private Image progressImage;
    [SerializeField] private Material progressMaterial;
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private ProximityInteractionTrigger trigger;
    [SerializeField] private InteractableObject interactable;
    [SerializeField] private InventoryItem item;
    
    private Material originalMaterial;
    private Material instanceMaterial;
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    
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
            }

            progressImage.enabled = false;
        }
    }
    
    public void StartProgress()
    {
        trigger.enabled = false;
        progressImage.enabled = true;
        interactable.HideIndicator();
        StartCoroutine(FillProgressBar());
    }
    
    private IEnumerator FillProgressBar()
    {
        if (progressImage == null || instanceMaterial == null)
            yield break;
            
        // Switch to progress material
        progressImage.material = instanceMaterial;
        
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