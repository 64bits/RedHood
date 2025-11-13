using UnityEngine;

/// <summary>
/// Main locomotion controller that orchestrates movement, commitment, and pivot animations
/// </summary>
[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class CharacterLocomotionController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 540f; // degrees per second
    
    [Header("Commitment Settings")]
    [SerializeField] private float commitmentThreshold = 0.15f; // time before committing to run
    [SerializeField] private float movementHysteresis = 0.1f; // grace period after input stops
    
    [Header("Pivot Settings")]
    [SerializeField] private float pivotAngleThreshold = 90f; // angle change to trigger pivot
    // Blending fields are removed
    
    [Header("Rotation Correction")]
    [SerializeField] private float microRotationThreshold = 15f; // angle within which we slerp instead of using root motion
    [SerializeField] private float slerpSpeed = 10f;
    
    // Components
    private Animator animator;
    private CharacterController characterController;
    
    // Animator parameters
    private static readonly int TurnAngleParam = Animator.StringToHash("TurnAngle");
    private static readonly int CommitmentParam = Animator.StringToHash("Commitment");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static readonly int IsPivotingParam = Animator.StringToHash("IsPivoting");
    
    // Movement state
    private Vector3 targetDirection;
    private Vector3 currentVelocity;
    private float movementTimer;
    private float lastInputTime = -32768;
    private bool isMoving;
    private float commitment;
    
    // Pivot state
    private float pivotWeight; // Will now be 0 or 1
    private bool isPivoting;
    private float lastAngle;
    
    // Root motion
    private Vector3 rootMotionDelta;
    private Quaternion rootMotionRotation;

    // New state flag for external control by StateMachineBehaviours
    public bool IsInRunLoop { get; private set; } 
    
    // Public setter for StateMachineBehaviour
    public void SetInRunLoop(bool isInRunLoop)
    {
        IsInRunLoop = isInRunLoop;
    }

    public void EndPivot()
    {
        isPivoting = false;
        pivotWeight = 0f;
        // Optional: Log to confirm it's called
        // Debug.Log($"EndPivot() called at {Time.time}. isPivoting: {isPivoting}");
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        
        // We'll apply root motion manually
        animator.applyRootMotion = false; 
    }
    
    private void Start()
    {
        targetDirection = transform.forward;
        lastAngle = 0f;
    }
    
    /// <summary>
    /// Public method to set target movement direction from input or AI
    /// </summary>
    public void SetTargetDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            targetDirection = direction.normalized;
            lastInputTime = Time.time;
        }
    }
    
    private void Update()
    {
        UpdateMovementState();
        UpdateCommitment();
        UpdateTurnAngle();
        UpdatePivotLayer();
        UpdateAnimatorParameters();
    }
    
    private void UpdateMovementState()
    {
        // Check if we have recent input
        float timeSinceInput = Time.time - lastInputTime;
        bool hasRecentInput = timeSinceInput < movementHysteresis;
        
        // Update moving state with hysteresis
        if (hasRecentInput)
        {
            if (!isMoving)
            {
                isMoving = true;
                movementTimer = 0f;
            }
            movementTimer += Time.deltaTime;
        }
        else
        {
            if (isMoving)
            {
                isMoving = false;
                movementTimer = 0f;
                commitment = 0f;
            }
        }
    }
    
    private void UpdateCommitment()
    {
        if (isMoving)
        {
            // Commitment builds up after threshold
            if (movementTimer >= commitmentThreshold)
            {
                commitment = 1f;
            }
            else
            {
                commitment = 0f;
            }
        }
        else
        {
            commitment = 0f;
        }
    }
    
    private void UpdateTurnAngle()
    {
        // Calculate angle between forward and target direction
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 flatTarget = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
        
        if (flatTarget.sqrMagnitude < 0.01f)
        {
            flatTarget = flatForward;
        }
        
        // Calculate signed angle
        float angle = Vector3.SignedAngle(flatForward, flatTarget, Vector3.up);
        
        // Check for pivot condition:
        // 1. We are committed to a run (commitment >= 1f)
        // 2. We are NOT already pivoting (!isPivoting)
        // 3. The RunLoopBehaviour has flagged that we are in the main run animation.
        if (commitment >= 1f && !isPivoting && IsInRunLoop)
        {
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) * Mathf.Rad2Deg; 

            if (angleDelta > pivotAngleThreshold)
            {
                TriggerPivot();
            }
        }
        
        lastAngle = angle;
    }
    
    private void TriggerPivot()
    {
        isPivoting = true;
        pivotWeight = 1f; // Set weight directly to 1
        animator.SetTrigger(IsPivotingParam);
        // Optional: Log to confirm it's called
        // Debug.Log($"TriggerPivot() called at {Time.time}. isPivoting: {isPivoting}");
    }
    
    //================================================================//
    // SIMPLIFIED PIVOT LAYER LOGIC
    //================================================================//
    private void UpdatePivotLayer()
    {
        // The weight is now managed by TriggerPivot() (sets to 1) 
        // and EndPivot() (sets to 0).
        // We just need to apply this value to the animator layer.
        animator.SetLayerWeight(1, pivotWeight);
    }
    //================================================================//
    
    private void UpdateAnimatorParameters()
    {
        // Calculate current turn angle
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 flatTarget = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
        float turnAngle = Vector3.SignedAngle(flatForward, flatTarget, Vector3.up);
        
        animator.SetFloat(TurnAngleParam, turnAngle);
        animator.SetFloat(CommitmentParam, commitment);
        animator.SetBool(IsMovingParam, isMoving);
    }
    
    private void OnAnimatorMove()
    {
        // Capture root motion
        rootMotionDelta = animator.deltaPosition;
        rootMotionRotation = animator.deltaRotation;
        
        // Apply movement
        if (characterController.enabled)
        {
            characterController.Move(rootMotionDelta);
            
            // Handle rotation
            ApplyRotation();
        }
        
        // Apply gravity
        if (!characterController.isGrounded)
        {
            characterController.Move(Physics.gravity * Time.deltaTime);
        }
    }
    
    private void ApplyRotation()
    {
        Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 flatTarget = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
        float angle = Vector3.Angle(flatForward, flatTarget);
        float adjustedThreshold = isPivoting ? microRotationThreshold * 2f : microRotationThreshold;

        // Use slerp for micro-rotations to maintain smooth forward movement
        if (angle < adjustedThreshold && isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatTarget);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                slerpSpeed * Time.deltaTime
            );
        }
        else
        {
            // Use root motion rotation
            transform.rotation *= rootMotionRotation;
        }
    }
    
    // Public getters for debugging
    public float CurrentCommitment => commitment;
    public bool IsMoving => isMoving;
    public float PivotWeight => pivotWeight;
    public float CurrentTurnAngle
    {
        get
        {
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 flatTarget = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
            return Vector3.SignedAngle(flatForward, flatTarget, Vector3.up);
        }
    }
}