using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This Editor script creates an Animator Controller designed to work
/// with the SimpleDirectionController.
///
/// It creates:
/// 1. An 'Idle' state.
/// 2. An 'IdleTurn' state containing an 8-way blend tree.
/// 3. The 'TargetDirection' integer parameter.
/// 4. Transitions between Idle and IdleTurn.
///
/// It will attempt to find and assign the 9 required animation clips
/// from the specified folder.
/// </summary>
public class SimpleDirectionAnimatorSetup : EditorWindow
{
    private DefaultAsset animationFolder;

    // Map integers from the controller to the expected animation clip names
    // This is based on your provided file list.
    private static readonly Dictionary<int, string> IntToClipName = new Dictionary<int, string>
    {
        { 0, "MOB_Stand_Relaxed_Idle" },
        { 1, "MOB_Stand_Relaxed_R_45" },
        { 2, "MOB_Stand_Relaxed_R_90" },
        { 3, "MOB_Stand_Relaxed_R_135" },
        { 4, "MOB_Stand_Relaxed_R_180" },
        { 5, "MOB_Stand_Relaxed_L180" }, // Note: L180, L135, etc. based on your file list
        { 6, "MOB_Stand_Relaxed_L135" },
        { 7, "MOB_Stand_Relaxed_L90" },
        { 8, "MOB_Stand_Relaxed_L45" }
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
        // FindAssets searches for files *named* clipName
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
                    // Found the clip inside the FBX
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
        
        // Load Idle (0)
        AnimationClip idleClip = LoadClipFromFolder(folderPath, IntToClipName[0]);
        AnimatorState idleState = rootSM.AddState("Idle");
        idleState.motion = idleClip;
        rootSM.defaultState = idleState;

        // Load Turns (1-8)
        AnimatorState turnState = rootSM.AddState("IdleTurn");
        BlendTree tree = new BlendTree
        {
            name = "IdleTurnBlendTree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "TargetDirection",
            useAutomaticThresholds = false
        };
        
        // Add all 8 directional clips to the blend tree
        for (int i = 1; i <= 8; i++)
        {
            AnimationClip turnClip = LoadClipFromFolder(folderPath, IntToClipName[i]);
            tree.AddChild(turnClip, i); // Threshold matches the integer
        }

        turnState.motion = tree;
        AssetDatabase.AddObjectToAsset(tree, controller);

        // 5. Create Transitions
        AnimatorStateTransition toTurn = idleState.AddTransition(turnState);
        toTurn.AddCondition(AnimatorConditionMode.Greater, 0, "TargetDirection");
        toTurn.hasExitTime = false;
        toTurn.duration = 0.1f;
        toTurn.exitTime = 0;

        AnimatorStateTransition toIdle = turnState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Equals, 0, "TargetDirection");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.exitTime = 0;

        // 6. Finish
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = controller;

        Debug.Log($"Successfully created Animator Controller at: {path}. " +
                  $"Animations from '{folderPath}' have been assigned.");
    }
}