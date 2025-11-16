using UnityEngine;

/// <summary>
/// A simplified controller that sets an integer on the Animator based on
/// a target direction. This is used for a standing 8-way directional blend.
/// </summary>
[RequireComponent(typeof(Animator))]
public class SimpleDirectionController : MonoBehaviour
{
    private Animator animator;
    private bool hasTarget;
    private Vector3 currentTargetDirection;

    // This is the parameter we will set on the animator controller.
    private static readonly int TargetDirectionParam = Animator.StringToHash("TargetDirection");

    /// <summary>
    /// Our state latch. When true, we are in the 'IdleTurn' state and
    /// should not re-calculate the angle.
    /// </summary>
    private bool isTurning;

    // --- INTEGER MAPPING ---
    // This script maps angles to integers like this:
    // 0: Idle (no target, or target is forward)
    // 1: R_45  (22.5 to 67.5 degrees)
    // 2: R_90  (67.5 to 112.5 degrees)
    // 3: R_135 (112.5 to 157.5 degrees)
    // 4: R_180 (157.5 to 180 degrees)
    // 5: L_180 (-157.5 to -180 degrees)
    // 6: L_135 (-112.5 to -157.5 degrees)
    // 7: L_90  (-67.5 to -112.5 degrees)
    // 8: L_45  (-22.5 to -67.5 degrees)
    // -----------------------

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Call this from your Input or AI script to provide a target direction.
    /// </summary>
    /// <param name="worldDirection">The direction the character should be facing.</param>
    public void SetTargetDirection(Vector3 worldDirection)
    {
        // Check if the input is significant. If not, we'll clear the target.
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            currentTargetDirection = worldDirection.normalized;
            hasTarget = true;
        }
        else
        {
            hasTarget = false;
        }
    }

    private void Update()
    {
        // Get the animator's current state.
        bool animatorIsTurning = animator.GetCurrentAnimatorStateInfo(0).IsName("IdleTurn");

        if (!isTurning)
        {
            // --- We are NOT latched, so we are free to START a new turn ---
            
            int directionInt;
            if (hasTarget)
            {
                // Get the character's forward direction, ignoring Y
                Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                
                // Get the target direction, ignoring Y
                Vector3 targetDir = new Vector3(currentTargetDirection.x, 0, currentTargetDirection.z).normalized;
                
                // Calculate the signed angle between forward and target
                float angle = Vector3.SignedAngle(forward, targetDir, Vector3.up);

                // Convert this angle into our integer mapping
                directionInt = GetDirectionIntFromAngle(angle);
            }
            else
            {
                // No target, so we set the direction to 0 (Idle)
                directionInt = 0;
            }
                
            // Set the integer on the animator
            animator.SetFloat(TargetDirectionParam, directionInt / 8f); // Not actually an int!

            // If this calculation *caused* a turn (param > 0), set our latch
            if (directionInt != 0)
            {
                isTurning = true;
            }
        }
        else
        {
            // --- We ARE latched, meaning a turn is in progress ---
            
            // We must wait for the animator to leave the IdleTurn state
            if (animatorIsTurning)
            {
                // Animator is still turning.
                // Check if the user has let go of the input (interruption)
                if (!hasTarget)
                {
                    // User let go. Force the animator back to Idle.
                    animator.SetFloat(TargetDirectionParam, 0);
                    // We keep isTurning=true until the animator *actually*
                    // leaves the IdleTurn state (which will happen next frame).
                }
                
                // If user is *still* holding the target (hasTarget=true),
                // we do *nothing*. We let the animator keep the value
                // it had, preventing the angle from being recalculated.
            }
            else
            {
                // Animator is NO LONGER in the IdleTurn state.
                // The turn is complete (or was interrupted and finished).
                // Release the latch so we can calculate a new turn next frame.
                isTurning = false;
            }
        }
    }

    /// <summary>
    /// Converts a signed angle (-180 to 180) into the 0-8 integer map.
    /// </summary>
    private int GetDirectionIntFromAngle(float angle)
    {
        // R_45
        if (angle > 22.5f && angle <= 67.5f) return 1;
        // R_90
        if (angle > 67.5f && angle <= 112.5f) return 2;
        // R_135
        if (angle > 112.5f && angle <= 157.5f) return 3;
        // R_180
        if (angle > 157.5f) return 4;
        
        // L_180
        if (angle <= -157.5f) return 5;
        // L_135
        if (angle > -157.5f && angle <= -112.5f) return 6;
        // L_90
        if (angle > -112.5f && angle <= -67.5f) return 7;
        // L_45
        if (angle > -67.5f && angle <= -22.5f) return 8;

        // Default: Idle (covers -22.5 to 22.5)
        return 0; 
    }
}