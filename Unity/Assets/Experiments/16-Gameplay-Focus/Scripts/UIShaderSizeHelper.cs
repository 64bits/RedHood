using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
public class UIShaderSizeHelper : MonoBehaviour
{
    private Image img;
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

    void Update()
    {
        if (img == null) img = GetComponent<Image>();
        Rect r = ((RectTransform)transform).rect;
        // Feed the width and height to the shader
        img.material.SetVector(RectSizeID, new Vector4(r.width, r.height, 0, 0));
    }
}