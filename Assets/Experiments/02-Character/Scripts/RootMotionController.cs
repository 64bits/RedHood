using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionController : MonoBehaviour
{
    [System.Serializable]
    public struct MoveCommand
    {
        public Vector3 direction; // World-space direction
        public float duration;    // Duration to hold this movement
    }

    public float forwardSmoothing = 3f;
    public MoveCommand[] commands;

    private Animator animator;
    private int currentCommandIndex = 0;
    private Vector3 currentDirection;
    private bool isMoving = false;
    private Vector3 logicalForward; // Smoothed forward vector for rotation
    private bool isForwardFrozen = false; // Flag to freeze forward during pivots

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        logicalForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (commands.Length > 0)
            StartCoroutine(ProcessCommands());
    }

    private IEnumerator ProcessCommands()
    {
        while (true) // Loop indefinitely
        {
            MoveCommand cmd = commands[currentCommandIndex];
            currentDirection = cmd.direction.normalized;
            isMoving = currentDirection.sqrMagnitude > 0;

            float timer = 0f;
            while (timer < cmd.duration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Advance to next command, wrap around if at end
            currentCommandIndex = (currentCommandIndex + 1) % commands.Length;
        }
    }

    private void LateUpdate()
    {
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

        Debug.DrawLine(transform.position, transform.position + logicalForward, Color.red);
        Debug.DrawLine(transform.position, transform.position + currentDirection, Color.blue);

        // Ensure y component is zero for 2D rotation on the XZ plane
        Vector3 flatLogicalForward = logicalForward;
        Vector3 flatTargetDir = currentDirection;
        flatLogicalForward.y = 0f;
        flatTargetDir.y = 0f;

        // Calculate signed angle between logical forward and target direction
        float angle = Vector3.SignedAngle(flatLogicalForward, flatTargetDir, Vector3.up);

        // Map angle to -1 to 1 for blend tree
        float rotationValue = 0f;
        if (angle < -135f) rotationValue = -1f;        // Hood_180_Left
        else if (angle < -45f) rotationValue = -0.5f;  // Hood_90_Left
        else if (angle > 135f) rotationValue = 1f;     // Hood_180_Right
        else if (angle > 45f) rotationValue = 0.5f;    // Hood_90_Right
        else rotationValue = Mathf.Clamp(angle / 45f, -1f, 1f); // Soft adjustments
        float fineRotation = Mathf.Clamp(angle / 180f, -1f, 1f);

        animator.SetFloat("rotation", rotationValue);
        animator.SetFloat("absRotation", Mathf.Abs(rotationValue));
        animator.SetFloat("fineRotation", fineRotation);
    }

    // PUBLIC METHODS - Called by Animation Events
    
    /// <summary>
    /// Call this at the START of a pivot animation to freeze logicalForward
    /// </summary>
    public void FreezeForward()
    {
        //isForwardFrozen = true;
    }

    /// <summary>
    /// Call this at the END of a pivot animation to resume smoothing.
    /// This snaps logicalForward to the current transform.forward.
    /// </summary>
    public void ResumeForward()
    {
        // Snap logicalForward to current direction
        //logicalForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        //isForwardFrozen = false;
    }
}