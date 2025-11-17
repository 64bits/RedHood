using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SimpleDirectionController))]
[RequireComponent(typeof(PlayerInput))]
/// <summary>
/// Reads input from the PlayerInput component's "Move" action, calculates
/// the world-space direction relative to the camera, and passes it to the
/// SimpleDirectionController.
/// This version uses standard camera-relative movement.
/// Logic is in Update() to avoid feedback loops with camera systems like Cinemachine.
/// </summary>
public class UserInputSource : MonoBehaviour
{
    [Tooltip("Time in seconds the input must be held before it's considered 'committed'.")]
    [SerializeField] private float commitThreshold = 0.1f;

    private SimpleDirectionController motionController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Transform mainCameraTransform;
    private Vector2 rawInputVector = Vector2.zero;
    
    // Time tracking for commitment
    private float inputHeldTime = 0f;
    private bool isInputActive = false;
    private bool hasCommitted = false; // Track if we've already committed this input

    private void Awake()
    {
        // RequireComponent ensures these will not be null
        motionController = GetComponent<SimpleDirectionController>();
        playerInput = GetComponent<PlayerInput>();

        // Get the Move action from the PlayerInput component
        moveAction = playerInput.actions["Move"];

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
        if (moveAction != null)
        {
            moveAction.Enable();
            // Subscribe to the action's performed event to capture the latest value
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Disable();
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