using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerInput))]
public class SpecialController : MonoBehaviour
{
    // Singleton instance
    public static SpecialController Instance { get; private set; }

    // Event that others can subscribe to
    public static event Action<bool> OnSpecialStateChanged;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject lanternObject;
    [SerializeField] private GameObject regularDomeObject;
    [SerializeField] private GameObject lanternDomeObject;

    [Header("Animation Settings")]
    [SerializeField] private int lanternLayerIndex = 1; // 2nd layer (0-indexed)
    [SerializeField] private float transitionSpeed = 5f; // Speed of weight transition

    private PlayerInput playerInput;
    private InputAction specialAction;
    private bool isSpecialEnabled = false;
    private float targetWeight = 0f;

    // Public property to check special state
    public bool IsSpecialEnabled => isSpecialEnabled;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Get the PlayerInput component and the Special action
        playerInput = GetComponent<PlayerInput>();
        specialAction = playerInput.actions["Special"];
    }

    private void OnDestroy()
    {
        // Clear singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        // Enable the input action and subscribe to performed event (for toggle)
        if (specialAction != null)
        {
            specialAction.Enable();
            specialAction.performed += OnSpecialToggle;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events and disable the input action
        if (specialAction != null)
        {
            specialAction.performed -= OnSpecialToggle;
            specialAction.Disable();
        }
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

        // Invoke the event to notify subscribers
        OnSpecialStateChanged?.Invoke(isSpecialEnabled);
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