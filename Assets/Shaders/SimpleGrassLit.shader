Shader "Custom/SimpleGrassLit" {
    Properties { }
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    
    struct DrawVertex {
        float3 positionWS;
        float2 uv;
        float3 baseColor;
        float3 diffuseColor;
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
        float3 baseColor    : TEXCOORD3;
        float3 diffuseColor : COLOR;
    };
    
    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
    
    CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        half4 _TopColor;
    CBUFFER_END
    
    Attributes Vertex(uint vertexID: SV_VertexID) {
        Attributes output = (Attributes)0;
        DrawTriangle tri = _DrawTriangles[vertexID / 3];
        DrawVertex input = tri.vertices[vertexID % 3];
    
        output.positionCS = TransformWorldToHClip(input.positionWS);
        output.positionWS = input.positionWS;
        
        output.normalWS = tri.normalWS;
        output.uv = TRANSFORM_TEX(input.uv, _MainTex);
        output.baseColor = input.baseColor;
        output.diffuseColor = input.diffuseColor;
        return output;
    }
            
    float4 Fragment(Attributes input) : SV_Target {
        #ifdef SHADERPASS_SHADOWCASTER
            return 0;
        #else
            float t = smoothstep(0.1f, 1.0f, input.uv.y);
        
            float3 baseColor = _BaseColor.xyz;
            #if BLEND
                baseColor = input.baseColor.xyz;
            #endif
        
            float3 col = lerp(baseColor.xyz, _TopColor.xyz, t.xxx);
            float3 final = col.xyz * input.diffuseColor;

            #if GRASS_TEXTURE
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                final *= albedo.xyz;
            #endif
        
            //lighting
            InputData lightingInput = (InputData)0;
            lightingInput.positionWS = input.positionWS;
            lightingInput.normalWS = normalize(input.normalWS);
            lightingInput.viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
            lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            
            SurfaceData surfaceInput = (SurfaceData)0;
            surfaceInput.albedo = final.xyz;
            surfaceInput.alpha = 1.0f;
            surfaceInput.smoothness = 0;
            surfaceInput.specular = 1;  
            
            return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
        #endif 
    } 
    ENDHLSL
    
    SubShader{
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            Cull Off
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
            #pragma shader_feature BLEND
            #pragma shader_feature GRASS_TEXTURE
            
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