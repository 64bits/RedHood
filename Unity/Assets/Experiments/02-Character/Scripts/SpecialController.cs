using UnityEngine;
using UnityEngine.InputSystem;

public class SpecialController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject lanternObject;
    [SerializeField] private GameObject regularDomeObject;
    [SerializeField] private GameObject lanternDomeObject;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionReference specialAction;
    
    [Header("Animation Settings")]
    [SerializeField] private int lanternLayerIndex = 1; // 2nd layer (0-indexed)
    [SerializeField] private float transitionSpeed = 5f; // Speed of weight transition
    
    private bool isSpecialEnabled = false;
    private float targetWeight = 0f;
    
    private void OnEnable()
    {
        // Enable the input action and subscribe to performed event (for toggle)
        specialAction.action.Enable();
        specialAction.action.performed += OnSpecialToggle;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events and disable the input action
        specialAction.action.performed -= OnSpecialToggle;
        specialAction.action.Disable();
    }
    
    private void OnSpecialToggle(InputAction.CallbackContext context)
    {
        // Toggle the special state
        isSpecialEnabled = !isSpecialEnabled;
        
        if (isSpecialEnabled)
        {
            EnableSpecial();
        }
        else
        {
            DisableSpecial();
        }
    }
    
    private void EnableSpecial()
    {
        // Set target weight to 1 for smooth transition
        targetWeight = 1f;
        
        // Enable the lantern GameObject
        if (lanternObject != null)
        {
            lanternObject.SetActive(true);
        }

        if (lanternDomeObject != null)
        {
            lanternDomeObject.SetActive(true);
        }

        if (regularDomeObject != null)
        {
            regularDomeObject.SetActive(false);
        }
    }
    
    private void DisableSpecial()
    {
        // Set target weight to 0 for smooth transition
        targetWeight = 0f;
        
        // Disable the lantern GameObject
        if (lanternObject != null)
        {
            lanternObject.SetActive(false);
        }

        if (lanternDomeObject != null)
        {
            lanternDomeObject.SetActive(false);
        }

        if (regularDomeObject != null)
        {
            regularDomeObject.SetActive(true);
        }
    }
    
    private void Update()
    {
        // Smoothly transition the layer weight to target
        if (animator != null)
        {
            float currentWeight = animator.GetLayerWeight(lanternLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * transitionSpeed);
            animator.SetLayerWeight(lanternLayerIndex, newWeight);
        }
    }
    
    // Optional: Validate that references are set
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Clamp layer index to valid range
        if (animator != null && lanternLayerIndex >= animator.layerCount)
        {
            Debug.LogWarning($"Lantern layer index {lanternLayerIndex} is out of range. Animator has {animator.layerCount} layers.");
        }
    }
}