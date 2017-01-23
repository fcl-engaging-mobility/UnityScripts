// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: renders V2 thermal cells with an index (uint) and temperature (float)

CGINCLUDE
	#pragma target 5.0
	#pragma vertex vert
	#pragma geometry geom
	#pragma fragment frag

	struct ThermalCell
	{
		uint index;
		float temperature;
	};

	StructuredBuffer<ThermalCell> thermalBuffer;

	uint Resolution;
	float HalfSize;
	float HalfHeight;
	float MinTemperature;
	float InvTemperatureRange;
	float FilterMinTemperature;
	float FilterMaxTemperature;
	float FilterMinX;
	float FilterMaxX;
	float FilterMinY;
	float FilterMaxY;
	float FilterMinZ;
	float FilterMaxZ;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float temp : TEXCOORD0;
	};

	v2f vert(uint id : SV_VertexID)
	{
		ThermalCell data = thermalBuffer[id];

		uint xIdx = data.index % Resolution;
		uint zIdx = (data.index / Resolution) % Resolution;
		uint yIdx = data.index / (Resolution * Resolution);
		float cellSize = HalfSize * 2;
		float cellHeight = HalfHeight * 2;

		v2f o;
		o.pos = float4((xIdx + 0.5) * cellSize, (yIdx + 0.5) * cellHeight, (zIdx + 0.5) * cellSize, 1);
		o.temp = saturate((data.temperature - MinTemperature) * InvTemperatureRange);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return float4(i.temp, 1, 0, 0);
	}
ENDCG

//-----------------------------------------------------------------------------

Shader "Thermal/ThermalCellsV2"
{
	Properties
	{
		Resolution("Resolution", int) = 1
		HalfSize("Half Size", float) = 0.25
		HalfHeight("Half Height", float) = 0.25
		MinTemperature("Min Temperature", float) = 28.0
		InvTemperatureRange("Inv Temperature Range", float) = 1.0
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