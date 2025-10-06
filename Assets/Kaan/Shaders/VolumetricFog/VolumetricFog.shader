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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #pragma multi_compile _ _ADDITIONAL_LIGHTS           
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS     

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
                

                while (distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled; 
                    float density = getDensity(rayPos);
                    if (density > 0)
                    {
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        fogCol.rgb += mainLight.color.rgb * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) *  density * mainLight.shadowAttenuation * _StepSize;


                        #ifdef _ADDITIONAL_LIGHTS
                        int addCount = GetAdditionalLightsCount();
                        float maxLightPresence = 0.0;
                        [loop]
                        for (int li = 0; li < addCount; li++)
                        {
                            Light L = GetAdditionalLight(li, rayPos);
                            float cosTheat = dot(-rayDir, -L.direction);
                            float phase = henyey_greenstein(cosTheat, _LightScattering);
                            float atten = L.distanceAttenuation;

                            fogCol.rgb += L.color.rgb * _LightContribution.rgb * phase * density * atten * _StepSize;

                            maxLightPresence = max(maxLightPresence, saturate(atten));
                        }
                        #endif
                        
                        
                        float lightPresence = maxLightPresence; // 0..1
                        float litDensity = lerp(density, density * (1.0 - _LightClearsFog * lightPresence), _LightClearsFog);

                        // Single extinction update for the step
                        transmittance *= exp(-litDensity * _StepSize);

                        // (Minor perf win)
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