Shader "Custom/SimpleGrassLit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _TopColor("Top Color", Color) = (1, 1, 1, 1)
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        _FadeAmount("Fade Amount", Range(0,1)) = 0.5
        _FadeSize("Fade Size", Range(0,1)) = 0.5
    }
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    
    struct DrawVertex {
        float3 positionWS;
        float2 uv;
    };

    struct DrawTriangle {
        float3 normalWS;
        DrawVertex vertices[3];
    };

    StructuredBuffer<DrawTriangle> _DrawTriangles;
    
    struct Attributes {
        float3 positionWS   : TEXCOORD0;
        float3 normalWS     : TEXCOORD1;
        float2 uv           : TEXCOORD2;
        float4 positionCS   : SV_POSITION;
    };
    
    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
    
    CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;            
        half4 _TopColor;            
    CBUFFER_END
    
    float _FadeAmount;
    float _FadeSize;
    
    Attributes Vertex(uint vertexID: SV_VertexID) {
        Attributes output = (Attributes)0;
        DrawTriangle tri = _DrawTriangles[vertexID / 3];
        DrawVertex input = tri.vertices[vertexID % 3];
    
        output.positionCS = TransformWorldToHClip(input.positionWS);
        output.positionWS = input.positionWS;
        
        output.normalWS = tri.normalWS;
        output.uv = TRANSFORM_TEX(input.uv, _MainTex);

        return output;
    }
            
    float4 Fragment(Attributes input) : SV_Target {

        #ifdef SHADERPASS_SHADOWCASTER
            return 0;
        #else
            float t = smoothstep(_FadeAmount - _FadeSize, _FadeAmount + _FadeSize, input.uv.y);
            float fade = lerp(0.0f, 1.0f, t);
            
            t =  input.uv.y;
            float4 col = lerp(_BaseColor, _TopColor, t);
            
            float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            float3 final = albedo.xyz * col.xyz;
            
            //lighting
            InputData lightingInput = (InputData)0;
            lightingInput.positionWS = input.positionWS;
            lightingInput.normalWS = normalize(input.normalWS);
            lightingInput.viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
            lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            
            SurfaceData surfaceInput = (SurfaceData)0;
            surfaceInput.albedo = final.xyz;
            surfaceInput.alpha = clamp(fade, 0, 1);
            surfaceInput.smoothness = 0;
            surfaceInput.specular = 1;  
            
            return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
        #endif 
    } 
    ENDHLSL
    
    SubShader{
        Tags {"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            AlphaToMask On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
        
        Pass {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            
            #pragma multi_compile_shadowcaster
            #pragma shader_feature_local _ DISTANCE_DETAIL
            #pragma vertex Vertex
            #pragma fragment Fragment
         
            #define SHADERPASS_SHADOWCASTER
            ENDHLSL
        }
    }
}