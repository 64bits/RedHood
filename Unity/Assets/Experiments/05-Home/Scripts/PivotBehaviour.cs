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

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the controller reference
        if (controller == null)
        {
            controller = animator.GetComponent<CharacterLocomotionController>();
        }
        
        // Snap the initial turn angle and set it
        float currentAngle = animator.GetFloat(TurnAngleParam);
        float snappedAngle = SnapToCardinalAngle(currentAngle);
        animator.SetFloat(FrozenTurnAngleParam, snappedAngle);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Continuously update by snapping the current turn angle
        float currentAngle = animator.GetFloat(TurnAngleParam);
        float snappedAngle = SnapToCardinalAngle(currentAngle);
        animator.SetFloat(FrozenTurnAngleParam, snappedAngle);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Signal the controller that the pivot animation is finished.
        if (controller != null)
        {
            controller.EndPivot();
        }
    }

    private float SnapToCardinalAngle(float angle)
    {
        // Define the cardinal angles
        float[] cardinalAngles = { -180f, -90f, 90f, 180f };
        
        // Find the nearest cardinal angle
        float nearestAngle = cardinalAngles[0];
        float minDistance = Mathf.Abs(Mathf.DeltaAngle(angle, cardinalAngles[0]));
        
        for (int i = 1; i < cardinalAngles.Length; i++)
        {
            float distance = Mathf.Abs(Mathf.DeltaAngle(angle, cardinalAngles[i]));
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestAngle = cardinalAngles[i];
            }
        }
        
        return nearestAngle;
    }
}