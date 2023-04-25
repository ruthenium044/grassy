Shader "Unlit/Eyball"
{
    Properties
    {
        _Color1("Color1", Color) = (1, 1, 1, 1)
        _Color2("Color2", Color) = (1, 1, 1, 1)
        _Color3("Color3", Color) = (1, 1, 1, 1)
    }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
    struct Attributes
    {
        float2 uv          : TEXCOORD0;
        float4 positionOS  : POSITION;
    };
    
    struct Varyings
    {
        float4 positionHCS : SV_POSITION;
        float2 uv          : TEXCOORD0;
    };
   
    half4 _Color1;
    half4 _Color2;
    half4 _Color3;
   
    //float circle(float2 _st, float _radius) {
    //    float2 dist = _st - float2(0.5);
    //    return 1.0 - smoothstep(_radius - (_radius * 0.01), _radius + (_radius * 0.01), dot(dist,dist) * 4.0);
    //}

    Varyings vert (Attributes input) {
        Varyings o;
        o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
        o.uv = input.uv;
        return o;
    }
    
    float4 frag (Varyings input) : SV_Target {
        //float3 dist = distance(0, float3(2.0, 2.0, 1.0));
        //float irisRadius = 1 - saturate(dist / 0.1f);
        
        float t = step(input.uv.y, 0.7);
        float t2 = step(input.uv.y, 0.8);
        float3 lerpedColor = lerp(_Color3.xyz, _Color2.xyz, t2);
        float3 col = lerp(lerpedColor.xyz, _Color1.xyz, t);
        return float4(col.xyz, 1);
    }
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
                        
            #pragma vertex vert
            #pragma fragment frag
            
            ENDHLSL
        }
    }
}
