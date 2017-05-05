// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: geometry shader for thermal ground-aligned quads

#ifndef THERMALQUAD_INCLUDED
#define THERMALQUAD_INCLUDED

[maxvertexcount(4)]
void geom(point v2f IN[1], inout TriangleStream<v2f> outStream)
{
	float4 pos = IN[0].pos;
	v2f OUT;
	OUT.temp = IN[0].temp;
	if (OUT.temp >= FilterMinTemperature && OUT.temp <= FilterMaxTemperature &&
		pos.x >= FilterMinX && pos.x <= FilterMaxX &&
		pos.y >= FilterMinY && pos.y <= FilterMaxY &&
		pos.z >= FilterMinZ && pos.z <= FilterMaxZ)
	{
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize,-HalfHeight, HalfSize, 0));
		outStream.Append(OUT);
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4( HalfSize,-HalfHeight, HalfSize, 0));
		outStream.Append(OUT);
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize,-HalfHeight,-HalfSize, 0));
		outStream.Append(OUT);
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4( HalfSize,-HalfHeight,-HalfSize, 0));
		outStream.Append(OUT);
	}
}

#endif
