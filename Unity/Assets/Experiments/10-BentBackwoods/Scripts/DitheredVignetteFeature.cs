using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DitheredVignetteFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader vignetteShader;
        [Range(0f, 1f)]
        public float intensity = 0.5f;
        [Range(0f, 1f)]
        public float vignetteSize = 0.5f;
    }

    public Settings settings = new Settings();
    private DitheredVignettePass vignettePass;
    private Material vignetteMaterial;
    
    private static readonly int IntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int SizeID = Shader.PropertyToID("_VignetteSize");

    public override void Create()
    {
        if (settings.vignetteShader != null)
        {
            vignetteMaterial = CoreUtils.CreateEngineMaterial(settings.vignetteShader);
            vignettePass = new DitheredVignettePass(vignetteMaterial, settings);
            
            // Set initial global shader values
            Shader.SetGlobalFloat(IntensityID, settings.intensity);
            Shader.SetGlobalFloat(SizeID, settings.vignetteSize);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (vignettePass != null && vignetteMaterial != null)
        {
            vignettePass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(vignettePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        vignettePass?.Dispose();
        CoreUtils.Destroy(vignetteMaterial);
    }
}