// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: This file contains the data structures and methods to parse
//          Vissim's traffic simulation data files which are CSV files.

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public struct SimulationKeyframe
{
    public int vehicleID;
    public Vector3 position;
    public Quaternion rotation;
}

public class TrafficData
{
    public uint startTime;
    public SimulationKeyframe[] keyframes;
    public int[] frameOffsets;
    public byte[] assetTypes;       // Index is vehicleID; value is assetType
}

public class TrafficIO
{
    private static readonly uint INTERVAL = 250;
    private static readonly int CONTINUOS_TO_DISCRETE_TIME = 1000;

    public static TrafficData Load(string path, char columnSeparator, char vectorSeparator, bool useFirstFrameTime)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Could not find file: " + path);
            return null;
        }

        List<byte> assetTypes = new List<byte>(4096);   // Prepare for 4k IDs
        List<int> frameOffsets = new List<int>(16384);  // Prepare for 16k frames (1 hour)
        List<SimulationKeyframe> keyframes = new List<SimulationKeyframe>(2097152);   // 64 Mb

        bool sameSeparator = columnSeparator == vectorSeparator;
        Vector3 front;
        Vector3 back;
        string line;
        string[] data;
        uint time = 0;
        uint startTime = 0;

        // Add first frame offset (always 0)
        frameOffsets.Add(0);

        string msg = "Loading " + Path.GetFileName(path) + " : ";

        using (StreamReader reader = new StreamReader(File.OpenRead(path)))
        {
            // Skip header
            int next = reader.Peek();
            while (!reader.EndOfStream && !char.IsDigit((char)next))
            {
                reader.ReadLine();
                next = reader.Peek();
            }

            if (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                data = line.Split(columnSeparator);
                startTime = (uint)(float.Parse(data[0]) * CONTINUOS_TO_DISCRETE_TIME);

                if (useFirstFrameTime)
                {
                    time = startTime;
                }

                // Rewind
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();

                // Skip header (again)
                next = reader.Peek();
                while (!reader.EndOfStream && !char.IsDigit((char)next))
                {
                    reader.ReadLine();
                    next = reader.Peek();
                }
            }

            float dataRead = 0;
            float percentLoaded = 0;
            while (!reader.EndOfStream)
            {
                float newPercentLoaded = dataRead / reader.BaseStream.Length;
                if (newPercentLoaded > percentLoaded)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Traffic Data", msg + (int)(newPercentLoaded * 100) + " %", newPercentLoaded))
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.isPlaying = false;
                        return null;
                    }
                    percentLoaded = newPercentLoaded + 0.05f;
                }

                line = reader.ReadLine();
                data = line.Split(columnSeparator);
                dataRead += line.Length;

                uint t = (uint)(float.Parse(data[0]) * CONTINUOS_TO_DISCRETE_TIME);
                int vehicleID = int.Parse(data[1]);
                byte vehicleType = byte.Parse(data[2]);

                while (t > time)
                {
                    time += INTERVAL;
                    frameOffsets.Add(keyframes.Count);
                }

                if (vehicleID >= assetTypes.Count)
                {
                    for (int i = assetTypes.Count; i < vehicleID; i++)
                    {
                        assetTypes.Add(0);
                    }
                    assetTypes.Add(vehicleType);
                }
                else
                {
                    assetTypes[vehicleID] = vehicleType;
                }

                if (sameSeparator)
                {
                    front = new Vector3(float.Parse(data[3]), float.Parse(data[5]), float.Parse(data[4]));
                    back = new Vector3(float.Parse(data[6]), float.Parse(data[8]), float.Parse(data[7]));
                }
                else
                {
                    string[] pos1 = data[3].Split(vectorSeparator);
                    string[] pos2 = data[4].Split(vectorSeparator);
                    front = new Vector3(float.Parse(pos1[0]), float.Parse(pos1[2]), float.Parse(pos1[1]));
                    back = new Vector3(float.Parse(pos2[0]), float.Parse(pos2[2]), float.Parse(pos2[1]));
                }
                Vector3 center = (front + back) * 0.5f;
                Vector3 dir = front - back;

                keyframes.Add(new SimulationKeyframe
                {
                    vehicleID = vehicleID,
                    position = center,
                    rotation = Quaternion.LookRotation(dir, Vector3.up)
                });
            }
        }

        EditorUtility.ClearProgressBar();

        // Add last frame offset (always the number of keyframes)
        frameOffsets.Add(keyframes.Count);

        return new TrafficData
        {
            startTime = startTime,
            keyframes = keyframes.ToArray(),
            frameOffsets = frameOffsets.ToArray(),
            assetTypes = assetTypes.ToArray()
        };
    }
    
}