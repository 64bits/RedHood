using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SimpleDirectionController))]
/// <summary>
/// Reads input from the new Unity Input System's "Move" action, calculates
/// the world-space direction relative to the camera, and passes it to the
/// SimpleDirectionController.
/// This version uses standard camera-relative movement.
/// Logic is in Update() to avoid feedback loops with camera systems like Cinemachine.
/// </summary>
public class UserInputSource : MonoBehaviour
{
    // The Input Action Reference for the movement vector (usually WASD/Left Stick)
    [Tooltip("Assign the Input System Action used for 2D movement (e.g., 'Move').")]
    public InputActionReference moveActionReference;

    [Tooltip("Time in seconds the input must be held before it's considered 'committed'.")]
    [SerializeField] private float commitThreshold = 0.1f;

    private SimpleDirectionController motionController;
    private Transform mainCameraTransform;
    private Vector2 rawInputVector = Vector2.zero;
    
    // Time tracking for commitment
    private float inputHeldTime = 0f;
    private bool isInputActive = false;
    private bool hasCommitted = false; // Track if we've already committed this input

    private void Awake()
    {
        // RequireComponent ensures this will not be null
        motionController = GetComponent<SimpleDirectionController>();

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
        
        // Start tracking held time if input just became active
        if (!isInputActive)
        {
            isInputActive = true;
            inputHeldTime = 0f;
            hasCommitted = false;
        }
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Vector3 forward = mainCameraTransform.forward;
        Vector3 right = mainCameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 targetDirection = (forward * rawInputVector.y) + (right * rawInputVector.x);
        motionController.SetTargetDirection(targetDirection, false);
        
        // Stop movement when keys are released
        rawInputVector = Vector2.zero;
        isInputActive = false;
        inputHeldTime = 0f;
        hasCommitted = false;
    }

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
        
        // Update held time if input is active
        if (isInputActive)
        {
            inputHeldTime += Time.deltaTime;
            
            // Only call SetTargetDirection once we've crossed the commit threshold
            if (!hasCommitted && inputHeldTime >= commitThreshold)
            {
                hasCommitted = true;
                
                // Calculate and send the committed direction
                Vector3 forward = mainCameraTransform.forward;
                Vector3 right = mainCameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                Vector3 targetDirection = (forward * rawInputVector.y) + (right * rawInputVector.x);
                motionController.SetTargetDirection(targetDirection, true);
            }
            else if (hasCommitted)
            {
                // Continue updating direction after commitment
                Vector3 forward = mainCameraTransform.forward;
                Vector3 right = mainCameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                Vector3 targetDirection = (forward * rawInputVector.y) + (right * rawInputVector.x);
                motionController.SetTargetDirection(targetDirection, true);
            }
        }
    }
}