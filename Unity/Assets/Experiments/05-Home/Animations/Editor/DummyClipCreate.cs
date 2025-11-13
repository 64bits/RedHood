using UnityEditor;
using UnityEngine;

public class DummyClipCreate : Editor
{
    [MenuItem("Tools/Create Dummy Clip")]
    static void CreateDummyClip()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Dummy Animation Clip", "Dummy", "anim", "");
        AnimationClip clip = new AnimationClip();
        clip.name = "Dummy";
        AnimationCurve curve = AnimationCurve.Linear(0.0F, 1.0F, 0.017F, 1.0F);
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(UnityEngine.Animator), "DummyAnimationClip");
        AnimationUtility.SetEditorCurve(clip, binding, curve);
        AssetDatabase.CreateAsset(clip, path);
    }
}
