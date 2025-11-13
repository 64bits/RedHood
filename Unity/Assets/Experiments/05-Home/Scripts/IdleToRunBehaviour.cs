using UnityEngine;

/// <summary>
/// State machine behaviour for the IdleToRun state
/// Handles transition timing based on commitment
/// </summary>
public class IdleToRunBehaviour : StateMachineBehaviour
{
    private static readonly int TurnAngleParam = Animator.StringToHash("TurnAngle");
    private static readonly int FrozenTurnAngleParam = Animator.StringToHash("FrozenTurnAngle");
    
    private float snapshotAngle;
    
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Capture the turn angle at the moment we enter this state
        snapshotAngle = animator.GetFloat(TurnAngleParam);
        animator.SetFloat(FrozenTurnAngleParam, snapshotAngle);
    }
    
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Keep the frozen value throughout the state
        animator.SetFloat(FrozenTurnAngleParam, snapshotAngle);
    }
}