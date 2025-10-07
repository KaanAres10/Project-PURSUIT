using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricFogFeature : ScriptableRendererFeature
{
    [Header("Fog Settings")]
    public Material fogMaterial;
    public int passIndex = 0;

    [Header("Execution")]
    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    [Header("Baked Volume (optional)")]
    public BakedVolumeAsset bakedAsset;

    [Header("Baked Analytic Lights (optional)")]
    public BakedVolumetricLights bakedLights;

    [Header("Dynamic Spotlight (optional)")]
    public Light spotlight; // assign a Unity Light here (e.g., a real Spot light in the scene)
    
    public enum Downsample { x1 = 1, x2 = 2, x4 = 4 }
    public Downsample downsample = Downsample.x1;

    private FullscreenFogPass _pass;

    public override void Create()
    {
        _pass = new FullscreenFogPass(name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (fogMaterial == null) return;

        _pass.Setup(fogMaterial, passIndex, downsample, bakedAsset, bakedLights, spotlight);
        _pass.renderPassEvent = injectionPoint;
        _pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(_pass);
    }

    // =======================================================================
    class FullscreenFogPass : ScriptableRenderPass
    {
        private Material _mat;
        private int _passIndex;
        private Downsample _downsample;

        private BakedVolumeAsset _baked;
        private BakedVolumetricLights _bakedLights;
        private Light _spotlight;

        private RTHandle _rtLow;
        private RTHandle _rtTemp;

        // baked volume properties
        static readonly int _BakedVolumeTex    = Shader.PropertyToID("_BakedVolumeTex");
        static readonly int _BakedVolumeCenter = Shader.PropertyToID("_BakedVolumeCenterWS");
        static readonly int _BakedVolumeSize   = Shader.PropertyToID("_BakedVolumeSizeWS");
        static readonly int _BakedDensityScale = Shader.PropertyToID("_BakedDensityScale");
        static readonly int _BakedBlend        = Shader.PropertyToID("_BakedBlend");

        // spotlight properties
        static readonly int _SpotPosWS      = Shader.PropertyToID("_SpotPosWS");
        static readonly int _SpotDirWS      = Shader.PropertyToID("_SpotDirWS");
        static readonly int _SpotRange      = Shader.PropertyToID("_SpotRange");
        static readonly int _SpotInnerDeg   = Shader.PropertyToID("_SpotInnerDeg");
        static readonly int _SpotOuterDeg   = Shader.PropertyToID("_SpotOuterDeg");
        static readonly int _SpotColor      = Shader.PropertyToID("_SpotColor");
        static readonly int _SpotIntensity  = Shader.PropertyToID("_SpotIntensity");

        public FullscreenFogPass(string profilerName)
        {
            profilingSampler = new ProfilingSampler(profilerName);
        }

        public void Setup(Material mat, int passIndex, Downsample downsample,
                          BakedVolumeAsset baked, BakedVolumetricLights bakedLights, Light spot)
        {
            _mat = mat;
            _passIndex = passIndex;
            _downsample = downsample;
            _baked = baked;
            _bakedLights = bakedLights;
            _spotlight = spot;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            if (_downsample == Downsample.x1)
            {
                RenderingUtils.ReAllocateIfNeeded(ref _rtTemp, desc, name: "_VolumetricFog_Temp");
                _rtLow?.Release();
            }
            else
            {
                int factor = (int)_downsample;
                desc.width  = Mathf.Max(1, desc.width  / factor);
                desc.height = Mathf.Max(1, desc.height / factor);
                RenderingUtils.ReAllocateIfNeeded(ref _rtLow, desc, name: "_VolumetricFog_Low");
                _rtTemp?.Release();
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("VolumetricFog");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                var camColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // ---- spotlight injection ----
                if (_spotlight != null && _spotlight.type == LightType.Spot)
                {
                    var dir = _spotlight.transform.forward.normalized;
                    var pos = _spotlight.transform.position;
                    float inner = _spotlight.innerSpotAngle * 0.5f;
                    float outer = _spotlight.spotAngle * 0.5f;

                    cmd.SetGlobalVector(_SpotPosWS, pos);
                    cmd.SetGlobalVector(_SpotDirWS, dir);
                    cmd.SetGlobalFloat(_SpotRange, _spotlight.range);
                    cmd.SetGlobalFloat(_SpotInnerDeg, inner);
                    cmd.SetGlobalFloat(_SpotOuterDeg, outer);
                    cmd.SetGlobalVector(_SpotColor, _spotlight.color.linear);
                    cmd.SetGlobalFloat(_SpotIntensity, _spotlight.intensity);
                }

                // ---- baked volume ----
                if (_baked != null && _baked.volumeTex != null)
                {
                    cmd.SetGlobalTexture(_BakedVolumeTex, _baked.volumeTex);
                    cmd.SetGlobalVector (_BakedVolumeCenter, _baked.volumeCenterWS);
                    cmd.SetGlobalVector (_BakedVolumeSize,   _baked.volumeSizeWS);
                    cmd.SetGlobalFloat  (_BakedDensityScale, _baked.densityScale);
                    cmd.SetGlobalFloat  (_BakedBlend,        _baked.bakedBlend);
                    _mat.EnableKeyword("_USE_BAKED_VOLUME");
                }
                else
                {
                    _mat.DisableKeyword("_USE_BAKED_VOLUME");
                }

                // ---- draw pass ----
                if (_downsample == Downsample.x1)
                {
                    Blitter.BlitCameraTexture(cmd, camColor, _rtTemp, _mat, _passIndex);
                    Blitter.BlitCameraTexture(cmd, _rtTemp, camColor);
                }
                else
                {
                    Blitter.BlitCameraTexture(cmd, camColor, _rtLow, _mat, _passIndex);
                    Blitter.BlitCameraTexture(cmd, _rtLow, camColor);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _rtLow?.Release();
            _rtTemp?.Release();
        }
    }
}
