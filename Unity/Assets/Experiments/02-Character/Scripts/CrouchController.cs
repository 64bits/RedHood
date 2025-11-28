using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInput))]
public class CrouchController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    
    [Header("Animation Settings")]
    [SerializeField] private int crouchLayerIndex = 2; // Adjust based on your animator layer
    [SerializeField] private float transitionSpeed = 5f; // Speed of weight transition
    
    [Header("Hiding Settings")]
    [SerializeField] private float hideDistanceThreshold = 0.02f; // Distance from collider center to be hidden
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    
    private PlayerInput playerInput;
    private InputAction crouchAction;
    private bool isCrouching = false;
    private float targetWeight = 0f;
    
    // Track nearby hiding spots
    private List<Collider> nearbyHidingSpots = new List<Collider>();
    private bool isHidden = false;
    
    private void Awake()
    {
        // Get the PlayerInput component and the Crouch action
        playerInput = GetComponent<PlayerInput>();
        crouchAction = playerInput.actions["Crouch"];
    }
    
    private void OnEnable()
    {
        // Enable the input action and subscribe to events
        if (crouchAction != null)
        {
            crouchAction.Enable();
            crouchAction.started += OnCrouchStarted;
            crouchAction.canceled += OnCrouchCanceled;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events and disable the input action
        if (crouchAction != null)
        {
            crouchAction.started -= OnCrouchStarted;
            crouchAction.canceled -= OnCrouchCanceled;
            crouchAction.Disable();
        }
    }
    
    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        // Start crouching when key is pressed
        isCrouching = true;
        targetWeight = 1f;
    }
    
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        // Stop crouching when key is released
        isCrouching = false;
        targetWeight = 0f;
        isHidden = false; // Can't be hidden if not crouching
    }
    
    private void Update()
    {
        // Smoothly transition the crouch layer weight to target
        if (animator != null)
        {
            float currentWeight = animator.GetLayerWeight(crouchLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * transitionSpeed);
            animator.SetLayerWeight(crouchLayerIndex, newWeight);
        }
        
        // Update hiding state
        UpdateHidingState();
    }
    
    private void UpdateHidingState()
    {
        isHidden = false;
        
        // Only check for hiding if we're crouching
        if (!isCrouching)
        {
            return;
        }
        
        // Check each nearby hiding spot
        foreach (Collider hideSpot in nearbyHidingSpots)
        {
            if (hideSpot == null) continue;
            
            // Calculate distance to the center of the collider
            float distanceToCenter = Vector3.Distance(transform.position, hideSpot.bounds.center);
            
            // Check if we're close enough to be hidden
            if (distanceToCenter <= hideDistanceThreshold)
            {
                isHidden = true;
                break; // No need to check further
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Add this collider to our list of nearby hiding spots
        // You can add a tag check here if you want only specific objects to be hiding spots
        // Example: if (other.CompareTag("HidingSpot"))
        if (!nearbyHidingSpots.Contains(other))
        {
            nearbyHidingSpots.Add(other);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Remove this collider from our list when we leave
        nearbyHidingSpots.Remove(other);
    }
    
    private void OnGUI()
    {
        if (!showDebugGUI) return;
        
        // Display debug information
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        string debugText = $"Crouching: {isCrouching}\n";
        debugText += $"Hidden: {isHidden}\n";
        debugText += $"Nearby Hiding Spots: {nearbyHidingSpots.Count}\n";
        debugText += $"Layer Weight: {(animator != null ? animator.GetLayerWeight(crouchLayerIndex).ToString("F2") : "N/A")}";
        
        // Draw background box
        GUI.Box(new Rect(10, 10, 300, 100), "");
        GUI.Label(new Rect(20, 20, 280, 80), debugText, style);
        
        // Visual indicator for hidden state
        if (isHidden)
        {
            GUIStyle hiddenStyle = new GUIStyle(GUI.skin.label);
            hiddenStyle.fontSize = 30;
            hiddenStyle.fontStyle = FontStyle.Bold;
            hiddenStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(Screen.width / 2 - 100, 50, 200, 50), "HIDDEN!", hiddenStyle);
        }
    }
    
    // Public getter for other scripts to check hiding state
    public bool IsHidden()
    {
        return isHidden;
    }
    
    public bool IsCrouching()
    {
        return isCrouching;
    }
    
    // Optional: Validate that references are set
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Clamp layer index to valid range
        if (animator != null && crouchLayerIndex >= animator.layerCount)
        {
            Debug.LogWarning($"Crouch layer index {crouchLayerIndex} is out of range. Animator has {animator.layerCount} layers.");
        }
    }
}