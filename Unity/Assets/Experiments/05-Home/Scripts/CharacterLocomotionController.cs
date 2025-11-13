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
    [SerializeField] private float pivotBlendInTime = 0.15f;
    [SerializeField] private float pivotBlendOutTime = 0.2f;
    
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
    
    // Movement state
    private Vector3 targetDirection;
    private Vector3 currentVelocity;
    private float movementTimer;
    private float lastInputTime;
    private bool isMoving;
    private float commitment;
    
    // Pivot state
    private float pivotWeight;
    private bool isPivoting;
    private float pivotTimer;
    private float lastAngle;
    private bool wasPivotBlendingIn;
    
    // Root motion
    private Vector3 rootMotionDelta;
    private Quaternion rootMotionRotation;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        
        // Ensure root motion is enabled
        animator.applyRootMotion = false; // We'll apply it manually
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
        
        // Check for pivot condition
        if (isMoving && commitment >= 1f && !isPivoting)
        {
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle));
            
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
        pivotTimer = 0f;
        wasPivotBlendingIn = true;
    }
    
    private void UpdatePivotLayer()
    {
        if (isPivoting)
        {
            pivotTimer += Time.deltaTime;
            
            if (wasPivotBlendingIn)
            {
                // Blend in
                pivotWeight = Mathf.Clamp01(pivotTimer / pivotBlendInTime);
                
                // Check if pivot animation is near completion
                AnimatorStateInfo pivotState = animator.GetCurrentAnimatorStateInfo(1);
                if (pivotState.normalizedTime >= 0.7f) // Start blending out at 70%
                {
                    wasPivotBlendingIn = false;
                    pivotTimer = 0f;
                }
            }
            else
            {
                // Blend out
                pivotWeight = 1f - Mathf.Clamp01(pivotTimer / pivotBlendOutTime);
                
                if (pivotWeight <= 0f)
                {
                    isPivoting = false;
                    pivotWeight = 0f;
                }
            }
        }
        else
        {
            pivotWeight = 0f;
        }
        
        // Apply pivot layer weight
        animator.SetLayerWeight(1, pivotWeight);
    }
    
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
        
        // Use slerp for micro-rotations to maintain smooth forward movement
        if (angle < microRotationThreshold && isMoving)
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