using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

public class OcclusionMaskFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader HighlightMaskShader; // Renders Layer 6 silhouettes
        public string MaskTextureName = "_HighlightMaskTex";
        public string DepthTextureName = "_HighlightDepthTex";
        public LayerMask OccluderLayer = 1 << 7; // Objects that will show X-ray
        public LayerMask HighlightLayer = 1 << 6; // Objects to detect behind occluders
        public float AlphaCutoff = 0.5f;
        public Color HighlightColor = Color.yellow;
    }

    public Settings settings = new();

    class OcclusionMaskPass : ScriptableRenderPass
    {
        private Settings _settings;
        private Material _maskMaterial;

        public OcclusionMaskPass(Settings settings)
        {
            _settings = settings;
            if (_settings.HighlightMaskShader != null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(_settings.HighlightMaskShader);
        }

        private class PassData 
        {
            public TextureHandle maskTexture;
            public TextureHandle depthTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_maskMaterial == null) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.graphicsFormat = GraphicsFormat.R8_UNorm;

            // Pass 1: Render Layer 6 silhouettes WITH DEPTH to mask texture
            TextureHandle maskTex = renderGraph.CreateTexture(
                new TextureDesc(desc.width, desc.height)
                {
                    colorFormat = GraphicsFormat.R8_UNorm,
                    depthBufferBits = DepthBits.None,
                    msaaSamples = (MSAASamples) desc.msaaSamples,
                    name = _settings.MaskTextureName,
                    clearBuffer = true,
                    clearColor = Color.clear
                });

            TextureHandle depthTex = renderGraph.CreateTexture(
                new TextureDesc(desc.width, desc.height)
                {
                    colorFormat = GraphicsFormat.None,
                    depthBufferBits = DepthBits.Depth32,
                    msaaSamples = (MSAASamples) desc.msaaSamples,
                    name = _settings.DepthTextureName,
                    clearBuffer = true,
                    clearColor = Color.clear
                });

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Render Highlight Mask", out PassData passData))
            {
                builder.SetRenderAttachment(maskTex, 0);
                builder.SetRenderAttachmentDepth(depthTex, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    _maskMaterial.SetFloat("_Cutoff", _settings.AlphaCutoff);
                    
                    // Draw all registered highlightable objects to mask
                    foreach (var rend in Highlightable.AllHighlightables)
                    {
                        if (rend == null) continue;
                        MeshFilter mf = rend.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            ctx.cmd.DrawMesh(mf.sharedMesh, rend.transform.localToWorldMatrix, _maskMaterial);
                        }
                    }
                });
            }

            // Pass 2: Expose mask and depth textures globally for Layer 7 shaders
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Expose Highlight Mask", out PassData exposeData))
            {
                exposeData.maskTexture = maskTex;
                exposeData.depthTexture = depthTex;
                
                builder.UseTexture(maskTex);
                builder.UseTexture(depthTex);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    // Make mask and depth available to all shaders
                    ctx.cmd.SetGlobalTexture(_settings.MaskTextureName, data.maskTexture);
                    ctx.cmd.SetGlobalTexture(_settings.DepthTextureName, data.depthTexture);
                    ctx.cmd.SetGlobalColor("_HighlightColor", _settings.HighlightColor);
                });
            }
        }
    }

    private OcclusionMaskPass _maskPass;

    public override void Create()
    {
        _maskPass = new OcclusionMaskPass(settings)
        {
            // Run before Layer 7 objects are rendered
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_maskPass);
    }
}