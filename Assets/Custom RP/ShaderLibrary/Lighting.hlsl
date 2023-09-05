#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
//发送给GPU的灯光数据

#include "Light.hlsl"
#include "BRDF.hlsl"
#include "Surface.hlsl"

float3 IncomingLight(Surface surface,Light light)
{
    return saturate(dot(surface.normal,light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf,Light light)
{
    //乘上BRDF反射率
    return IncomingLight(surface,light) * DirectBRDF(surface,brdf,light);
}

float3 GetLighting(Surface surfaceWS ,BRDF brdf)
{
    ShadowData shadowData = GetShadowData(surfaceWS);
    float3 color = 0.0;
    for(int i=0;i<GetDirectionalLightCount();i++)
    {
        Light light = GetDirectionalLight(i,surfaceWS,shadowData);
        color += GetLighting(surfaceWS, brdf ,light);
    }
    return color;
}

#endif