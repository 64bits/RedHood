using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MonsterController : MonoBehaviour
{
    private enum MonsterState { Patrol, Chase, Attack, Search }

    [Header("References (Auto-Assigned if tag is 'Player')")]
    [SerializeField] private CrouchController playerController;
    [SerializeField] private Transform playerTransform;

    [Header("Senses")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 45f; 
    [SerializeField] private float stopAndAttackDistance = 1.5f;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    // FIX #2: Prevents jittering when very close to the player
    [SerializeField] private float rotationDeadZone = 0.5f; 

    [Header("Timing Settings")]
    [SerializeField] private float patrolWalkTime = 10f;
    [SerializeField] private float patrolIdleTime = 3f;
    [SerializeField] private float searchWalkTime = 10f;
    [SerializeField] private float searchLookTime = 3f;

    // State Variables
    private Animator animator;
    private MonsterState currentState;
    private float stateTimer;
    private int patrolStage = 0; 

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentState = MonsterState.Patrol;
    }

    // FIX #1: Find Player Automatically
    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerController = playerObj.GetComponent<CrouchController>();
            }
            else
            {
                Debug.LogError("MonsterController: No GameObject with tag 'Player' found!");
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return; // Safety check

        // 1. Check for Player Detection
        if (currentState != MonsterState.Attack)
        {
            CheckForPlayer();
        }

        // 2. Execute Logic
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
                RotateTowards(playerTransform.position);
                break;
        }
    }

    private void OnAnimatorMove()
    {
        if (animator)
        {
            Vector3 newPosition = transform.position;
            newPosition += animator.deltaPosition;
            transform.position = newPosition;
        }
    }

    // -------------------------------------------------------------------------
    // STATE LOGIC
    // -------------------------------------------------------------------------

    private void CheckForPlayer()
    {
        if (playerController == null || playerController.IsHidden()) return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= viewDistance)
        {
            if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
            {
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
        patrolStage = -1; 
    }

    private void HandleChase()
    {
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
            RotateTowards(playerTransform.position);
            animator.SetBool("IsWalking", true);
        }
    }

    private void StartAttack()
    {
        currentState = MonsterState.Attack;
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Swipe");
        Invoke(nameof(EndAttack), 1.5f); 
    }

    private void EndAttack()
    {
        if(currentState == MonsterState.Attack)
            currentState = MonsterState.Chase;
    }

    private void StartSearch()
    {
        currentState = MonsterState.Search;
        stateTimer = 0f;
        animator.SetBool("IsWalking", true);
    }

    private void HandleSearch()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer < searchWalkTime)
        {
            // Walk straight (no rotation)
            animator.SetBool("IsWalking", true);
        }
        else if (stateTimer < searchWalkTime + searchLookTime)
        {
            animator.SetBool("IsWalking", false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandlePatrol()
    {
        stateTimer += Time.deltaTime;

        switch (patrolStage)
        {
            case 0: // Walk 1
                animator.SetBool("IsWalking", true);
                if (stateTimer >= patrolWalkTime) NextPatrolStage();
                break;

            case 1: // Idle
                animator.SetBool("IsWalking", false);
                if (stateTimer >= patrolIdleTime) NextPatrolStage();
                break;

            case 2: // Turn
                float targetY = transform.eulerAngles.y + 180f;
                transform.rotation = Quaternion.Euler(0, targetY, 0);
                patrolStage++; 
                stateTimer = 0;
                break;

            case 3: // Walk 2
                animator.SetBool("IsWalking", true);
                if (stateTimer >= patrolWalkTime) NextPatrolStage();
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

    // FIX #2: Updated Rotation Logic with Dead Zone
    private void RotateTowards(Vector3 targetPosition)
    {
        // Calculate distance just for the rotation logic
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Only rotate if we are further away than the dead zone
        if (distanceToTarget > rotationDeadZone)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep rotation flat

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Visualize Dead Zone
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rotationDeadZone);

        Vector3 viewAngleLeft = Quaternion.Euler(0, -viewAngle, 0) * transform.forward;
        Vector3 viewAngleRight = Quaternion.Euler(0, viewAngle, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleLeft * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleRight * viewDistance);
    }
}