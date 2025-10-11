Shader "Unlit/VolumetricFog"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
        _NoiseOffset("Noise offset", float) = 0
        
        _FogNoise("Fog noise", 3D) = "white" {}
        _NoiseTiling("Noise tiling", float) = 1
        _DensityThreshold("Density threshold", Range(0, 1)) = 0.1
        
        [HDR] _LightContribution("Light contribution", Color) = (1, 1, 1, 1)
        _LightScattering("Light scattering", Range(0, 1)) = 0.2
        
        _EdgePad("Edge padding", Range(0, 0.1)) = 0.03
        
        _LightClearsFog("Light clears fog", Range(0,1)) = 0
        
        _BakedLightVolume("Baked Light Volume", 3D) = "black" {}
        _BLV_Origin("Volume Origin (world)", Vector) = (0,0,0,0)
        _BLV_Size("Volume Size (world)", Vector) = (10,10,10,0)
        _UseBakedVolume("Use Baked Volume", Float) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;
            float _NoiseOffset;
            float4 _BlitTexture_TexelSize;
            TEXTURE3D(_FogNoise);
            SAMPLER(sampler_FogNoise);
            float _DensityThreshold;
            float _NoiseTiling;
            float4 _LightContribution;
            float _LightScattering;

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _EdgePad;

            float  pad01(float v, float eps)   { v = saturate(v);   return v * (1.0 - 2.0*eps) + eps; }
            float2 pad01(float2 v, float eps)  { v = saturate(v);   return v * (1.0 - 2.0*eps) + eps; }
            float3 pad01(float3 v, float eps)  { v = saturate(v);   return v * (1.0 - 2.0*eps) + eps; }

            float3 pad01Frac(float3 v, float eps) { v = frac(v); return v * (1.0 - 2.0*eps) + eps; }

            float _LightClearsFog;


            TEXTURE3D(_BakedLightVolume);
            SAMPLER(sampler_BakedLightVolume);
            float3 _BLV_Origin;   // world-space min (x,y,z)
            float3 _BLV_Size;     // world-space size (x,y,z)
            float  _UseBakedVolume;



            float3 SampleBakedLight(float3 worldPos)
            {
                if (_UseBakedVolume < 0.5) return 0;
                float3 uvw = (worldPos - _BLV_Origin) / max(_BLV_Size, 1e-5);
                uvw = saturate(uvw);
                return SAMPLE_TEXTURE3D(_BakedLightVolume, sampler_BakedLightVolume, uvw).rgb;
            }
            
            
            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }

            float getDensity(float3 worldPos)
            {
                float3 p = worldPos * (0.01 * _NoiseTiling);
                p = pad01Frac(p, _EdgePad);
                
                float4 noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, p, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;
                return density;
            }
            
            half4 frag(Varyings IN): SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.texcoord;
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    uv = UnityStereoTransformScreenSpaceTex(uv);
                #endif
                uv = pad01(uv, _EdgePad);

                float4 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                float depth = SampleSceneDepth(uv);
                
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = uv * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                float transmittance = 1;
                float4 fogCol = _Color;
                

                float3 probePos = entryPoint + rayDir * (distLimit * 0.5);
                
                // Debug Light Volume
                //return float4(SampleBakedLight(probePos), 1);
                
                float3 Li_const = SampleBakedLight(probePos);                // 0..∞, depends on your baked volume values
                float  presence_const = saturate(dot(Li_const, float3(0.2126, 0.7152, 0.0722))); // luminance 0..1 for "light clears fog"
                
                while (distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;
    float  density = getDensity(rayPos);

    if (density > 0)
    {
        float stepLen = _StepSize;

        // Thin the fog by baked light presence (constant along the ray)
        float litDensity = lerp(density, density * (1.0 - _LightClearsFog * presence_const), _LightClearsFog);

        // Single-scattering for this segment using baked lighting only (constant along the ray)
        float3 scatterCol = _LightContribution.rgb * Li_const * (density * stepLen);
        fogCol.rgb += transmittance * scatterCol;

        // Extinction update
        transmittance *= exp(-litDensity * stepLen);
        if (transmittance < 1e-3) break;
    }

    distTravelled += _StepSize;
                }

                return lerp(sceneColor, fogCol, 1.0 - saturate(transmittance));
            }

            ENDHLSL
        }
    }
}