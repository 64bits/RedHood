using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

public class MixedMaskFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader HighlightMaskShader; // Renders highlight silhouettes
        public Shader RevealMaskShader; // Renders reveal silhouettes
        public string MaskTextureName = "_CombinedMaskTex";
        public float AlphaCutoff = 0.5f;
        
        [Header("Debug")]
        public bool DebugMask = false;
    }

    public Settings settings = new();

    class CombinedMaskPass : ScriptableRenderPass
    {
        private Settings _settings;
        private Material _highlightMaterial;
        private Material _revealMaterial;

        public CombinedMaskPass(Settings settings)
        {
            _settings = settings;
            if (_settings.HighlightMaskShader != null)
                _highlightMaterial = CoreUtils.CreateEngineMaterial(_settings.HighlightMaskShader);
            if (_settings.RevealMaskShader != null)
                _revealMaterial = CoreUtils.CreateEngineMaterial(_settings.RevealMaskShader);
        }

        private class PassData 
        {
            public TextureHandle maskTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_highlightMaterial == null && _revealMaterial == null) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            // Create RG texture for combined masks
            TextureHandle maskTex = renderGraph.CreateTexture(
                new TextureDesc(desc.width, desc.height)
                {
                    colorFormat = GraphicsFormat.R8G8_UNorm, // R channel = Highlight, G channel = Reveal
                    depthBufferBits = DepthBits.None,
                    msaaSamples = (MSAASamples) desc.msaaSamples,
                    name = _settings.MaskTextureName,
                    clearBuffer = true,
                    clearColor = Color.clear
                });

            // Pass 1: Render Highlight mask to R channel
            if (_highlightMaterial != null)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "Render Highlight Mask (R)", out PassData passData))
                {
                    passData.maskTexture = maskTex;
                    
                    builder.SetRenderAttachment(maskTex, 0);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        _highlightMaterial.SetFloat("_Cutoff", _settings.AlphaCutoff);
                        
                        // Draw all registered highlightable objects to R channel
                        foreach (var rend in Highlightable.AllHighlightables)
                        {
                            if (rend == null) continue;
                            ctx.cmd.DrawRenderer(rend, _highlightMaterial, 0); 
                        }
                    });
                }
            }

            // Pass 2: Render Reveal mask to G channel (additive)
            if (_revealMaterial != null)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "Render Reveal Mask (G)", out PassData passData))
                {
                    passData.maskTexture = maskTex;
                    
                    builder.SetRenderAttachment(maskTex, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        _revealMaterial.SetFloat("_Cutoff", _settings.AlphaCutoff);
                        
                        // Draw all registered revealable objects to G channel
                        foreach (var rend in Revealable.AllRevealables)
                        {
                            if (rend == null) continue;
                            ctx.cmd.DrawRenderer(rend, _revealMaterial, 0); 
                        }
                    });
                }
            }

            // Pass 3: Expose mask texture globally
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Expose Combined Mask", out PassData exposeData))
            {
                exposeData.maskTexture = maskTex;
                
                builder.UseTexture(maskTex);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    // Make mask available to all shaders
                    ctx.cmd.SetGlobalTexture(_settings.MaskTextureName, data.maskTexture);
                });
            }
        }
    }

    private CombinedMaskPass _maskPass;

    public override void Create()
    {
        _maskPass = new CombinedMaskPass(settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_maskPass);
    }
}