// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to create a heatmap image given some simulation frames.

using UnityEngine;
using System.IO;

public static class Heatmap
{
    public static string path = "/Textures/Heatmaps/";

    public static void GenerateHeatmap(SimulationKeyframe[] keyframes, int resolution, Bounds aabb, string filename)
    {
        float max = 0;
        int count = resolution * resolution;
        float[] values = new float[count];
        Vector2 offset = new Vector2(aabb.min.x, aabb.min.z);
        Vector2 scale = new Vector2(resolution / aabb.size.x, resolution / aabb.size.z);
        int index = 0;
        float value = 0f;
        foreach (var keyframe in keyframes)
        {
            if (aabb.Contains(keyframe.position))
            {
                float xPos = (keyframe.position.x - offset.x) * scale.x;
                float yPos = (keyframe.position.z - offset.y) * scale.y;
                int x = Mathf.FloorToInt(xPos);
                int y = Mathf.FloorToInt(yPos);

                index = x + y * resolution;
                value = 1f - Mathf.Sqrt(Mathf.Pow(xPos - x, 2) + Mathf.Pow(yPos - y, 2));
                values[index] += Mathf.Clamp01(value);
                max = Mathf.Max(max, values[index]);

                index++;
                value = 1f - Mathf.Sqrt(Mathf.Pow(xPos - x - 1, 2) + Mathf.Pow(yPos - y, 2));
                values[index] += Mathf.Clamp01(value);
                max = Mathf.Max(max, values[index]);

                index += resolution;
                value = 1f - Mathf.Sqrt(Mathf.Pow(xPos - x - 1, 2) + Mathf.Pow(yPos - y - 1, 2));
                values[index] += Mathf.Clamp01(value);
                max = Mathf.Max(max, values[index]);

                index--;
                value = 1f - Mathf.Sqrt(Mathf.Pow(xPos - x, 2) + Mathf.Pow(yPos - y - 1, 2));
                values[index] += Mathf.Clamp01(value);
                max = Mathf.Max(max, values[index]);
            }
        }

        index = 0;
        max = 1f / max;
        float minLog = Mathf.Log10(max);
        float invMinLog = 1f / minLog;

        var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        Color c = new Color(1, 1, 1);
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, index++)
            {
                value = Mathf.Clamp01((minLog - Mathf.Log10(values[index] * max)) * invMinLog);
                c.r = c.g = c.b = value;
                texture.SetPixel(x, y, c);
            }
        }

        Directory.CreateDirectory(Application.dataPath + path);
        File.WriteAllBytes(Application.dataPath + path + filename, texture.EncodeToPNG());
    }
}
