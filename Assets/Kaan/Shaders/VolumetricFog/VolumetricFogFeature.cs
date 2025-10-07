using UnityEngine;
using UnityEngine.Profiling;                    // <-- for BeginSample / EndSample labels
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;



public class VolumetricFogFeature : ScriptableRendererFeature
{
    
    [Header("Baked Volume (optional)")]
    public BakedVolumeAsset bakedAsset;
    
    
    [Header("Baked analytic lights (optional)")]
    public BakedVolumetricLights bakedLights;

 
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
        _pass.Setup(fogMaterial, passIndex, downsample, bindDepthStencilAttachment, bakedAsset, bakedLights);
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
        
        public BakedVolumetricLights bakedLights;
        const int MAX_BAKED_POINTS = 16;
        const int MAX_BAKED_SPOTS  = 16;

        
        static readonly int _BakedVolumeTex    = Shader.PropertyToID("_BakedVolumeTex");
        static readonly int _BakedVolumeCenter = Shader.PropertyToID("_BakedVolumeCenterWS");
        static readonly int _BakedVolumeSize   = Shader.PropertyToID("_BakedVolumeSizeWS");
        static readonly int _BakedDensityScale = Shader.PropertyToID("_BakedDensityScale");
        static readonly int _BakedBlend        = Shader.PropertyToID("_BakedBlend");
        
        
        static readonly int _BakedPointCount    = Shader.PropertyToID("_BakedPointCount");
        static readonly int _BakedPointPosRange = Shader.PropertyToID("_BakedPointPosRange"); // float4(x,y,z,range)
        static readonly int _BakedPointColInt   = Shader.PropertyToID("_BakedPointColInt");   // float4(r,g,b,intensity)

        static readonly int _BakedSpotCount     = Shader.PropertyToID("_BakedSpotCount");
        static readonly int _BakedSpotPosRange  = Shader.PropertyToID("_BakedSpotPosRange");
        static readonly int _BakedSpotDirCos    = Shader.PropertyToID("_BakedSpotDirCos");    // float4(dx,dy,dz, cosInner)
        static readonly int _BakedSpotColIntCos = Shader.PropertyToID("_BakedSpotColIntCos"); // float4(r,g,b,intensity), cosOuter in .w? we already used .w, so put cosOuter separately:
        static readonly int _BakedSpotCosOuter  = Shader.PropertyToID("_BakedSpotCosOuter");

        private BakedVolumeAsset _baked;

        public FullscreenFogPass(string profilerName)
        {
            profilingSampler = new ProfilingSampler(profilerName);
        }

        public void Setup(Material m, int passIndex, Downsample downsample, bool bindDepth, 
            BakedVolumeAsset baked = null, BakedVolumetricLights lights = null)
        {
            _mat = m;
            _passIndex = passIndex;
            _downsample = downsample;
            _bindDepthStencil = bindDepth;
            _baked = baked;
            bakedLights = lights;   // <-- store it
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
                
                // guard null
                if (bakedLights != null)
                {
                    // --- Points
                    var pList = bakedLights.points;
                    int pc = Mathf.Min(pList.Count, MAX_BAKED_POINTS);
                    var pPosR = new Vector4[pc];
                    var pColI = new Vector4[pc];
                    for (int i = 0; i < pc; i++)
                    {
                        pPosR[i] = new Vector4(pList[i].positionWS.x, pList[i].positionWS.y, pList[i].positionWS.z, pList[i].range);
                        var c = (Vector4)pList[i].color.linear;
                        pColI[i] = new Vector4(c.x, c.y, c.z, pList[i].intensity);
                    }
                    cmd.SetGlobalInt(_BakedPointCount, pc);
                    if (pc > 0) {
                        cmd.SetGlobalVectorArray(_BakedPointPosRange, pPosR);
                        cmd.SetGlobalVectorArray(_BakedPointColInt,   pColI);
                    }

                    // --- Spots
                    var sList = bakedLights.spots;
                    int sc = Mathf.Min(sList.Count, MAX_BAKED_SPOTS);
                    var sPosR  = new Vector4[sc];
                    var sDirCi = new Vector4[sc];
                    var sColI  = new Vector4[sc];
                    var sCosO  = new float[sc];
                    for (int i = 0; i < sc; i++)
                    {
                        sPosR[i]  = new Vector4(sList[i].positionWS.x, sList[i].positionWS.y, sList[i].positionWS.z, sList[i].range);
                        sDirCi[i] = new Vector4(sList[i].directionWS.normalized.x, sList[i].directionWS.normalized.y, sList[i].directionWS.normalized.z, sList[i].cosInner);
                        var c = (Vector4)sList[i].color.linear;
                        sColI[i]  = new Vector4(c.x, c.y, c.z, sList[i].intensity);
                        sCosO[i]  = sList[i].cosOuter;
                    }
                    cmd.SetGlobalInt(_BakedSpotCount, sc);
                    if (sc > 0) {
                        cmd.SetGlobalVectorArray(_BakedSpotPosRange,  sPosR);
                        cmd.SetGlobalVectorArray(_BakedSpotDirCos,    sDirCi);
                        cmd.SetGlobalVectorArray(_BakedSpotColIntCos, sColI);
                        cmd.SetGlobalFloatArray (_BakedSpotCosOuter,  sCosO);
                    }
                }
                else
                {
                    cmd.SetGlobalInt(_BakedPointCount, 0);
                    cmd.SetGlobalInt(_BakedSpotCount,  0);
                }

                if (_baked != null && _baked.volumeTex != null)
                {
                    _mat.EnableKeyword("_USE_BAKED_VOLUME");   // << use material keyword
                    cmd.SetGlobalTexture(_BakedVolumeTex, _baked.volumeTex);
                    cmd.SetGlobalVector (_BakedVolumeCenter, _baked.volumeCenterWS);
                    cmd.SetGlobalVector (_BakedVolumeSize,   _baked.volumeSizeWS);
                    cmd.SetGlobalFloat  (_BakedDensityScale, _baked.densityScale);
                    cmd.SetGlobalFloat  (_BakedBlend,        _baked.bakedBlend);

                    // (also push to material to be extra-safe)
                    _mat.SetTexture("_BakedVolumeTex", _baked.volumeTex);
                    _mat.SetVector ("_BakedVolumeCenterWS", _baked.volumeCenterWS);
                    _mat.SetVector ("_BakedVolumeSizeWS",   _baked.volumeSizeWS);
                    _mat.SetFloat  ("_BakedDensityScale",   _baked.densityScale);
                    _mat.SetFloat  ("_BakedBlend",          _baked.bakedBlend);
                }
                else
                {
                    _mat.DisableKeyword("_USE_BAKED_VOLUME");
                }

                
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
