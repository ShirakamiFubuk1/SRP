﻿#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

#include "Surface.hlsl"
#include "Common.hlsl"

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range(1-metallic);
}

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;

    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    
    brdf.diffuse = surface.color * oneMinusReflectivity;
    brdf.specular = lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    return brdf;
}



#endif