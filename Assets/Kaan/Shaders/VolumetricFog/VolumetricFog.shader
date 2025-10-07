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

        // -------- BAKED ----------
        [Toggle(_USE_BAKED_VOLUME)] _UseBaked("Use Baked Volume", Float) = 0
        _BakedVolumeTex("Baked Volume", 3D) = "" {}
        _BakedVolumeCenterWS("Baked Center (WS)", Vector) = (0,0,0,0)
        _BakedVolumeSizeWS("Baked Size (WS)", Vector) = (1,1,1,0)
        _BakedDensityScale("Baked Density Scale", Float) = 1
        _BakedBlend("Baked Light Blend", Range(0,1)) = 1
        [Toggle(_DEBUG_BAKED_PREVIEW)] _DebugBakedPreview("DEBUG: Show baked color", Float) = 0
        _BakedLightIntensity("Baked Light Intensity", Range(0,4)) = 1

        _PhaseG("Phase g (-0.9..0.9)", Range(-0.9,0.9)) = 0.2
        _BaseFogIntensity("Base Fog Intensity", Range(0,2)) = 1

        _SpotPosWS("Spot Pos (WS)", Vector) = (97.36, 8.52, -290.74, 0)
        _SpotDirWS("Spot Dir (WS, normalize)", Vector) = (0, -1, 0, 0)
        _SpotRange("Spot Range (m)", Float) = 20
        _SpotInnerDeg("Spot Inner (deg)", Range(0,89)) = 25
        _SpotOuterDeg("Spot Outer (deg)", Range(0,89)) = 40
        [HDR] _SpotColor("Spot Color", Color) = (1, 0.9, 0.7, 1)
        _SpotIntensity("Spot Intensity", Range(0,200)) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
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
            #pragma multi_compile _ _DEBUG_BAKED_PREVIEW

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

            // -------- BAKED ----------
            TEXTURE3D(_BakedVolumeTex);
            SAMPLER(sampler_BakedVolumeTex);

            float3 _BakedVolumeCenterWS;
            float3 _BakedVolumeSizeWS;
            float _BakedDensityScale;
            float _BakedBlend;

            int _BakedPointCount;
            int _BakedSpotCount;

            float4 _BakedPointPosRange[16];   // xyz, range
            float4 _BakedPointColInt[16];     // rgb, intensity
            float4 _BakedSpotPosRange[16];    // xyz, range
            float4 _BakedSpotDirCos[16];      // xyz = dir, w = cosInner
            float4 _BakedSpotColIntCos[16];   // rgb, intensity
            float _BakedSpotCosOuter[16];

            float _BakedLightIntensity;
            float _PhaseG;
            float _BaseFogIntensity;

            float3 _SpotPosWS;
            float3 _SpotDirWS;
            float _SpotRange;
            float _SpotInnerDeg;
            float _SpotOuterDeg;
            float4 _SpotColor;    // rgb
            float _SpotIntensity;

            float _LightClearsFog;

            // ------------------ Utility ------------------
            float pad01(float v, float eps) {
                v = saturate(v);
                return v * (1.0 - 2.0*eps) + eps;
            }

            float2 pad01(float2 v, float eps) {
                v = saturate(v);
                return v * (1.0 - 2.0*eps) + eps;
            }

            float3 pad01(float3 v, float eps) {
                v = saturate(v);
                return v * (1.0 - 2.0*eps) + eps;
            }

            float3 pad01Frac(float3 v, float eps) {
                v = frac(v);
                return v * (1.0 - 2.0*eps) + eps;
            }

            float3 BakedMinWS() {
                return _BakedVolumeCenterWS - 0.5 * _BakedVolumeSizeWS;
            }

            float3 BakedMaxWS() {
                return _BakedVolumeCenterWS + 0.5 * _BakedVolumeSizeWS;
            }

            bool InsideBaked(float3 wpos) {
                float3 p = wpos - _BakedVolumeCenterWS;
                float3 e = 0.5 * _BakedVolumeSizeWS;
                return all(p >= -e) && all(p <= e);
            }

            float3 WorldToBakedUV(float3 wpos) {
                float3 uvw = (wpos - BakedMinWS()) / _BakedVolumeSizeWS;
                return saturate(uvw);
            }

            // ------------------ Density ------------------
            float getProceduralDensity(float3 worldPos) {
                float3 p = worldPos * (0.01 * _NoiseTiling);
                p = pad01Frac(p, _EdgePad);
                float4 noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, p, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;
                return density;
            }

            float getDensity(float3 worldPos) {
                float3 p = worldPos * (0.01 * _NoiseTiling);
                p = pad01Frac(p, _EdgePad);
                float4 noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, p, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;
                return density;
            }

            // ------------------ Lighting ------------------
            float henyey_greenstein(float angle, float scattering) {
                return (1.0 - angle * angle) /
                       (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }

            float HenyeyGreenstein(float cosTheta, float g) {
                float denom = 1 + g*g - 2*g*cosTheta;
                return (1 - g*g) / (4*PI*pow(denom, 1.5));
            }

            float AttenInvSquare(float dist, float range) {
                float r = max(dist, 1e-3);
                float att = 1.0 / (1.0 + r*r);
                float s = saturate(1.0 - r / max(range, 1e-3));
                return att * s * s;
            }

            float SpotFalloff(float3 Ldir, float3 spotDir, float cosInner, float cosOuter) {
                float c = dot(-Ldir, normalize(spotDir));
                return smoothstep(cosOuter, cosInner, c);
            }

            float SpotFalloffCos(float cosL, float cosInner, float cosOuter) {
                return smoothstep(cosOuter, cosInner, cosL);
            }

            // ------------------ Intersection ------------------
            bool IntersectAABB(float3 ro, float3 rd, float3 bmin, float3 bmax, out float t0, out float t1) {
                float3 invD = 1.0 / rd;
                float3 tA = (bmin - ro) * invD;
                float3 tB = (bmax - ro) * invD;
                float3 tmin3 = min(tA, tB);
                float3 tmax3 = max(tA, tB);
                t0 = max(max(tmin3.x, tmin3.y), tmin3.z);
                t1 = min(min(tmax3.x, tmax3.y), tmax3.z);
                return t1 > max(t0, 0.0);
            }

            // ------------------ Fragment ------------------
            half4 frag(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.texcoord;

                #if defined(UNITY_SINGLE_PASS_STEREO)
                uv = UnityStereoTransformScreenSpaceTex(uv);
                #endif

                float4 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                float depth = SampleSceneDepth(uv);

                // Build ray
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(worldPos - ro);

                // Intersect with baked volume AABB
                float3 bmin = _BakedVolumeCenterWS - 0.5 * _BakedVolumeSizeWS;
                float3 bmax = _BakedVolumeCenterWS + 0.5 * _BakedVolumeSizeWS;

                float tEnter, tExit;
                if (!IntersectAABB(ro, rd, bmin, bmax, tEnter, tExit))
                    return sceneColor;

                // March range (stop at scene depth so we don't draw behind geometry)
                float distToSurface = length(worldPos - ro);
                float t0 = max(0.0, tEnter);
                float t1 = min(min(_MaxDistance, distToSurface), tExit);

                // Per-pixel jitter to reduce banding
                float2 pixelCoords = uv * _BlitTexture_TexelSize.zw;
                float jitter = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x)));

                float t = t0 + jitter * _StepSize;

                // Baked-only accumulation (no lights, no baked RGB)
                float3 accum = 0;
                float T = 1.0; // transmittance

                [loop]
                for (; t < t1; t += _StepSize)
                {
                    float3 posWS = ro + rd * t;
                    float3 uvw = (posWS - bmin) / _BakedVolumeSizeWS;
                    uvw = saturate(uvw);

                    float a = SAMPLE_TEXTURE3D(_BakedVolumeTex, sampler_BakedVolumeTex, uvw).a;
                    float density = saturate(a) * _BakedDensityScale * _DensityMultiplier;

                    if (density > 0)
                    {
                        float alpha = 1.0 - exp(-density * _StepSize);
                        float3 baseFog = _Color.rgb * _BaseFogIntensity;

                        // === Analytic SPOTLIGHT (single) ===
                        float3 S = 0;
                        {
                            float3 spotDir = normalize(_SpotDirWS);
                            float3 Lvec = _SpotPosWS - posWS;
                            float dist = length(Lvec);
                            float3 Ldir = Lvec / max(dist, 1e-3);

                            float cosL = dot(-Ldir, spotDir);
                            float cosInner = cos(radians(_SpotInnerDeg));
                            float cosOuter = cos(radians(_SpotOuterDeg));

                            float cone = SpotFalloffCos(cosL, cosInner, cosOuter);
                            if (cone > 0)
                            {
                                float att = AttenInvSquare(dist, _SpotRange) * cone;
                                float phase = HenyeyGreenstein(dot(rd, Ldir), _PhaseG);
                                float3 spotRGB = _SpotColor.rgb * _SpotIntensity;
                                S += spotRGB * (att * phase);
                            }
                        }


                        

                        // === Beer–Lambert composite for this step ===
                        accum += (baseFog + S) * (alpha * T);
                        T *= (1.0 - alpha);

                        if (T < 1e-3) break;
                    }
                }

                return lerp(sceneColor, float4(accum, 1), 1.0 - saturate(T));
            }
            ENDHLSL
        }
    }
}
