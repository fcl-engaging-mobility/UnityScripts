// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: post-process shader that converts a monochrome texture into
//			a thermogram colored coded and blends it with the main texture.

Shader "Thermal/ThermalPostProcess"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "red" {}
		ThermalTexture("Thermal Texture", 2D) = "red" {}
		Opacity("Opacity", Range(0.0, 1.0)) = 0.5
		IndividualOpacity("Individual Opacity", Range(0.0, 1.0)) = 0.0
		Tint("Tint", Color) = (1,1,1)
	}
	SubShader
	{
		// No culling or depth
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "Monochromatic"
			CGPROGRAM
				inline float3 Thermogram(float value)
				{
					return saturate(value * Tint);
				}
			ENDCG
		}
		Pass
		{
			Name "Purple Thermogram"
			CGPROGRAM
				inline float3 Thermogram(float value)
				{
					float v = saturate(1.0 - pow(cos(value * HALF_PI), 4));
					float3 hsv = float3(
						fmod(v * 0.45 + 0.67, 1.0),
						saturate(cos(pow(value, 4) * HALF_PI)),
						saturate(log10(value + 0.19) * 1.2 + 0.91)
						);
					return hsv2rgb(hsv);
				}
			ENDCG
		}
		Pass
		{
			Name "Rainbow (Fast)"
			CGPROGRAM
				inline float3 Thermogram(float value)
				{
					float v = value * 1.16666 - 0.08333;
					float v2 = v * v;
					float r = 5 * v - 3;
					float g = 10 * v - 8.33333 * v2 - 1.66666;
					float b = 7.5 * v - 12.5 * v2;
					return saturate(float3(r, g, b));
				}
			ENDCG
		}
		Pass
		{
			Name "Rainbow (Acurate)"
			CGPROGRAM
				inline float3 Thermogram(float value)
				{
					float v = value * 1.16666 - 0.08333;
					float v2 = v * v, v3 = v2 * v, v4 = v3 * v;
					float r = -39.58333*v4 + 158.33333*v3 - 237.91666*v2 + 159.16666*v - 38.92;
					float g = -36.4583*v4 + 87.5*v3 - 79.7916*v2 + 32.75*v - 4;
					float b = -50*v4 + 60*v3 - 34.5*v2 + 9.9*v;
					return saturate(float3(r, g, b));
				}
			ENDCG
		}
	}
}

CGINCLUDE
#pragma vertex vert
#pragma fragment frag

struct appdata
{
	float4 pos : POSITION;
	float2 uv  : TEXCOORD0;
};

struct v2f
{
	float4 pos : SV_POSITION;
	float2 uv  : TEXCOORD0;
};

sampler2D _MainTex;
sampler2D ThermalTexture;
float Opacity;
float IndividualOpacity;
float3 Tint;

static const float4 HSV_TO_RGB_K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
static const float HALF_PI = 1.570796;

inline float3 hsv2rgb(float3 c)
{
	float3 p = abs(frac(c.xxx + HSV_TO_RGB_K.xyz) * 6.0 - HSV_TO_RGB_K.www);
	return c.z * lerp(HSV_TO_RGB_K.xxx, clamp(p - HSV_TO_RGB_K.xxx, 0.0, 1.0), c.y);
}

v2f vert(appdata v)
{
	v2f o;
	o.pos = mul(UNITY_MATRIX_MVP, v.pos);
	o.uv = v.uv;
	return o;
}

float3 Thermogram(float value);

fixed4 frag(v2f i) : SV_Target
{
	float4 scene = tex2D(_MainTex, i.uv);
	float2 thermalImage = tex2D(ThermalTexture, i.uv);

	// thermalImage.r => temperature
	// thermalImage.g => opacity

	float3 thermalColor = Thermogram(thermalImage.r);
	float opacity = Opacity * lerp(thermalImage.g, thermalImage.r, IndividualOpacity);
	return float4(lerp(scene.rgb, thermalColor, opacity), 1);
}

ENDCG