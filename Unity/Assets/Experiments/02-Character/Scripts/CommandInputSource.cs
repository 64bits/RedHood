// MovementInputSource.cs
using System.Collections;
using UnityEngine;
[RequireComponent(typeof(SimpleDirectionController))]
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
    
    [Header("Debug Visualization")]
    public bool showDebugArrow = true;
    public float arrowLength = 2f;
    
    // A reference to the controller that will receive the movement direction
    private SimpleDirectionController motionController;
    private int currentCommandIndex = 0;
    
    private void Awake()
    {
        // Get the controller component on the same GameObject
        motionController = GetComponent<SimpleDirectionController>();
        if (motionController == null)
        {
            Debug.LogError("MovementInputSource requires a SimpleDirectionController component on the same GameObject.");
            enabled = false;
        }
    }
    
    private void Start()
    {
        if (commands.Length > 0 && motionController != null)
            StartCoroutine(ProcessCommands());
    }
    
    private void Update()
    {
        // Draw debug arrow every frame
        if (showDebugArrow)
        {
            if (commands.Length > 0)
            {
                Vector3 direction = commands[currentCommandIndex].direction.normalized;
                DrawDebugArrow(transform.position, direction, arrowLength, new Color(0.5f, 0.7f, 1f)); // Light blue
            }
            
            // Draw transform.forward arrow in deep blue
            DrawDebugArrow(transform.position, transform.forward, arrowLength, new Color(0f, 0.2f, 0.8f)); // Deep blue
        }
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
    
    private void DrawDebugArrow(Vector3 origin, Vector3 direction, float length, Color color)
    {
        if (direction.magnitude < 0.001f)
            return;
        
        Vector3 end = origin + direction * length;
        
        // Draw main arrow line
        Debug.DrawLine(origin, end, color);
        
        // Draw arrowhead
        float arrowHeadLength = length * 0.25f;
        float arrowHeadAngle = 25f;
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        
        Debug.DrawRay(end, right * arrowHeadLength, color);
        Debug.DrawRay(end, left * arrowHeadLength, color);
    }
}