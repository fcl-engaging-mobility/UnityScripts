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
using System;

public class SimulationKeyframe
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
    public static readonly string VISSIM_VEHICLES_EXTENSION = "fzp";
    public static readonly string VISSIM_PEDESTRIANS_EXTENSION = "pp";
    public static readonly string BINARY_VEHICLES_EXTENSION = "bv";
    public static readonly string BINARY_PEDESTRIANS_EXTENSION = "bp";

    private static readonly uint INTERVAL = 250;
    private static readonly int CONTINUOS_TO_DISCRETE_TIME = 1000;

    public static TrafficData LoadBinary(string filename, bool useFirstFrameTime)
    {
        if (!File.Exists(filename))
        {
            Debug.LogError("Could not find file: " + filename);
            return null;
        }

        string msg = "Loading " + Path.GetFileName(filename);
        if (DisplayProgressBar(msg, 0))
            return null;

        TrafficData data = new TrafficData();

        using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
            data.startTime = reader.ReadUInt32();

            int count = reader.ReadInt32();
            data.keyframes = new SimulationKeyframe[count];
            for (int i = 0; i < count; i++)
            {
                data.keyframes[i] = new SimulationKeyframe
                {
                    vehicleID = reader.ReadInt32(),
                    position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                    rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
                };
            }

            if (DisplayProgressBar(msg, 0.33f))
                return null;

            count = reader.ReadInt32();
            data.frameOffsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                data.frameOffsets[i] = reader.ReadInt32();
            }

            if (DisplayProgressBar(msg, 0.66f))
                return null;

            count = reader.ReadInt32();
            data.assetTypes = reader.ReadBytes(count);
        }

        ClearProgressBar();

        return data;
    }

    public static void SaveBinary(TrafficData data, string filename)
    {
        string msg = "Saving " + Path.GetFileName(filename);
        if (DisplayProgressBar(msg, 0))
            return;

        using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
            writer.Write(data.startTime);

            int count = data.keyframes.Length;
            float invCount = 0.5f / count;
            int steps = count / 5;
            writer.Write(count);
            for (int i = 0; i < count;)
            {
                int next = Mathf.Min(count, i + steps);
                for (; i < next; i++)
                {
                    writer.Write(data.keyframes[i].vehicleID);

                    var pos = data.keyframes[i].position;
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                    writer.Write(pos.z);

                    var rot = data.keyframes[i].rotation;
                    writer.Write(rot.x);
                    writer.Write(rot.y);
                    writer.Write(rot.z);
                    writer.Write(rot.w);
                }
                if (DisplayProgressBar(msg, i * invCount))
                    return;
            }

            count = data.frameOffsets.Length;
            invCount = 0.5f / count;
            steps = count / 5;
            writer.Write(count);
            for (int i = 0; i < count; )
            {
                int next = Mathf.Min(count, i + steps);
                for (; i < next; i++)
                {
                    writer.Write(data.frameOffsets[i]);
                }
                if (DisplayProgressBar(msg, 0.5f + i * invCount))
                    return;
            }

            writer.Write(data.assetTypes.Length);
            writer.Write(data.assetTypes);
        }

        ClearProgressBar();
    }

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
        string line = "";
        string[] data;
        uint time = 0;
        uint startTime = 0;

        // Add first frame offset (always 0)
        frameOffsets.Add(0);

#if UNITY_EDITOR
        float percentLoaded = 0;
        string msg = "Loading " + Path.GetFileName(path) + " : ";
#endif

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

            try
            {
                float dataRead = 0;
                while (!reader.EndOfStream)
                {
#if UNITY_EDITOR
                    float newPercentLoaded = dataRead / reader.BaseStream.Length;
                    if (newPercentLoaded > percentLoaded)
                    {
                        if (DisplayProgressBar(msg, newPercentLoaded))
                            return null;
                        percentLoaded = newPercentLoaded + 0.05f;
                    }
#endif
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
            catch (Exception e)
            {
                Debug.LogError("Error reading line: " + line);
                Debug.LogError(e);
            }
        }

        ClearProgressBar();

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

    private static bool DisplayProgressBar(string msg, float progress)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Traffic Data", msg + "  " + (int)(progress * 100) + " %", progress))
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorApplication.isPlaying = false;
            return true;
        }
#endif
        return false;
    }

    private static void ClearProgressBar()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.ClearProgressBar();
#endif
    }

}