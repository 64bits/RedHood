using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This Editor script creates an Animator Controller designed to work
/// with the SimpleDirectionController.
///
/// It creates:
/// 1. An 'Idle' state (motion set in inspector).
/// 2. Eight directional states (R_45, R_90, R_135, R_180, L_180, L_135, L_90, L_45).
/// 3. The 'TargetDirection' integer parameter.
/// 4. Transitions from Idle to each directional state (no exit time).
/// 5. Transitions from each directional state back to Idle (using exit time).
///
/// It will attempt to find and assign the 9 required animation clips
/// from the specified folder.
/// </summary>
public class SimpleDirectionAnimatorSetup : EditorWindow
{
    private DefaultAsset animationFolder;

    // Map integers from the controller to the expected animation clip names
    private static readonly Dictionary<int, string> IntToClipName = new Dictionary<int, string>
    {
        { 0, "MOB_Stand_Relaxed_Idle" },
        { 1, "MOB_Stand_Relaxed_R_45" },
        { 2, "MOB_Stand_Relaxed_R_90" },
        { 3, "MOB_Stand_Relaxed_R_135" },
        { 4, "MOB_Stand_Relaxed_R_180" },
        { 5, "MOB_Stand_Relaxed_L180" },
        { 6, "MOB_Stand_Relaxed_L135" },
        { 7, "MOB_Stand_Relaxed_L90" },
        { 8, "MOB_Stand_Relaxed_L45" }
    };

    // State names matching the integer values
    private static readonly Dictionary<int, string> IntToStateName = new Dictionary<int, string>
    {
        { 0, "Idle" },
        { 1, "R_45" },
        { 2, "R_90" },
        { 3, "R_135" },
        { 4, "R_180" },
        { 5, "L_180" },
        { 6, "L_135" },
        { 7, "L_90" },
        { 8, "L_45" }
    };

    [MenuItem("Tools/Animation/Create Simple Direction Animator")]
    public static void ShowWindow()
    {
        GetWindow<SimpleDirectionAnimatorSetup>("Direction Animator Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Stand-Turn Animator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "This will create a new Animator Controller for the SimpleDirectionController.\n\n" +
            "Select the folder containing your 9 'MOB_Stand_Relaxed...' animations " +
            "(either .fbx or .anim files). The script will automatically find and assign them.", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        animationFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Animation Folder", 
            animationFolder, 
            typeof(DefaultAsset), 
            false);

        GUILayout.Space(10);
        
        GUI.enabled = animationFolder != null;
        if (GUILayout.Button("Create Animator Controller", GUILayout.Height(40)))
        {
            CreateAnimatorController();
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// Loads an AnimationClip from the specified folder.
    /// It searches for both Model files (FBX) and standalone AnimationClip files (.anim).
    /// </summary>
    private AnimationClip LoadClipFromFolder(string folderPath, string clipName)
    {
        // Try finding a Model asset (FBX) first
        string[] modelGuids = AssetDatabase.FindAssets($"{clipName} t:Model", new[] { folderPath });
        if (modelGuids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
            if (modelGuids.Length > 1)
            {
                Debug.LogWarning($"Found multiple models named '{clipName}' in {folderPath}. Using the first one: {assetPath}");
            }
            
            // An FBX is a container; we need to find the AnimationClip *inside* it.
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip)
                {
                    return clip;
                }
            }
        }
        
        // If no model, try finding a standalone AnimationClip asset (.anim)
        string[] animGuids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip", new[] { folderPath });
        if (animGuids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(animGuids[0]);
            if (animGuids.Length > 1)
            {
                Debug.LogWarning($"Found multiple .anim files named '{clipName}' in {folderPath}. Using the first one: {assetPath}");
            }
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        Debug.LogError($"Could not find animation clip for '{clipName}' in '{folderPath}'. Looked for FBX and .anim files.");
        return null;
    }

    private void CreateAnimatorController()
    {
        // 1. Validate folder
        if (animationFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an animation folder first.", "OK");
            return;
        }
        string folderPath = AssetDatabase.GetAssetPath(animationFolder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "The selected item is not a valid folder.", "OK");
            return;
        }

        // 2. Ask user where to save the controller
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Animator Controller", 
            "AC_SimpleDirection", 
            "controller", 
            "Please select a location to save the animator controller.");

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Animator creation cancelled.");
            return;
        }

        // 3. Create the controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("TargetDirection", AnimatorControllerParameterType.Int);
        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // 4. Load Clips and Create States
        
        // Load and create Idle state (0)
        AnimationClip idleClip = LoadClipFromFolder(folderPath, IntToClipName[0]);
        AnimatorState idleState = rootSM.AddState(IntToStateName[0]);
        idleState.motion = idleClip;
        rootSM.defaultState = idleState;

        // Create directional states (1-8) and set up transitions
        Dictionary<int, AnimatorState> directionalStates = new Dictionary<int, AnimatorState>();
        
        for (int i = 1; i <= 8; i++)
        {
            // Load the clip for this direction
            AnimationClip dirClip = LoadClipFromFolder(folderPath, IntToClipName[i]);
            
            // Create the state
            AnimatorState dirState = rootSM.AddState(IntToStateName[i]);
            dirState.motion = dirClip;
            directionalStates[i] = dirState;
            
            // Transition FROM Idle TO this directional state (no exit time, immediate response)
            AnimatorStateTransition toDirection = idleState.AddTransition(dirState);
            toDirection.AddCondition(AnimatorConditionMode.Equals, i, "TargetDirection");
            toDirection.hasExitTime = false;
            toDirection.duration = 0.15f;
            toDirection.exitTime = 0;
            toDirection.canTransitionToSelf = false;
            
            // Transition FROM this directional state BACK TO Idle (using exit time)
            AnimatorStateTransition backToIdle = dirState.AddTransition(idleState);
            backToIdle.hasExitTime = true;
            backToIdle.exitTime = 0.95f; // Near the end of the animation
            backToIdle.duration = 0.1f;
            backToIdle.canTransitionToSelf = false;
        }

        // 5. Add transitions between directional states for smooth direction changes
        for (int i = 1; i <= 8; i++)
        {
            AnimatorState fromState = directionalStates[i];
            
            for (int j = 1; j <= 8; j++)
            {
                if (i == j) continue; // Skip self-transitions
                
                AnimatorState toState = directionalStates[j];
                AnimatorStateTransition crossTransition = fromState.AddTransition(toState);
                crossTransition.AddCondition(AnimatorConditionMode.Equals, j, "TargetDirection");
                crossTransition.hasExitTime = false;
                crossTransition.duration = 0.15f;
                crossTransition.exitTime = 0;
                crossTransition.canTransitionToSelf = false;
            }
        }

        // 6. Finish
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = controller;

        Debug.Log($"Successfully created Animator Controller at: {path}\n" +
                  $"Animations from '{folderPath}' have been assigned.\n" +
                  $"Created states: Idle + 8 directional states (R_45, R_90, R_135, R_180, L_180, L_135, L_90, L_45)");
    }
}