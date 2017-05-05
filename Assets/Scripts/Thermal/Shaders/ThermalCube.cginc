// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: geometry shader for thermal cubes

#ifndef THERMALCUBE_INCLUDED
#define THERMALCUBE_INCLUDED

[maxvertexcount(14)]
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
		float4 v0 = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize, HalfHeight,-HalfSize, 0));
		float4 v1 = mul(UNITY_MATRIX_MVP, pos + float4( HalfSize, HalfHeight,-HalfSize, 0));
		float4 v2 = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize,-HalfHeight,-HalfSize, 0));
		float4 v4 = mul(UNITY_MATRIX_MVP, pos + float4( HalfSize,-HalfHeight, HalfSize, 0));
		float4 v5 = mul(UNITY_MATRIX_MVP, pos + float4( HalfSize, HalfHeight, HalfSize, 0));
		float4 v6 = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize, HalfHeight, HalfSize, 0));
		OUT.pos = v0; outStream.Append(OUT);
		OUT.pos = v1; outStream.Append(OUT);
		OUT.pos = v2; outStream.Append(OUT);
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4(HalfSize, -HalfHeight, -HalfSize, 0)); outStream.Append(OUT);
		OUT.pos = v4; outStream.Append(OUT);
		OUT.pos = v1; outStream.Append(OUT);
		OUT.pos = v5; outStream.Append(OUT);
		OUT.pos = v0; outStream.Append(OUT);
		OUT.pos = v6; outStream.Append(OUT);
		OUT.pos = v2; outStream.Append(OUT);
		OUT.pos = mul(UNITY_MATRIX_MVP, pos + float4(-HalfSize, -HalfHeight, HalfSize, 0)); outStream.Append(OUT);
		OUT.pos = v4; outStream.Append(OUT);
		OUT.pos = v6; outStream.Append(OUT);
		OUT.pos = v5; outStream.Append(OUT);
	}
}

#endif
