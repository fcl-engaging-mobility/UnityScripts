// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: geometry shader for thermal billboards

#ifndef THERMALBB_INCLUDED
#define THERMALBB_INCLUDED

[maxvertexcount(4)]
void geom(point v2f IN[1], inout TriangleStream<v2f> outStream)
{
	float dx = UNITY_MATRIX_P._m00 * HalfSize;
	float dy = UNITY_MATRIX_P._m11 * HalfHeight;
	float4 pos = IN[0].pos;
	v2f OUT;
	OUT.temp = IN[0].temp;
	if (OUT.temp >= FilterMinTemperature && OUT.temp <= FilterMaxTemperature &&
		pos.x >= FilterMinX && pos.x <= FilterMaxX &&
		pos.y >= FilterMinY && pos.y <= FilterMaxY &&
		pos.z >= FilterMinZ && pos.z <= FilterMaxZ)
	{
		pos = mul(UNITY_MATRIX_MVP, pos);
		OUT.pos = pos + float4(-dx, dy, 0, 0); outStream.Append(OUT);
		OUT.pos = pos + float4( dx, dy, 0, 0); outStream.Append(OUT);
		OUT.pos = pos + float4(-dx,-dy, 0, 0); outStream.Append(OUT);
		OUT.pos = pos + float4( dx,-dy, 0, 0); outStream.Append(OUT);
	}
}

#endif
