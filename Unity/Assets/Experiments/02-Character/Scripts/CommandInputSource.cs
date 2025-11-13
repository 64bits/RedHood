// MovementInputSource.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterLocomotionController))]
/// <summary>
/// This script demonstrates one way to set the target direction
/// (by processing a list of commands). This logic could be replaced
/// by a script reading user input (e.g., Input.GetAxis("Horizontal")).
/// </summary>
public class CommandInputSource : MonoBehaviour
{
    [System.Serializable]
    public struct MoveCommand
    {
        public Vector3 direction; // World-space direction
        public float duration;    // Duration to hold this movement
    }

    public MoveCommand[] commands;

    // A reference to the controller that will receive the movement direction
    private CharacterLocomotionController motionController;
    private int currentCommandIndex = 0;

    private void Awake()
    {
        // Get the controller component on the same GameObject
        motionController = GetComponent<CharacterLocomotionController>();
        if (motionController == null)
        {
            Debug.LogError("MovementInputSource requires a CharacterLocomotionController component on the same GameObject.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (commands.Length > 0 && motionController != null)
            StartCoroutine(ProcessCommands());
    }

    private IEnumerator ProcessCommands()
    {
        while (true) // Loop indefinitely
        {
            MoveCommand cmd = commands[currentCommandIndex];
            
            // *** CORE LOGIC: Set the target direction on the controller ***
            motionController.SetTargetDirection(cmd.direction.normalized);

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

    // Example of a public method for a separate script (like a CommandManager) to call
    // public void SetNextMoveDirection(Vector3 direction)
    // {
    //     if (motionController != null)
    //     {
    //         motionController.SetTargetDirection(direction);
    //     }
    // }
}