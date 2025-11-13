using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple input controller that feeds direction to the locomotion controller
/// Using Unity's new Input System
/// </summary>
[RequireComponent(typeof(CharacterLocomotionController))]
public class CharacterInputController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    
    private CharacterLocomotionController locomotionController;
    private Vector2 currentInput;
    
    private void Awake()
    {
        locomotionController = GetComponent<CharacterLocomotionController>();
        
        // Auto-find main camera if not set
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }
        
        // Enable input actions
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }
    
    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
        }
    }
    
    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
        }
        
        // Clear input when disabled
        currentInput = Vector2.zero;
        locomotionController.SetTargetDirection(Vector3.zero);
    }
    
    private void Update()
    {
        if (currentInput.sqrMagnitude > 0.01f)
        {
            // Normalize diagonal movement
            Vector2 inputDir = currentInput;
            if (inputDir.magnitude > 1f)
            {
                inputDir.Normalize();
            }
            
            // Convert to world space relative to camera
            Vector3 worldDir = ConvertToWorldSpace(new Vector3(inputDir.x, 0f, inputDir.y));
            
            // Send to locomotion controller
            locomotionController.SetTargetDirection(worldDir);
        }
        else
        {
            // Stop movement when no input
            locomotionController.SetTargetDirection(Vector3.zero);
        }
    }
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentInput = Vector2.zero;
    }
    
    private Vector3 ConvertToWorldSpace(Vector3 inputDir)
    {
        if (cameraTransform == null)
        {
            return inputDir;
        }
        
        // Get camera forward and right (flattened on Y)
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();
        
        // Calculate world space direction
        Vector3 worldDir = (camForward * inputDir.z + camRight * inputDir.x);
        return worldDir.normalized;
    }
    
    // Optional: Visualize input direction in editor
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && locomotionController != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, 
                locomotionController.transform.forward * 2f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f,
                locomotionController.transform.forward * locomotionController.CurrentCommitment * 2f);
        }
    }
}