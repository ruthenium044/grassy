Shader "Custom/SimpleUnlit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _TopColor("Top Color", Color) = (1, 1, 1, 1)
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        _FadeAmount("Fade Amount", Range(0,1)) = 0.5
    }
    SubShader{
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "ForwardLit"
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
            
            Attributes Vertex(uint vertexID: SV_VertexID) {
                Attributes output = (Attributes)0;
                DrawTriangle tri = _DrawTriangles[vertexID / 3];
                DrawVertex input = tri.vertices[vertexID % 3];
            
                output.positionCS = TransformWorldToHClip(input.positionWS);
                output.positionWS = input.positionWS;
        
                float3 faceNormal = GetMainLight().direction * tri.normalWS;
                //output.normalWS = TransformObjectToWorldNormal(faceNormal, true);
                output.normalWS = tri.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }
 
            float4 Fragment(Attributes input) : SV_Target {
                float t = input.uv.y;
                float4 col = lerp(_BaseColor, _TopColor, t);
                
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 final = albedo.xyz * col.xyz;
                
                float fade = lerp(_FadeAmount, 1, t);
                
                //lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
           	    lightingInput.normalWS = input.normalWS;
           	    lightingInput.viewDirectionWS = normalize(GetCameraPositionWS() - input.positionWS);
                //lightingInput.shadowCoord = ComputeScreenPos(input.positionCS);
           	    
           	    SurfaceData surfaceInput = (SurfaceData)0;
           	    surfaceInput.albedo = final.xyz;
                surfaceInput.alpha = fade;
                surfaceInput.specular = 1;
                surfaceInput.smoothness = 1;

                return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);	
            }
            ENDHLSL
        }
    }
}