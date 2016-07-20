// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to create a heatmap image given some simulation data.

using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class Heatmap
{
    public static string path = "/Textures/Heatmaps/";

    public static void GenerateHeatmap(TrafficData data, int resolution, float radius, Bounds aabb, bool log10, string filename, AssetType[] types = null, bool include = true)
    {
        SimulationKeyframe[] keyframes = data.keyframes;
        Dictionary<byte, byte> filterTypes = null;
        if (types != null && types.Length > 0)
        {
            filterTypes = new Dictionary<byte, byte>();
            foreach (byte t in types)
                filterTypes.Add(t, t);
        }

        float max = 0;
        int count = resolution * resolution;
        float[] values = new float[count];
        Vector2 offset = new Vector2(aabb.min.x, aabb.min.z);
        Vector2 scale = new Vector2(resolution / aabb.size.x, resolution / aabb.size.z);
        if (radius > 1f)
        {
            float k = 1f / radius;
            if (filterTypes == null)
            {
                foreach (var keyframe in keyframes)
                {
                    float xPos = (keyframe.position.x - offset.x) * scale.x;
                    float yPos = (keyframe.position.z - offset.y) * scale.y;
                    DrawPoint(xPos, yPos, resolution, ref values, ref max, radius, k);
                }
            }
            else
            {
                foreach (var keyframe in keyframes)
                {
                    if (filterTypes.ContainsKey(data.assetTypes[keyframe.vehicleID]) ^ !include)
                    {
                        float xPos = (keyframe.position.x - offset.x) * scale.x;
                        float yPos = (keyframe.position.z - offset.y) * scale.y;
                        DrawPoint(xPos, yPos, resolution, ref values, ref max, radius, k);
                    }
                }
            }
        }
        else
        {
            if (filterTypes == null)
            {
                foreach (var keyframe in keyframes)
                {
                    float xPos = (keyframe.position.x - offset.x) * scale.x;
                    float yPos = (keyframe.position.z - offset.y) * scale.y;
                    DrawPoint(xPos, yPos, resolution, ref values, ref max);
                }
            }
            else
            {
                foreach (var keyframe in keyframes)
                {
                    if (filterTypes.ContainsKey(data.assetTypes[keyframe.vehicleID]) ^ !include)
                    {
                        float xPos = (keyframe.position.x - offset.x) * scale.x;
                        float yPos = (keyframe.position.z - offset.y) * scale.y;
                        DrawPoint(xPos, yPos, resolution, ref values, ref max);
                    }
                }
            }
        }

        Directory.CreateDirectory(Application.dataPath + path);

        var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        Color c = new Color(1, 1, 1);
        int index = 0;

        if (log10)
        {
            max = 1f / max;
            float value = 0;
            float minLog = Mathf.Log10(max);
            float invMinLog = 1f / minLog;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++, index++)
                {
                    value = Mathf.Clamp01((minLog - Mathf.Log10(values[index] * max)) * invMinLog);
                    c.r = c.g = c.b = value;
                    texture.SetPixel(x, y, c);
                }
            }
        }
        else
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++, index++)
                {
                    c.r = c.g = c.b = Mathf.Clamp01(values[index]);
                    texture.SetPixel(x, y, c);
                }
            }
        }
        File.WriteAllBytes(Application.dataPath + path + filename + ".png", texture.EncodeToPNG());
    }

    private static void DrawPoint(float xPos, float yPos, int resolution, ref float[] values, ref float max)
    {
        int index = Mathf.FloorToInt(xPos) + Mathf.FloorToInt(yPos) * resolution;
        values[index] += 1f;
        max = Mathf.Max(max, values[index]);
    }

    private static void DrawPoint(float xPos, float yPos, int resolution, ref float[] values, ref float max, float radius, float k)
    {
        int minX = Mathf.Max(0, Mathf.RoundToInt(xPos - radius));
        int maxX = Mathf.Min(resolution, Mathf.RoundToInt(xPos + radius));
        int minY = Mathf.Max(0, Mathf.RoundToInt(yPos - radius));
        int maxY = Mathf.Min(resolution, Mathf.RoundToInt(yPos + radius));

        for (int y = minY; y < maxY; y++)
        {
            int index = minX + y * resolution;
            for (int x = minX; x < maxX; x++, index++)
            {
                float value = 1f - k * Mathf.Sqrt(Mathf.Pow(xPos - x, 2) + Mathf.Pow(yPos - y, 2));
                values[index] += Mathf.Clamp01(value);
                max = Mathf.Max(max, values[index]);
            }
        }
    }
}
