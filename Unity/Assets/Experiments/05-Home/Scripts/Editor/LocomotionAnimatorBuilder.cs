#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to automatically build the locomotion animator controller
/// </summary>
public class LocomotionAnimatorBuilder : EditorWindow
{
    private AnimatorController controller;
    private string savePath = "Assets/Animations/LocomotionController.controller";
    private string animationFolder = "Assets/Animations/Clips";
    
    // Animation clips dictionary
    private Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
    
    // Expected clip names
    private readonly string[] requiredClips = new string[]
    {
        "MOB_Run_F_To_Stand_Relaxed",
        "MOB_Run_F",
        "MOB_Run_L_180",
        "MOB_Run_L_90",
        "MOB_Run_R_180",
        "MOB_Run_R_90",
        "MOB_Stand_Relaxed_Idle",
        "MOB_Stand_Relaxed_L135",
        "MOB_Stand_Relaxed_L180",
        "MOB_Stand_Relaxed_L45",
        "MOB_Stand_Relaxed_L90",
        "MOB_Stand_Relaxed_R_135",
        "MOB_Stand_Relaxed_R_180",
        "MOB_Stand_Relaxed_R_45",
        "MOB_Stand_Relaxed_R_90",
        "MOB_Stand_Relaxed_To_Run_F",
        "MOB_Stand_Relaxed_To_Run_L135_Fwd",
        "MOB_Stand_Relaxed_To_Run_L180_Fwd",
        "MOB_Stand_Relaxed_To_Run_L45_Fwd",
        "MOB_Stand_Relaxed_To_Run_L90_Fwd",
        "MOB_Stand_Relaxed_To_Run_R135_Fwd",
        "MOB_Stand_Relaxed_To_Run_R180_Fwd",
        "MOB_Stand_Relaxed_To_Run_R45_Fwd",
        "MOB_Stand_Relaxed_To_Run_R90_Fwd"
    };
    
