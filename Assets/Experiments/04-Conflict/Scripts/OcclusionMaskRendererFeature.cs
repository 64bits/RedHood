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
        public string MaskTextureName = "_OccluderMaskTex";
        public LayerMask OccluderLayer = 1 << 7;
        public float AlphaCutoff = 0.5f;
    }

    public Settings settings = new();

    class OcclusionMaskPass : ScriptableRenderPass
    {
        private Settings _settings;
        private Material _maskMaterial;
        private List<Renderer> _occluders = new();

        public OcclusionMaskPass(Settings settings)
        {
            _settings = settings;
            if (_settings.OccluderMaskShader != null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(_settings.OccluderMaskShader);
        }

        public void CollectOccluders()
        {
            _occluders.Clear();
            foreach (var go in Object.FindObjectsOfType<Renderer>())
            {
                if (((1 << go.gameObject.layer) & _settings.OccluderLayer) != 0)
                    _occluders.Add(go);
            }
        }

        private class PassData { }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_maskMaterial == null || _occluders.Count == 0)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.graphicsFormat = GraphicsFormat.R8_UNorm;

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

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>(
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

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>(
                "Expose OccluderMask", out PassData passData2))
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
        _maskPass.CollectOccluders();
        renderer.EnqueuePass(_maskPass);
    }
}
