Shader "Unlit/ToonShader"
{
    Properties
    {
        [MainTexture] _ColorMap ("Color Map", 2D) = "white" {}
        [MainColor] _Color  ("Color", Color) = (0.91, 0.91, 0.38)
        _Smoothness ("Smoothness", Float) = 16.0
        _RimSharpness ("Rim Sharpness", Float) = 16.0
        [HDR] _RimColor ("Rim Color", Color) = (1.0, 1.0, 1.0)
        _WorldColor ("World Color", Color) = (0.1, 0.1, 0.1)
        
        [Toggle(_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionStrength ("Emission Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Cull Back
        ZWrite On
        ZTest LEqual
        ZClip Off
        
        Pass
        {
           Name "ForwardLit"
           Tags {"LightMode" = "UniversalForwardOnly"}
           
           HLSLPROGRAM

           #pragma multi_compile_fog
           #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
           
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
           #pragma multi_compile _ _SHADOWS_SOFT

           #pragma shader_feature _EMISSION

           #pragma vertex Vertex
           #pragma fragment Fragment

           #include "ToonShaderPass.hlsl"
           
           ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}
            
            HLSLPROGRAM
            #define SHADOW_CASTER_PASS

            #pragma vertex Vertex
            #pragma fragment FragmentDepthOnly

            #include "ToonShaderPass.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode" = "DepthOnly"}
            
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment FragmentDepthOnly

            #include "ToonShaderPass.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags {"LightMode" = "DepthNormalsOnly"}
            
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment FragmentDepthNormalsOnly

            #include "ToonShaderPass.hlsl"
            
            ENDHLSL
        }
    }
}
