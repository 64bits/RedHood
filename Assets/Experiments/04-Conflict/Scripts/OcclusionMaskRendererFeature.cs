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
        public Shader OccluderMaskShader;
        public Shader HighlightBlitShader;
        public string MaskTextureName = "_OccluderMaskTex";
        public LayerMask OccluderLayer = 1 << 7;
        public LayerMask HighlightLayer = 1 << 6; // Layer to highlight
        public float AlphaCutoff = 0.5f;
        public Color HighlightColor = Color.yellow;
    }

    public Settings settings = new();

    class OcclusionMaskPass : ScriptableRenderPass
    {
        private Settings _settings;
        private Material _maskMaterial;
        private Material _blitMaterial; 
        private List<Renderer> _occluders = new();
        private List<Renderer> _highlightables = new(); // Objects that can be highlighted

        public OcclusionMaskPass(Settings settings)
        {
            _settings = settings;
            if (_settings.OccluderMaskShader != null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(_settings.OccluderMaskShader);
            if (_settings.HighlightBlitShader != null)
                _blitMaterial = CoreUtils.CreateEngineMaterial(_settings.HighlightBlitShader);
        }

        public void CollectRenderers()
        {
            _occluders.Clear();
            _highlightables.Clear();
            
            foreach (var go in Object.FindObjectsOfType<Renderer>())
            {
                int layer = 1 << go.gameObject.layer;
                if ((layer & _settings.OccluderLayer) != 0)
                    _occluders.Add(go);
                if ((layer & _settings.HighlightLayer) != 0)
                    _highlightables.Add(go);
            }
        }

        private class PassData 
        {
            public TextureHandle maskTexture;
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

            // Pass 1: Create occlusion mask (same as before)
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

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Render Occluder Mask", out PassData passData))
            {
                builder.SetRenderAttachment(maskTex, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    _maskMaterial.SetFloat("_Cutoff", _settings.AlphaCutoff);
                    foreach (var rend in _occluders)
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

            // Pass 2: Draw yellow pixels where layer 6 objects are occluded
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Highlight Occluded Areas", out PassData highlightData))
            {
                highlightData.maskTexture = maskTex;
                
                builder.UseTexture(maskTex);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true); 

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_MaskTex", data.maskTexture);
                    ctx.cmd.SetGlobalColor("_HighlightColor", _settings.HighlightColor);
                    
                    // Draw a fullscreen quad that reads the mask and outputs yellow pixels
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, _blitMaterial, 0, MeshTopology.Triangles, 3);
                });
            }

            // Pass 3: Expose texture for other shaders (optional)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Expose OccluderMask", out PassData exposeData))
            {
                builder.UseTexture(maskTex);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture(_settings.MaskTextureName, maskTex);
                });
            }
        }
    }

    private OcclusionMaskPass _maskPass;

    public override void Create()
    {
        _maskPass = new OcclusionMaskPass(settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _maskPass.CollectRenderers();
        renderer.EnqueuePass(_maskPass);
    }
}
