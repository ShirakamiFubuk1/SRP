﻿Shader "Custom RP/Lit" 
{
	
	Properties {
		
		_BaseMap("Texture",2D) = "white" {}
		_BaseColor("Base Color",Color) = (0.5,0.5,0.5,1.0)
		_Metallic("Metallic",Range(0,1)) = 0
		_Smoothness("Smoothness",Range(0,1)) = 0
		_CutOff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping",Float) = 0
		[Toggle(_PREMULTIPLY_ALPHA)] _PremultiplyAlpha("Premultiply Alpha",Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend",Float) = 1
		[Enum(Off,0,On,1)] _ZWrite ("ZWrite",Float) = 1
		
	}
	
	SubShader {
		Pass 
		{
			Tags
			{
				"LightMode" = "CustomLit"
			}
			
			Blend [_SrcBlend] [_DstBlend]
			
			HLSLPROGRAM

			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _PREMULTIPLY_ALPHA
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "LitPass.hlsl"
			
			ENDHLSL
		}
		
		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}
			
			ColorMask 0
			
			HLSLPROGRAM

			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			
			ENDHLSL
		}
	}
	
	CustomEditor "CustomShaderGUI"
	
}