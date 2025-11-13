using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterLocomotionController))]
/// <summary>
/// Reads input from the new Unity Input System's "Move" action, calculates
/// the world-space direction relative to the camera, and passes it to the
/// CharacterLocomotionController.
/// This version uses standard camera-relative movement.
/// Logic is in Update() to avoid feedback loops with camera systems like Cinemachine.
/// </summary>
public class UserInputSource : MonoBehaviour
{
    // The Input Action Reference for the movement vector (usually WASD/Left Stick)
    [Tooltip("Assign the Input System Action used for 2D movement (e.g., 'Move').")]
    public InputActionReference moveActionReference;

    private CharacterLocomotionController motionController;
    private Transform mainCameraTransform;
    private Vector2 rawInputVector = Vector2.zero;

    // --- No state for locked directions is needed ---

    private void Awake()
    {
        // RequireComponent ensures this will not be null
        motionController = GetComponent<CharacterLocomotionController>();

        // Find and cache the main camera's transform
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("UserInputSource requires a Camera tagged 'MainCamera'.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (moveActionReference?.action != null)
        {
            moveActionReference.action.Enable();
            // Subscribe to the action's performed event to capture the latest value
            moveActionReference.action.performed += OnMovePerformed;
            moveActionReference.action.canceled += OnMoveCanceled;
        }
    }

    private void OnDisable()
    {
        if (moveActionReference?.action != null)
        {
            moveActionReference.action.performed -= OnMovePerformed;
            moveActionReference.action.canceled -= OnMoveCanceled;
            moveActionReference.action.Disable();
        }
    }

    // --- Input Handling Callbacks ---

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // Read the Vector2 value (e.g., (1, 0) for W, (0.5, -0.5) for diagonal stick)
        rawInputVector = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        // Stop movement when keys are released
        rawInputVector = Vector2.zero;
    }
    
    // --- No CacheDirectionVectors() needed ---

    // --- Movement Processing ---

    /// <summary>
    /// Changed from LateUpdate to Update.
    /// This ensures we read the camera's state *before* it is updated by
    /// systems like Cinemachine, which typically run after Update() or in LateUpdate().
    /// This breaks the "chasing" feedback loop that causes infinite rotation.
    /// </summary>
    private void Update()
    {
        if (motionController == null || mainCameraTransform == null) return;
        
        // 1. Get the camera's *current* forward and right vectors, flattened to the XZ plane
        Vector3 forward = mainCameraTransform.forward;
        Vector3 right = mainCameraTransform.right;

        // Ignore the Y component to keep movement strictly on the horizontal plane
        forward.y = 0f;
        right.y = 0f;
        
        // Normalize the vectors after flattening to ensure they are unit vectors
        forward.Normalize();
        right.Normalize();

        // 2. Calculate the world-space target direction
        // The input's Y component drives the camera's forward vector (Z world-axis)
        // The input's X component drives the camera's right vector (X world-axis)
        Vector3 targetDirection = (forward * rawInputVector.y) + (right * rawInputVector.x);

        // 3. Pass the resulting direction to the CharacterLocomotionController
        // We normalize the result to prevent diagonal movement from being faster (vector magnitude > 1)
        motionController.SetTargetDirection(targetDirection);
    }
}