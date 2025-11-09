// RootMotionController.cs
using UnityEngine;

[RequireComponent(typeof(Animator))]
/// <summary>
/// Handles the movement/animation processing based on a target direction
/// set externally (e.g., by a MovementInputSource).
/// </summary>
public class RootMotionController : MonoBehaviour
{
    public float forwardSmoothing = 3f;

    private Animator animator;
    private Vector3 currentDirection; // Target direction, set by external script
    private Vector3 logicalForward;   // Smoothed forward vector for rotation
    private bool isForwardFrozen = false; // Flag to freeze forward during pivots
    
    // PUBLIC ACCESSOR: The ONLY way for external scripts to influence movement
    public void SetTargetDirection(Vector3 direction)
    {
        currentDirection = direction;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        logicalForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
    }

    private void LateUpdate()
    {
        bool isMoving = currentDirection.sqrMagnitude > 0;

        if (!isMoving)
        {
            animator.SetBool("isMoving", false);
            animator.SetFloat("rotation", 0f);
            animator.SetFloat("absRotation", 0f);
            return;
        }

        animator.SetBool("isMoving", true);

        // Only smooth logical forward if not frozen (i.e., not during pivot)
        if (!isForwardFrozen)
        {
            Vector3 target = new Vector3(transform.forward.x, 0f, transform.forward.z);
            // logicalForward = Vector3.Slerp(logicalForward, target, forwardSmoothing * Time.deltaTime);
            logicalForward = target;
        }

        // --- Rotation Calculation and Animation Parameter Setting ---

        // Ensure y component is zero for 2D rotation on the XZ plane
        Vector3 flatLogicalForward = logicalForward;
        Vector3 flatTargetDir = currentDirection;
        flatLogicalForward.y = 0f;
        flatTargetDir.y = 0f;

        // Calculate signed angle between logical forward and target direction
        float angle = Vector3.SignedAngle(flatLogicalForward, flatTargetDir, Vector3.up);

        // Map angle to -1 to 1 for blend tree
        float rotationValue = 0f;
        if (angle < -135f) rotationValue = -1f;
        else if (angle < -45f) rotationValue = -0.5f;
        else if (angle > 135f) rotationValue = 1f;
        else if (angle > 45f) rotationValue = 0.5f;
        else rotationValue = Mathf.Clamp(angle / 45f, -1f, 1f);
        float fineRotation = Mathf.Clamp(angle / 180f, -1f, 1f);

        if  (rotationValue < 0.25f && rotationValue > -0.25f) {
            // Just consider it 0
            rotationValue = 0f;
        }

        animator.SetFloat("rotation", rotationValue);
        animator.SetFloat("absRotation", Mathf.Abs(rotationValue));
        animator.SetFloat("fineRotation", fineRotation);

        // Debug visualization (optional, for development)
        // Debug.DrawLine(transform.position, transform.position + logicalForward, Color.red);
        // Debug.DrawLine(transform.position, transform.position + currentDirection, Color.blue);
    }

    // PUBLIC METHODS - Called by Animation Events
    
    /// <summary>
    /// Call this at the START of a pivot animation to freeze logicalForward
    /// </summary>
    public void FreezeForward()
    {
        isForwardFrozen = true;
    }

    /// <summary>
    /// Call this at the END of a pivot animation to resume smoothing.
    /// This snaps logicalForward to the current transform.forward.
    /// </summary>
    public void ResumeForward()
    {
        // Snap logicalForward to current direction
        logicalForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        isForwardFrozen = false;
    }
}