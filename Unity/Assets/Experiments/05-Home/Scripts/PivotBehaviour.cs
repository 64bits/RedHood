using UnityEngine;

/// <summary>
/// State machine behaviour for Pivot animations
/// Signals the controller when the pivot state is exited.
/// </summary>
public class PivotBehaviour : StateMachineBehaviour
{
    private CharacterLocomotionController controller;
    private static readonly int TurnAngleParam = Animator.StringToHash("TurnAngle");
    private static readonly int FrozenTurnAngleParam = Animator.StringToHash("FrozenTurnAngle");

    private float snapshotAngle;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the controller reference
        if (controller == null)
        {
            controller = animator.GetComponent<CharacterLocomotionController>();
        }

        // Capture the turn angle at the moment we enter this state
        snapshotAngle = animator.GetFloat(TurnAngleParam);
        animator.SetFloat(FrozenTurnAngleParam, snapshotAngle);
    }
    
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Keep the frozen value throughout the state
        animator.SetFloat(FrozenTurnAngleParam, snapshotAngle);
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Signal the controller that the pivot animation is finished.
        if (controller != null)
        {
            controller.EndPivot();
        }
    }
}