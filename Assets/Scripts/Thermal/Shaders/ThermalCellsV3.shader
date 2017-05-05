// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: renders V3 thermal cells with a 26-bits index and 6-bits temperature

CGINCLUDE
	#pragma target 5.0
	#pragma vertex vert
	#pragma geometry geom
	#pragma fragment frag

	typedef uint ThermalCell;
	StructuredBuffer<ThermalCell> thermalBuffer;

	uint Resolution;
	float HalfSize;
	float HalfHeight;
	float FilterMinTemperature;
	float FilterMaxTemperature;
	float FilterMinX;
	float FilterMaxX;
	float FilterMinY;
	float FilterMaxY;
	float FilterMinZ;
	float FilterMaxZ;

	static const int TEMPERATURE_BIT_COUNT = 6;
	static const int TEMPERATURE_SHIFT = 32 - TEMPERATURE_BIT_COUNT;
	static const int INDEX_MASK = (1 << TEMPERATURE_SHIFT) - 1;
	static const int TEMPERATURE_MASK = (1 << TEMPERATURE_BIT_COUNT) - 1;
	static const float INV_TEMPERATURE_RANGE = 1.0 / TEMPERATURE_MASK;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float temp : TEXCOORD0;
	};

	v2f vert(uint id : SV_VertexID)
	{
		uint data = thermalBuffer[id];
		uint index = data & INDEX_MASK;

		uint xIdx = index % Resolution;
		uint zIdx = (index / Resolution) % Resolution;
		uint yIdx = index / (Resolution * Resolution);
		float cellSize = HalfSize * 2.0;
		float cellHeight = HalfHeight * 2.0;

		v2f o;
		o.pos = float4((xIdx + 0.5) * cellSize, (yIdx + 0.5) * cellHeight, (zIdx + 0.5) * cellSize, 1);
		o.temp = ((data >> TEMPERATURE_SHIFT) & TEMPERATURE_MASK) * INV_TEMPERATURE_RANGE;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return float4(i.temp, 1, 0, 0);
	}
ENDCG

//-----------------------------------------------------------------------------

Shader "Thermal/ThermalCellsV3"
{
	Properties
	{
		Resolution("Resolution", int) = 1
		HalfSize("Half Size", float) = 0.25
		HalfHeight("Half Height", float) = 0.25
		MinTemperature("Min Temperature", float) = 0.0					// MinTemperature is ignored in this version
		InvTemperatureRange("Inv Temperature Range", float) = 1.0		// InvTemperatureRange is ignored in this version
		FilterMinTemperature("Filter Min Temperature", float) = 0.0
		FilterMaxTemperature("Filter Max Temperature", float) = 1.0
		FilterMinX("Filter Min X", float) =-10000000000.0
		FilterMaxX("Filter Max X", float) = 10000000000.0
		FilterMinY("Filter Min Y", float) =-10000000000.0
		FilterMaxY("Filter Max Y", float) = 10000000000.0
		FilterMinZ("Filter Min Z", float) =-10000000000.0
		FilterMaxZ("Filter Max Z", float) = 10000000000.0
	}
	SubShader
	{
		LOD 100

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off

		BlendOp Max
		Blend One One

		Pass
		{
			Cull Off
			Name "GroundQuad"
			CGPROGRAM
				#include "ThermalQuad.cginc"
			ENDCG
		}
		Pass
		{
			Name "Billboard"
			CGPROGRAM
				#include "ThermalBillboard.cginc"
			ENDCG
		}
		Pass
		{
			Name "Cube"
			CGPROGRAM
				#include "ThermalCube.cginc"
			ENDCG
		}
	}
}