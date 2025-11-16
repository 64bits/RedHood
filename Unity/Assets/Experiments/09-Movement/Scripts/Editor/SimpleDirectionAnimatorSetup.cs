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
/// 2. Eight directional states (R_45, R_90, ... L_45).
/// 3. Eight "committed" directional states (Committed_R_45, ...).
/// 4. 'TargetDirection' (int) and 'committed' (bool) parameters.
/// 5. Transitions from Idle to directional states (requires committed == false).
/// 6. Transitions from Idle to committed states (requires committed == true).
/// 7. Transitions from directional states back to Idle (using exit time).
/// 8. Transitions from committed states back to Idle (immediate, when committed == false).
///
/// It will attempt to find and assign the 17 required animation clips
/// from the specified folder, ensuring they are saved as sub-assets of the controller.
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

    // Dictionaries for Committed States
    private static readonly Dictionary<int, string> IntToCommittedClipName = new Dictionary<int, string>
    {
        { 1, "MOB_Stand_Relaxed_To_Run_R45_Fwd" },
        { 2, "MOB_Stand_Relaxed_To_Run_R90_Fwd" },
        { 3, "MOB_Stand_Relaxed_To_Run_R135_Fwd" },
        { 4, "MOB_Stand_Relaxed_To_Run_R180_Fwd" },
        { 5, "MOB_Stand_Relaxed_To_Run_L180_Fwd" },
        { 6, "MOB_Stand_Relaxed_To_Run_L135_Fwd" },
        { 7, "MOB_Stand_Relaxed_To_Run_L90_Fwd" },
        { 8, "MOB_Stand_Relaxed_To_Run_L45_Fwd" }
    };

    private static readonly Dictionary<int, string> IntToCommittedStateName = new Dictionary<int, string>
    {
        { 1, "Committed_R_45" },
        { 2, "Committed_R_90" },
        { 3, "Committed_R_135" },
        { 4, "Committed_R_180" },
        { 5, "Committed_L_180" },
        { 6, "Committed_L_135" },
        { 7, "Committed_L_90" },
        { 8, "Committed_L_45" }
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
            "Select the folder containing your 17 animations:\n" +
            "- 9 'MOB_Stand_Relaxed...' (Idle + 8 Dirs)\n" +
            "- 8 'MOB_Stand_Relaxed_To_Run...' (Committed Dirs)\n\n" +
            "The script will automatically find, copy, and assign them.",
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
    /// Returns a new instance if sourced from an FBX sub-asset.
    /// </summary>
    private AnimationClip LoadClipFromFolder(string folderPath, string clipName)
    {
        // Try finding a Model asset (FBX) first
        string[] modelGuids = AssetDatabase.FindAssets($"{clipName} t:Model", new[] { folderPath });
        if (modelGuids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
            
            // An FBX is a container; we need to find the AnimationClip *inside* it.
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip)
                {
                    // MUST instantiate for FBX sub-assets to make them editable copies
                    AnimationClip instancedClip = Instantiate(clip);
                    instancedClip.name = clip.name;
                    return instancedClip;
                }
            }
        }
        
        // If no model, try finding a standalone AnimationClip asset (.anim)
        string[] animGuids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip", new[] { folderPath });
        if (animGuids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(animGuids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        Debug.LogError($"Could not find animation clip for '{clipName}' in '{folderPath}'.");
        return null;
    }
    
    /// <summary>
    /// Loads the clip, adds it as a sub-asset to the controller, and handles error checking.
    /// </summary>
    private AnimationClip SafeLoadAndAddClip(string clipName, string folderPath, AnimatorController controller)
    {
        AnimationClip clip = LoadClipFromFolder(folderPath, clipName);
        if (clip != null)
        {
            // Crucial step: Add the clip (especially if instantiated) as a sub-asset 
            // of the new controller so it gets saved correctly.
            AssetDatabase.AddObjectToAsset(clip, controller);
            return clip;
        }
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

        // 3. Create the controller and parameters
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("TargetDirection", AnimatorControllerParameterType.Int);
        controller.AddParameter("committed", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // 4. Load Clips and Create States
        
        // Load and create Idle state (0)
        AnimationClip idleClip = SafeLoadAndAddClip(IntToClipName[0], folderPath, controller);
        if(idleClip == null)
        {
            // Clean up the created controller file if the crucial Idle clip is missing
            AssetDatabase.DeleteAsset(path); 
            Debug.LogError("Failed to load Idle clip. Aborting animator creation.");
            return;
        }
        AnimatorState idleState = rootSM.AddState(IntToStateName[0]);
        idleState.motion = idleClip;
        rootSM.defaultState = idleState;

        // Create UNCOMMITTED directional states (1-8)
        Dictionary<int, AnimatorState> directionalStates = new Dictionary<int, AnimatorState>();
        
        for (int i = 1; i <= 8; i++)
        {
            // Load the clip and add it as a sub-asset
            AnimationClip dirClip = SafeLoadAndAddClip(IntToClipName[i], folderPath, controller);
            
            // Create the state
            AnimatorState dirState = rootSM.AddState(IntToStateName[i]);
            dirState.motion = dirClip;
            directionalStates[i] = dirState;
            
            // Transition FROM Idle TO this directional state (IF committed == false)
            AnimatorStateTransition toDirection = idleState.AddTransition(dirState);
            toDirection.AddCondition(AnimatorConditionMode.Equals, i, "TargetDirection");
            toDirection.AddCondition(AnimatorConditionMode.IfNot, 0, "committed"); 
            toDirection.hasExitTime = false;
            toDirection.duration = 0.15f;
            toDirection.exitTime = 0;
            toDirection.canTransitionToSelf = false;
            
            // Transition FROM this directional state BACK TO Idle (using exit time)
            AnimatorStateTransition backToIdle = dirState.AddTransition(idleState);
            backToIdle.hasExitTime = true;
            backToIdle.exitTime = 0.95f; 
            backToIdle.duration = 0.1f;
            backToIdle.canTransitionToSelf = false;
        }
        
        // Create COMMITTED states (1-8)
        for (int i = 1; i <= 8; i++)
        {
            // Load the clip and add it as a sub-asset
            AnimationClip committedClip = SafeLoadAndAddClip(IntToCommittedClipName[i], folderPath, controller);
            
            // Create the state
            AnimatorState committedState = rootSM.AddState(IntToCommittedStateName[i]);
            committedState.motion = committedClip;
            
            // Transition FROM Idle TO this committed state (WHEN committed == true)
            AnimatorStateTransition toCommittedState = idleState.AddTransition(committedState);
            toCommittedState.AddCondition(AnimatorConditionMode.Equals, i, "TargetDirection");
            toCommittedState.AddCondition(AnimatorConditionMode.If, 0, "committed"); 
            toCommittedState.hasExitTime = false;
            toCommittedState.duration = 0.15f;
            toCommittedState.exitTime = 0;
            toCommittedState.canTransitionToSelf = false;
            
            // Transition FROM this committed state BACK TO Idle (IMMEDIATE WHEN committed == false)
            AnimatorStateTransition committedBackToIdle = committedState.AddTransition(idleState);
            committedBackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "committed"); 
            committedBackToIdle.hasExitTime = false; 
            committedBackToIdle.duration = 0.1f;
            committedBackToIdle.canTransitionToSelf = false;
        }

        // 5. Add transitions between (non-committed) directional states for smooth direction changes
        for (int i = 1; i <= 8; i++)
        {
            AnimatorState fromState = directionalStates[i];
            
            for (int j = 1; j <= 8; j++)
            {
                if (i == j) continue; // Skip self-transitions
                
                AnimatorState toState = directionalStates[j];
                AnimatorStateTransition crossTransition = fromState.AddTransition(toState);
                crossTransition.AddCondition(AnimatorConditionMode.Equals, j, "TargetDirection");
                crossTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "committed"); // Also check committed is false
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
                  $"Animations from '{folderPath}' have been assigned and saved as sub-assets.\n" +
                  $"Created states: Idle + 8 directional states + 8 committed states.");
    }
}