    [MenuItem("Tools/Locomotion/Build Animator Controller")]
    public static void ShowWindow()
    {
        GetWindow<LocomotionAnimatorBuilder>("Locomotion Builder");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Locomotion Animator Controller Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        animationFolder = EditorGUILayout.TextField("Animation Folder:", animationFolder);
        savePath = EditorGUILayout.TextField("Save Path:", savePath);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Scan for Animations"))
        {
            ScanForAnimations();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Found Clips:", clips.Count + " / " + requiredClips.Length);
        
        if (clips.Count > 0)
        {
            EditorGUILayout.Space();
            foreach (var clip in clips)
            {
                EditorGUILayout.LabelField("✓ " + clip.Key);
            }
        }
        
        EditorGUILayout.Space();
        
        GUI.enabled = clips.Count == requiredClips.Length;
        if (GUILayout.Button("Build Animator Controller", GUILayout.Height(40)))
        {
            BuildAnimatorController();
        }
        GUI.enabled = true;
        
        if (clips.Count < requiredClips.Length)
        {
            EditorGUILayout.HelpBox("Not all required animations found. Please check the animation folder path.", MessageType.Warning);
        }
    }
    
    private void ScanForAnimations()
    {
        clips.Clear();
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationFolder });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip != null && requiredClips.Contains(clip.name))
            {
                clips[clip.name] = clip;
            }
        }
        
        Debug.Log($"Found {clips.Count} / {requiredClips.Length} required animation clips");
    }
    
    private void BuildAnimatorController()
    {
        // Create new controller
        controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);
        
        // Add parameters
        AddParameters();
        
        // Build layers
        BuildBaseLayer();
        BuildPivotLayer();
        
        // Save
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Animator Controller created at: " + savePath);
        Selection.activeObject = controller;
    }
    
    private void AddParameters()
    {
        controller.AddParameter("TurnAngle", AnimatorControllerParameterType.Float);
        controller.AddParameter("FrozenTurnAngle", AnimatorControllerParameterType.Float);
        controller.AddParameter("Commitment", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
    }
    
    private void BuildBaseLayer()
    {
        var rootStateMachine = controller.layers[0].stateMachine;
        rootStateMachine.name = "Base Locomotion";
        
        // Create states
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 50, 0));
        idleState.motion = clips["MOB_Stand_Relaxed_Idle"];
        
        var idleToRunState = rootStateMachine.AddState("IdleToRun", new Vector3(300, 150, 0));
        idleToRunState.motion = CreateIdleToRunBlendTree();
        idleToRunState.AddStateMachineBehaviour<IdleToRunBehaviour>();
        
        var runLoopState = rootStateMachine.AddState("RunLoop", new Vector3(550, 150, 0));
        runLoopState.motion = clips["MOB_Run_F"];
        runLoopState.AddStateMachineBehaviour<RunLoopBehaviour>();
        
        var runToIdleState = rootStateMachine.AddState("RunToIdle", new Vector3(550, 50, 0));
        runToIdleState.motion = clips["MOB_Run_F_To_Stand_Relaxed"];
        runToIdleState.AddStateMachineBehaviour<RunToIdleBehaviour>();
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // Create transitions
        // Idle -> IdleToRun
        var idleToIdleToRun = idleState.AddTransition(idleToRunState);
        idleToIdleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToIdleToRun.hasExitTime = false;
        idleToIdleToRun.duration = 0.1f;
        
        // IdleToRun -> RunLoop (with commitment)
        var idleToRunToRunLoop = idleToRunState.AddTransition(runLoopState);
        idleToRunToRunLoop.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Commitment");
        idleToRunToRunLoop.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToRunToRunLoop.hasExitTime = true;
        idleToRunToRunLoop.exitTime = 0.8f;
        idleToRunToRunLoop.duration = 0.2f;
        
        // IdleToRun -> Idle (no commitment - player changed mind quickly)
        var idleToRunToIdle = idleToRunState.AddTransition(idleState);
        idleToRunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        idleToRunToIdle.AddCondition(AnimatorConditionMode.Less, 0.5f, "Commitment");
        idleToRunToIdle.hasExitTime = false;
        idleToRunToIdle.duration = 0.15f;
        
        // RunLoop -> RunToIdle
        var runLoopToRunToIdle = runLoopState.AddTransition(runToIdleState);
        runLoopToRunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        runLoopToRunToIdle.hasExitTime = false;
        runLoopToRunToIdle.duration = 0.15f;
        
        // RunToIdle -> Idle
        var runToIdleToIdle = runToIdleState.AddTransition(idleState);
        runToIdleToIdle.hasExitTime = true;
        runToIdleToIdle.exitTime = 0.9f;
        runToIdleToIdle.duration = 0.1f;
    }
    
    private BlendTree CreateIdleToRunBlendTree()
    {
        var blendTree = new BlendTree
        {
            name = "IdleToRun BlendTree",
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "FrozenTurnAngle",
            blendParameterY = "Commitment"
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        
        // Add idle turn animations (commitment = 0) - micro-movements
        blendTree.AddChild(clips["MOB_Stand_Relaxed_Idle"], new Vector2(0, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_R_45"], new Vector2(45, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_R_90"], new Vector2(90, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_R_135"], new Vector2(135, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_R_180"], new Vector2(180, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_L45"], new Vector2(-45, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_L90"], new Vector2(-90, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_L135"], new Vector2(-135, 0));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_L180"], new Vector2(-180, 0));
        
        // Add committed turn animations (commitment = 1) - full transitions to run
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_F"], new Vector2(0, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_R45_Fwd"], new Vector2(45, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_R90_Fwd"], new Vector2(90, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_R135_Fwd"], new Vector2(135, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_R180_Fwd"], new Vector2(180, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_L45_Fwd"], new Vector2(-45, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_L90_Fwd"], new Vector2(-90, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_L135_Fwd"], new Vector2(-135, 1));
        blendTree.AddChild(clips["MOB_Stand_Relaxed_To_Run_L180_Fwd"], new Vector2(-180, 1));
        
        return blendTree;
    }
    
    private void BuildPivotLayer()
    {
        // Add new layer
        controller.AddLayer("Pivot Layer");
        var layers = controller.layers;
        layers[1].defaultWeight = 0f;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        controller.layers = layers;
        
        var pivotStateMachine = controller.layers[1].stateMachine;
        
        // Create dummy state
        var dummyState = pivotStateMachine.AddState("Dummy", new Vector3(300, 50, 0));
        
        // Create pivot state
        var pivotState = pivotStateMachine.AddState("Pivot", new Vector3(300, 150, 0));
        pivotState.motion = CreatePivotBlendTree();
        pivotState.AddStateMachineBehaviour<PivotBehaviour>();
        
        // Set default
        pivotStateMachine.defaultState = dummyState;
        
        // Note: Pivot triggering is handled by layer weight in the controller script
        // This layer is activated programmatically
    }
    
    private BlendTree CreatePivotBlendTree()
    {
        var blendTree = new BlendTree
        {
            name = "Pivot BlendTree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "TurnAngle",
            minThreshold = -180f,
            maxThreshold = 180f
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        
        // Add pivot animations
        blendTree.AddChild(clips["MOB_Run_R_180"], -180f);
        blendTree.AddChild(clips["MOB_Run_R_90"], -90f);
        blendTree.AddChild(clips["MOB_Run_L_90"], 90f);
        blendTree.AddChild(clips["MOB_Run_L_180"], 180f);
        
        return blendTree;
    }
}
#endif