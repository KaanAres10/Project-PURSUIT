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
<<<<<<< HEAD
        
        _BakedLightVolume("Baked Light Volume", 3D) = "black" {}
        _BLV_Origin("Volume Origin (world)", Vector) = (0,0,0,0)
        _BLV_Size("Volume Size (world)", Vector) = (10,10,10,0)
        _UseBakedVolume("Use Baked Volume", Float) = 1
        
        _MaxSteps("Max March Steps", Range(1,1024)) = 256

=======
>>>>>>> 525fa3102d7fc94aad854c94a790206c3e7f2c19

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
<<<<<<< HEAD
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            
=======
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #pragma multi_compile _ _ADDITIONAL_LIGHTS           
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS     
>>>>>>> 525fa3102d7fc94aad854c94a790206c3e7f2c19

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

<<<<<<< HEAD

            TEXTURE3D(_BakedLightVolume);
            SAMPLER(sampler_BakedLightVolume);
            float3 _BLV_Origin;   // world-space min (x,y,z)
            float3 _BLV_Size;     // world-space size (x,y,z)
            float  _UseBakedVolume;

            float _MaxSteps;


           float3 SampleBakedLight(float3 worldPos)
{
    if (_UseBakedVolume < 0.5) return 0;

    float3 bmin = _BLV_Origin;
    float3 bmax = _BLV_Origin + _BLV_Size;

    // HARD OUT-OF-BOX CUTOFF (no sampling, no light)
    if (any(worldPos < bmin) || any(worldPos > bmax))
        return 0;

    // Inside: map to [0,1] WITHOUT saturate/clamp
    float3 uvw = (worldPos - bmin) / max(_BLV_Size, 1e-5);
    return SAMPLE_TEXTURE3D_LOD(_BakedLightVolume, sampler_BakedLightVolume, uvw, 0).rgb;
}
            
=======
>>>>>>> 525fa3102d7fc94aad854c94a790206c3e7f2c19
            
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

<<<<<<< HEAD
    float2 uv = IN.texcoord;
    #if defined(UNITY_SINGLE_PASS_STEREO)
    uv = UnityStereoTransformScreenSpaceTex(uv);
    #endif
    uv = pad01(uv, _EdgePad);
=======
                float2 uv = IN.texcoord;
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    uv = UnityStereoTransformScreenSpaceTex(uv);
                #endif
                uv = pad01(uv, _EdgePad);

                float4 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                float depth = SampleSceneDepth(uv);
                
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
>>>>>>> 525fa3102d7fc94aad854c94a790206c3e7f2c19

    float4 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
    float depth = SampleSceneDepth(uv);
    float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

<<<<<<< HEAD
    float3 entryPoint = _WorldSpaceCameraPos;
    float3 viewDir = worldPos - _WorldSpaceCameraPos;
    float viewLength = length(viewDir);
    float3 rayDir = normalize(viewDir);

    float2 pixelCoords = uv * _BlitTexture_TexelSize.zw;
=======
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
                            float cosTheat = dot(-rayDir, L.direction);
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
>>>>>>> 525fa3102d7fc94aad854c94a790206c3e7f2c19

    float distLimit = min(viewLength, _MaxDistance);

    float stepLen = max(_StepSize, 1e-4);
int   maxSteps = (int)min(_MaxSteps, ceil(distLimit / stepLen) + 1);
    
    float transmittance = 1.0;
    float4 fogCol = _Color;

    // --- DEBUG: visualize baked volume at midpoint ---
    // float3 dbgUVW = (entryPoint + rayDir * (distLimit * 0.5) - _BLV_Origin) / max(_BLV_Size, 1e-5);
    // return float4(SAMPLE_TEXTURE3D(_BakedLightVolume, sampler_BakedLightVolume, saturate(dbgUVW)).rgb, 1);

    const float3 LUMA = float3(0.2126, 0.7152, 0.0722);
float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;

   [loop]
for (int i = 0; i < maxSteps; ++i)
{
    if (distTravelled >= distLimit) break;

    float3 rayPos = entryPoint + rayDir * distTravelled;

    // 1) baked lighting at current position
    float3 Li = SampleBakedLight(rayPos);
    Li = min(Li, 10);
    float presence = saturate(dot(Li, LUMA));  // 0..1

    // 2) local density
    float density = getDensity(rayPos);

    if (density > 0)
    {
        // Thin fog locally
        float litDensity = lerp(density, density * (1.0 - _LightClearsFog * presence), _LightClearsFog);

        // Single scattering
        float3 scatterCol = _LightContribution.rgb * Li * (density * stepLen);
        fogCol.rgb += transmittance * scatterCol;

        // Beer-Lambert
        transmittance *= exp(-litDensity * stepLen);
        if (transmittance < 1e-3) break;
    }

    distTravelled += stepLen;
}

return lerp(sceneColor, fogCol, 1.0 - saturate(transmittance));
}
            ENDHLSL
        }
    }
}