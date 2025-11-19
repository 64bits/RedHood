using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class DitheredVignettePass : ScriptableRenderPass
{
    private Material material;
    private DitheredVignetteFeature.Settings settings;
    
    private static readonly int IntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int SizeID = Shader.PropertyToID("_VignetteSize");

    private class PassData
    {
        public TextureHandle source;
        public TextureHandle destination;
        public Material material;
        public float intensity;
        public float size;
    }

    public DitheredVignettePass(Material mat, DitheredVignetteFeature.Settings settings)
    {
        this.material = mat;
        this.settings = settings;
        renderPassEvent = settings.renderPassEvent;
        profilingSampler = new ProfilingSampler("DitheredVignette");
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (material == null) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        TextureHandle source = resourceData.activeColorTexture;

        if (!source.IsValid()) return;

        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;
        
        // Create temporary texture for the vignette effect
        TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, descriptor, "_TempVignetteTexture", false);

        // Pass 1: Apply vignette effect
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Dithered Vignette Pass", out var passData))
        {
            passData.source = source;
            passData.destination = destination;
            passData.material = material;
            passData.intensity = settings.intensity;
            passData.size = settings.vignetteSize;

            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // Set shader globals
                // context.cmd.SetGlobalFloat(IntensityID, data.intensity);
                // context.cmd.SetGlobalFloat(SizeID, data.size);
                
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                    data.material, 0);
            });
        }

        // Pass 2: Copy vignetted result back to camera color
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Copy Vignette Back", out var passData))
        {
            passData.source = destination;
            passData.destination = source;

            builder.UseTexture(destination, AccessFlags.Read);
            builder.SetRenderAttachment(source, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 
                    0, false);
            });
        }
    }

    public void Dispose()
    {
        // No RTHandles to dispose of when using RenderGraph
    }
}