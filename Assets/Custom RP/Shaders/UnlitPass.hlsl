//include 保护，防止重新引入文件
#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// CBUFFER_START(UnityPerMaterial)
//     float4 _BaseColor;
// CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    //在Instanced buffer 中添加参数
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Vrayings
{
    float4 positionCS : SV_POSITION;
    //只是传递数据并不需要让GPU关注
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Attributes
{
    float3 positionOS:POSITION;
    float2 baseUV :TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Vrayings UnlitPassVertex(Attributes input)
{
    Vrayings output;
    UNITY_SETUP_INSTANCE_ID(input);
    //提供实例索引
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(positionWS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    //scale 存储在xy中 offset存储在ZW中
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

float4 UnlitPassFragment(Vrayings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 base = baseColor * baseMap;

    #if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
    #endif
    
    return base;
}

#endif