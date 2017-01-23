// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class that uses the V2 pipeline data structures to
//          perform IO operations with vision data

using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class VisionDataIO
{
    public struct Header
    {
        public uint resolution;
        public float sizeX, sizeY, sizeZ;
        public float minValue, maxValue;
    }

    public static bool LoadFromBinary<C>(string path, ThermalDataT<C> thermalData, ref Header header)
        where C : struct
    {
        if (typeof(C) == typeof(PipelineV2.ThermalCell))
        {
            return LoadFromBinaryV2(path, thermalData as ThermalDataT<PipelineV2.ThermalCell>, ref header);
        }

        Debug.LogError("LoadFromBinary not implemented for " + typeof(C));
        return false;
    }

    private static bool LoadFromBinaryV2(string path, ThermalDataT<PipelineV2.ThermalCell> thermalData, ref Header header)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            header.resolution = reader.ReadUInt32();
            header.minValue = reader.ReadSingle();
            header.maxValue = reader.ReadSingle();
            header.sizeX = reader.ReadSingle();
            header.sizeY = reader.ReadSingle();
            header.sizeZ = reader.ReadSingle();
            uint count = reader.ReadUInt32();

            thermalData.frames = new ThermalFrame[2];
            thermalData.frames[1].offset = count;
            thermalData.frames[1].minTemperature = header.minValue;
            thermalData.frames[1].temperatureRange = header.maxValue - header.minValue;

            thermalData.CreateCells(count);
            for (uint i = 0; i < count; i++)
            {
                thermalData.cells[i].index = reader.ReadUInt32();
                thermalData.cells[i].temperature = reader.ReadSingle();
            }

            Debug.Log("Loaded " + count + " cells");
        }

        return true;
    }

    public static void SaveToBinary(string file, Header header, Dictionary<uint, float> values)
    {
        uint count = (uint)values.Count;
        using (BinaryWriter writer = new BinaryWriter(File.Open(file, FileMode.Create)))
        {
            writer.Write(header.resolution);
            writer.Write(header.minValue);
            writer.Write(header.maxValue);
            writer.Write(header.sizeX);
            writer.Write(header.sizeY);
            writer.Write(header.sizeZ);
            writer.Write(count);
            foreach (var key in values.Keys)
            {
                writer.Write(key);
                writer.Write(values[key]);
            }
            Debug.Log("Saved " + count + " cells");
        }
    }
}
