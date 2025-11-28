using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class MonsterController : MonoBehaviour
{
    private enum MonsterState { Patrol, Chase, Attack, Search }

    [Header("References")]
    [SerializeField] private CrouchController playerController;
    [SerializeField] private Transform playerTransform;

    [Header("Senses")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 45f; // Half angle (45 means 90 degree cone)
    [SerializeField] private float stopAndAttackDistance = 1.5f;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Timing Settings")]
    [SerializeField] private float patrolWalkTime = 10f;
    [SerializeField] private float patrolIdleTime = 3f;
    [SerializeField] private float searchWalkTime = 10f;
    [SerializeField] private float searchLookTime = 3f;

    // State Variables
    private Animator animator;
    private MonsterState currentState;
    private float stateTimer;
    private int patrolStage = 0; // 0=Walk1, 1=Idle, 2=Turn, 3=Walk2, 4=Despawn

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentState = MonsterState.Patrol;
    }

    private void Update()
    {
        // 1. Check for Player Detection (Overrides everything unless attacking)
        if (currentState != MonsterState.Attack)
        {
            CheckForPlayer();
        }

        // 2. Execute Logic based on current state
        switch (currentState)
        {
            case MonsterState.Patrol:
                HandlePatrol();
                break;
            case MonsterState.Chase:
                HandleChase();
                break;
            case MonsterState.Search:
                HandleSearch();
                break;
            case MonsterState.Attack:
                // Logic handled by Animation Events or Coroutines mostly, 
                // but we ensure we face the player here
                RotateTowards(playerTransform.position);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // ROOT MOTION HANDLING
    // -------------------------------------------------------------------------
    private void OnAnimatorMove()
    {
        // This callback overrides standard Transform movement with Animation movement
        if (animator)
        {
            Vector3 newPosition = transform.position;
            newPosition += animator.deltaPosition;
            transform.position = newPosition;
            
            // Apply root rotation if desired, or let script handle rotation (preferred for AI)
            // transform.rotation *= animator.deltaRotation; 
        }
    }

    // -------------------------------------------------------------------------
    // STATE LOGIC
    // -------------------------------------------------------------------------

    private void CheckForPlayer()
    {
        if (playerController == null) return;

        // If the player is hidden, we cannot "See" them to start a chase.
        // If we were ALREADY chasing and they hide, HandleChase() manages that transition.
        if (playerController.IsHidden()) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 1. Check Distance
        if (distanceToPlayer <= viewDistance)
        {
            // 2. Check Angle (Cone)
            if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
            {
                // Optional: Add Raycast here to check for walls
                
                // If we see them, switch to chase immediately
                if (currentState != MonsterState.Chase && currentState != MonsterState.Attack)
                {
                    StartChase();
                }
            }
        }
    }

    private void StartChase()
    {
        currentState = MonsterState.Chase;
        animator.SetBool("IsWalking", true);
        patrolStage = -1; // Break out of patrol logic
    }

    private void HandleChase()
    {
        // If player goes hidden while we are chasing
        if (playerController.IsHidden())
        {
            StartSearch();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= stopAndAttackDistance)
        {
            StartAttack();
        }
        else
        {
            // Move and Rotate
            RotateTowards(playerTransform.position);
            animator.SetBool("IsWalking", true);
        }
    }

    private void StartAttack()
    {
        currentState = MonsterState.Attack;
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Swipe");
        
        // Return to chase after a delay approx length of animation
        // Ideally done via Animation Event, but Invoke is okay for simple setup
        Invoke(nameof(EndAttack), 1.5f); 
    }

    private void EndAttack()
    {
        if(currentState == MonsterState.Attack)
            currentState = MonsterState.Chase;
    }

    private void StartSearch()
    {
        // "Monster will walk in its current direction"
        currentState = MonsterState.Search;
        stateTimer = 0f;
        animator.SetBool("IsWalking", true);
    }

    private void HandleSearch()
    {
        stateTimer += Time.deltaTime;

        // Phase 1: Walk current direction (10s)
        if (stateTimer < searchWalkTime)
        {
            animator.SetBool("IsWalking", true);
            // Do not rotate, keep walking straight
        }
        // Phase 2: Look around (3s)
        else if (stateTimer < searchWalkTime + searchLookTime)
        {
            animator.SetBool("IsWalking", false);
            // Optional: You could add a slow rotation here to simulate "Looking around"
        }
        // Phase 3: Despawn
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandlePatrol()
    {
        // patrolStage mapping:
        // 0: Walk 10s
        // 1: Idle 3s
        // 2: Turn Around
        // 3: Walk 10s
        // 4: Despawn

        stateTimer += Time.deltaTime;

        switch (patrolStage)
        {
            case 0: // Walk 1
                animator.SetBool("IsWalking", true);
                if (stateTimer >= patrolWalkTime)
                {
                    NextPatrolStage();
                }
                break;

            case 1: // Idle
                animator.SetBool("IsWalking", false);
                if (stateTimer >= patrolIdleTime)
                {
                    NextPatrolStage();
                }
                break;

            case 2: // Turn Around
                // We perform the turn logic instantly or over time, then advance
                float targetY = transform.eulerAngles.y + 180f;
                transform.rotation = Quaternion.Euler(0, targetY, 0);
                
                // Immediately go to next stage after turn
                patrolStage++; 
                stateTimer = 0;
                break;

            case 3: // Walk 2
                animator.SetBool("IsWalking", true);
                if (stateTimer >= patrolWalkTime)
                {
                    NextPatrolStage();
                }
                break;

            case 4: // Despawn
                Destroy(gameObject);
                break;
        }
    }

    private void NextPatrolStage()
    {
        patrolStage++;
        stateTimer = 0f;
    }

    // -------------------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------------------

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep rotation flat on Y axis

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Visual Debugging for the Cone of View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 viewAngleLeft = Quaternion.Euler(0, -viewAngle, 0) * transform.forward;
        Vector3 viewAngleRight = Quaternion.Euler(0, viewAngle, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleLeft * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleRight * viewDistance);
    }
}