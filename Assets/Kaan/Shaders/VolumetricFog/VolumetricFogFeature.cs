using UnityEngine;
using UnityEngine.Profiling;                    // <-- for BeginSample / EndSample labels
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricFogFeature : ScriptableRendererFeature
{
    public enum InjectionPoint
    {
        BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
        BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
        AfterRenderingPostProcessing  = RenderPassEvent.AfterRenderingPostProcessing
    }

    public enum Downsample
    {
        x1 = 1,
        x2 = 2,
        x4 = 4
    }

    [Header("Execution")]
    public InjectionPoint injectionPoint = InjectionPoint.BeforeRenderingPostProcessing;

    [Tooltip("Material using shader \"Unlit/VolumetricFog\"")]
    public Material fogMaterial;

    [Tooltip("Shader pass index (usually 0).")]
    public int passIndex = 0;

    [Header("Quality")]
    [Tooltip("Render the fog at lower resolution, then upsample.")]
    public Downsample downsample = Downsample.x1;

    [Header("Buffers")]
    [Tooltip("Bind camera depth-stencil while drawing (not required for Blit path; kept for legacy).")]
    public bool bindDepthStencilAttachment = false;

    private FullscreenFogPass _pass;

    public override void Create()
    {
        _pass = new FullscreenFogPass(name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (UniversalRenderer.IsOffscreenDepthTexture(in renderingData.cameraData) ||
            renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        if (fogMaterial == null)
        {
            Debug.LogWarning($"[{nameof(VolumetricFogFeature)}] Assign a fog material (shader \"Unlit/VolumetricFog\").");
            return;
        }

        if (passIndex < 0 || passIndex >= fogMaterial.passCount)
        {
            Debug.LogWarning($"[{nameof(VolumetricFogFeature)}] passIndex out of range for material \"{fogMaterial.name}\".");
            return;
        }

        _pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        _pass.renderPassEvent = (RenderPassEvent)injectionPoint;
        _pass.Setup(fogMaterial, passIndex, downsample, bindDepthStencilAttachment);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    // ---------------- PASS ----------------
    class FullscreenFogPass : ScriptableRenderPass
    {
        private Material _mat;
        private int _passIndex;
        private Downsample _downsample;
        private bool _bindDepthStencil;

        private RTHandle _rtLow;     // downsampled target (optional)
        private RTHandle _rtTemp;    // temp when downsample is x1 (ping-pong)

        public FullscreenFogPass(string profilerName)
        {
            profilingSampler = new ProfilingSampler(profilerName);
        }

        public void Setup(Material m, int passIndex, Downsample downsample, bool bindDepth)
        {
            _mat = m;
            _passIndex = passIndex;
            _downsample = downsample;
            _bindDepthStencil = bindDepth;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();

            // Make the top-level profiler sample label reflect the resolution (nice in Profiler)
            profilingSampler = new ProfilingSampler(
                _downsample == Downsample.x1 ? "VolumetricFog (x1)" :
                _downsample == Downsample.x2 ? "VolumetricFog (x2)" :
                                               "VolumetricFog (x4)");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;

            if (_downsample == Downsample.x1)
            {
                RenderingUtils.ReAllocateIfNeeded(ref _rtTemp, desc, name: "_VolumetricFog_Temp");
                _rtLow?.Release();
                _rtLow = null;
            }
            else
            {
                int factor = (int)_downsample;
                desc.width  = Mathf.Max(1, desc.width  / factor);
                desc.height = Mathf.Max(1, desc.height / factor);

                RenderingUtils.ReAllocateIfNeeded(ref _rtLow, desc, name: "_VolumetricFog_Low");
                _rtTemp?.Release();
                _rtTemp = null;
            }
        }

        public void Dispose()
        {
            _rtLow?.Release();
            _rtTemp?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilingSampler.name);

            using (new ProfilingScope(cmd, profilingSampler))
            {
                ref var camData = ref renderingData.cameraData;
                var camColor = camData.renderer.cameraColorTargetHandle;

                if (_downsample == Downsample.x1)
                {
                    // ---- MATERIAL RAYMARCH BLIT (x1) ----
                    cmd.BeginSample("VolumetricFog: MaterialBlit (x1)");
                    Blitter.BlitCameraTexture(cmd, camColor, _rtTemp, _mat, _passIndex);
                    cmd.EndSample("VolumetricFog: MaterialBlit (x1)");

                    // ---- COMPOSITE BACK (x1) ----
                    cmd.BeginSample("VolumetricFog: CompositeBack (x1)");
                    Blitter.BlitCameraTexture(cmd, _rtTemp, camColor);
                    cmd.EndSample("VolumetricFog: CompositeBack (x1)");
                }
                else if (_downsample == Downsample.x2)
                {
                    // ---- MATERIAL RAYMARCH BLIT (x2) ----
                    cmd.BeginSample("VolumetricFog: MaterialBlit (x2)");
                    Blitter.BlitCameraTexture(cmd, camColor, _rtLow, _mat, _passIndex);
                    cmd.EndSample("VolumetricFog: MaterialBlit (x2)");

                    // ---- UPSAMPLE + COMPOSITE (x2) ----
                    cmd.BeginSample("VolumetricFog: UpsampleComposite (x2)");
                    Blitter.BlitCameraTexture(cmd, _rtLow, camColor);
                    cmd.EndSample("VolumetricFog: UpsampleComposite (x2)");
                }
                else // x4
                {
                    // ---- MATERIAL RAYMARCH BLIT (x4) ----
                    cmd.BeginSample("VolumetricFog: MaterialBlit (x4)");
                    Blitter.BlitCameraTexture(cmd, camColor, _rtLow, _mat, _passIndex);
                    cmd.EndSample("VolumetricFog: MaterialBlit (x4)");

                    // ---- UPSAMPLE + COMPOSITE (x4) ----
                    cmd.BeginSample("VolumetricFog: UpsampleComposite (x4)");
                    Blitter.BlitCameraTexture(cmd, _rtLow, camColor);
                    cmd.EndSample("VolumetricFog: UpsampleComposite (x4)");
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
