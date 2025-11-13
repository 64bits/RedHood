using UnityEngine;

/// <summary>
/// Simple input controller that feeds direction to the locomotion controller
/// Can be replaced with your own input system
/// </summary>
[RequireComponent(typeof(CharacterLocomotionController))]
public class CharacterInputController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    
    private CharacterLocomotionController locomotionController;
    
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
    }
    
    private void Update()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Create input vector
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Normalize diagonal movement
            if (inputDir.magnitude > 1f)
            {
                inputDir.Normalize();
            }
            
            // Convert to world space relative to camera
            Vector3 worldDir = ConvertToWorldSpace(inputDir);
            
            // Send to locomotion controller
            locomotionController.SetTargetDirection(worldDir);
        }
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