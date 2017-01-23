// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: classes and structures used for thermal data storage

// #define USE_UNSAFE_CODE

using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct ThermalDataHeader
{
    public uint level;
    public int startTime;
    public int endTime;
    public uint count;
}

[StructLayout(LayoutKind.Sequential)]
public struct ThermalFrame
{
    public uint offset;
    public float minTemperature;
    public float temperatureRange;
}

public abstract class ThermalDataBase
{
    public int startTime = int.MaxValue;
    public int endTime = int.MinValue;
    public ThermalFrame[] frames;
    public abstract T[] Cells<T>();
    public abstract void CreateCells(uint count);
#if USE_UNSAFE_CODE
    public abstract unsafe void* GetCellsPointer();
#endif
}

public abstract class ThermalDataT<C> : ThermalDataBase
    where C : struct
{
    public C[] cells;
    public override T[] Cells<T>() { return cells as T[]; }
    public override void CreateCells(uint count) { cells = new C[count]; }
}

public abstract class ThermalCells
{
    public abstract T[] Cells<T>();
}

public class ThermalCellsT<C> : ThermalCells
    where C : struct
{
    public C[] cells;

    public ThermalCellsT(uint count) { cells = new C[count]; }
    public override T[] Cells<T>() { return cells as T[]; }
}

namespace PipelineV1
{
    public class ThermalData : ThermalDataT<ThermalCell>
    {
#if USE_UNSAFE_CODE
        public override unsafe void* GetCellsPointer()
        {
            fixed (void* pFirst = &cells[0]) return pFirst;
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThermalCell
    {
        public Vector3 position;
        public float temperature;
    }
}

namespace PipelineV2
{
    public class ThermalData : ThermalDataT<ThermalCell>
    {
#if USE_UNSAFE_CODE
        public override unsafe void* GetCellsPointer()
        {
            fixed (void* pFirst = &cells[0]) return pFirst;
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThermalCell
    {
        public uint index;
        public float temperature;
    }
}

namespace PipelineV3
{
    public class ThermalData : ThermalDataT<uint>
    {
#if USE_UNSAFE_CODE
        public override unsafe void* GetCellsPointer()
        {
            fixed (void* pFirst = &cells[0]) return pFirst;
        }
#endif
    }
}
