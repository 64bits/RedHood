using UnityEngine;

/// <summary>
/// Debug visualizer for the locomotion system
/// Shows movement state, commitment, pivot info, and turn angles in editor and runtime
/// </summary>
[RequireComponent(typeof(CharacterLocomotionController))]
public class LocomotionDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showOnScreenDebug = true;
    
    [Header("Gizmo Settings")]
    [SerializeField] private float forwardArrowLength = 2f;
    [SerializeField] private float targetArrowLength = 2.5f;
    [SerializeField] private float gizmoHeight = 1.5f;
    
    private CharacterLocomotionController locomotionController;
    private Animator animator;
    
    // Cached for display
    private string currentStateName;
    private string currentPivotStateName;
    
    private void Awake()
    {
        locomotionController = GetComponent<CharacterLocomotionController>();
        animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        if (showDebugInfo && animator != null)
        {
            // Cache state names
            AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo pivotState = animator.GetCurrentAnimatorStateInfo(1);
            
            currentStateName = GetStateName(baseState);
            currentPivotStateName = GetStateName(pivotState);
        }
    }
    
    private string GetStateName(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName("Idle")) return "Idle";
        if (stateInfo.IsName("IdleToRun")) return "IdleToRun";
        if (stateInfo.IsName("RunLoop")) return "RunLoop";
        if (stateInfo.IsName("RunToIdle")) return "RunToIdle";
        if (stateInfo.IsName("Dummy")) return "Dummy";
        if (stateInfo.IsName("Pivot")) return "Pivot";
        return "Unknown";
    }
    
    private void OnGUI()
    {
        if (!showOnScreenDebug || !showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Box("Locomotion Debug Info");
        
        GUILayout.Label($"Is Moving: {locomotionController.IsMoving}");
        GUILayout.Label($"Commitment: {locomotionController.CurrentCommitment:F2}");
        GUILayout.Label($"Turn Angle: {locomotionController.CurrentTurnAngle:F1}°");
        GUILayout.Label($"Pivot Weight: {locomotionController.PivotWeight:F2}");
        
        GUILayout.Space(10);
        GUILayout.Label($"Base State: {currentStateName}");
        GUILayout.Label($"Pivot State: {currentPivotStateName}");
        
        if (animator != null)
        {
            AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
            GUILayout.Label($"State Time: {baseState.normalizedTime:F2}");
        }
        
        GUILayout.EndArea();
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        if (locomotionController == null) return;
        
        Vector3 basePos = transform.position + Vector3.up * gizmoHeight;
        
        // Draw forward direction (blue)
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward * forwardArrowLength;
        Gizmos.DrawLine(basePos, basePos + forward);
        DrawArrowHead(basePos + forward, transform.forward, 0.3f, Color.blue);
        
        // Draw target direction (green if moving, gray if not)
        Gizmos.color = locomotionController.IsMoving ? Color.green : Color.gray;
        Vector3 targetDir = Quaternion.Euler(0, locomotionController.CurrentTurnAngle, 0) * transform.forward;
        Vector3 target = targetDir * targetArrowLength;
        Gizmos.DrawLine(basePos, basePos + target);
        DrawArrowHead(basePos + target, targetDir, 0.3f, locomotionController.IsMoving ? Color.green : Color.gray);
        
        // Draw commitment indicator (yellow ring)
        if (locomotionController.CurrentCommitment > 0.01f)
        {
            Gizmos.color = Color.yellow;
            DrawCircle(basePos, 0.5f + locomotionController.CurrentCommitment * 0.5f, 16);
        }
        
        // Draw pivot indicator (red flash)
        if (locomotionController.PivotWeight > 0.01f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, locomotionController.PivotWeight);
            DrawCircle(basePos, 1.5f, 24);
            
            // Draw pivot arc
            float angle = locomotionController.CurrentTurnAngle;
            DrawArc(basePos, 1.2f, 0, angle, 16);
        }
        
        // Draw movement path prediction
        if (locomotionController.IsMoving)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 predictedPos = transform.position + targetDir * 3f;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, predictedPos + Vector3.up * 0.1f);
        }
    }
    
    private void DrawArrowHead(Vector3 pos, Vector3 direction, float size, Color color)
    {
        Gizmos.color = color;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 25, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 25, 0) * Vector3.forward;
        
        Gizmos.DrawLine(pos, pos + right * size);
        Gizmos.DrawLine(pos, pos + left * size);
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    private void DrawArc(Vector3 center, float radius, float startAngle, float endAngle, int segments)
    {
        if (Mathf.Abs(endAngle - startAngle) < 0.1f) return;
        
        float angleRange = endAngle - startAngle;
        float angleStep = angleRange / segments;
        
        Vector3 prevPoint = center + Quaternion.Euler(0, startAngle, 0) * Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector3 newPoint = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}