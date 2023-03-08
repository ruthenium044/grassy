Shader "Custom/SimpleUnlit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader{
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Cull Off

        Pass {
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct DrawVertex {
                float3 positionWS;
                float2 uv;
            };

            struct DrawTriangle {
                float3 normalWS;
                DrawVertex vertices[3];
            };

            StructuredBuffer<DrawTriangle> _DrawTriangles;
            
            struct VertexOutput {
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 positionCS   : SV_POSITION;
            };
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
            
            VertexOutput Vertex(uint vertexID: SV_VertexID) {
                VertexOutput output = (VertexOutput)0;
                 DrawTriangle tri = _DrawTriangles[vertexID / 3];
                DrawVertex input = tri.vertices[vertexID % 3];
            
                output.positionWS = input.positionWS;
                output.normalWS = tri.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                output.positionCS = TransformWorldToHClip(input.positionWS);
                return output;
            }
 
            float4 Fragment(VertexOutput input) : SV_Target {
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return albedo;
            }
            ENDHLSL
        }
    }
}