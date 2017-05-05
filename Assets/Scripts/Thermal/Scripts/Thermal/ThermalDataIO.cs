// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: Helper class to perform thermal IO operations

// #define USE_UNSAFE_CODE
// #define ENABLE_SAFETY_CHECKS

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

// Need to use alias due to System.Diagnostics.Debug taking preference
using UnityDebug = UnityEngine.Debug;


public class ThermalDataIO
{
#if USE_UNSAFE_CODE
    const uint GENERIC_READ = 0x80000000;
    const uint OPEN_EXISTING = 3;

    [DllImport("kernel32", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
    static extern unsafe System.IntPtr CreateFile(
        string FileName,          // file name
        uint DesiredAccess,       // access mode
        uint ShareMode,           // share mode
        uint SecurityAttributes,  // Security Attributes
        uint CreationDisposition, // how to create
        uint FlagsAndAttributes,  // file attributes
        int hTemplateFile         // handle to template file
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern unsafe bool ReadFile(IntPtr handle, void* buffer, int numBytesToRead, int* numBytesRead, int overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern unsafe bool CloseHandle(IntPtr handle);

    public static unsafe bool LoadFromBinary<C>(string path, uint level, int startTime, int endTime, uint interval, ThermalDataT<C> thermalData)
        where C : struct
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        string filename = path + "LOD" + level + "_" + startTime + "_" + endTime + ".heat";
        if (!File.Exists(filename))
        {
            UnityDebug.LogError("Couldn't find file " + filename);
            return false;
        }

        IntPtr fileHandle = IntPtr.Zero;
        if (!Open(filename, ref fileHandle))
        {
            UnityDebug.LogError("Couldn't open file " + filename);
            return false;
        }

        int size;
        int bytesRead = 0;

        // Read header
        ThermalDataHeader header = new ThermalDataHeader();
        ThermalDataHeader* pHeader = &header;
        ReadFile(fileHandle, pHeader, sizeof(ThermalDataHeader), &bytesRead, 0);

#if ENABLE_SAFETY_CHECKS
        if (level != header.level)
        {
            UnityDebug.LogError("File " + filename + "contains invalid header LOD: " + header.level);
            CloseHandle(fileHandle);
            return false;
        }
        if (startTime != header.startTime)
        {
            UnityDebug.LogError("File " + filename + "contains invalid header start time: " + header.startTime);
            CloseHandle(fileHandle);
            return false;
        }
        if (endTime != header.endTime)
        {
            UnityDebug.LogError("File " + filename + "contains invalid header end time: " + header.endTime);
            CloseHandle(fileHandle);
            return false;
        }
#endif
        thermalData.startTime = header.startTime;
        thermalData.endTime = header.endTime;

        // UnityDebug.Log("Loading level " + header.level + " from " + header.startTime + " to " + header.endTime + " with " + header.count + " elements.");

        // Note: add 1 element at the beginning of the keyframes buffer with value 0
        // for easier computation and reduce if statements
        uint framesCount = (uint)(endTime - startTime) / interval + 1;
        if (thermalData.frames == null || thermalData.frames.Length != framesCount)
        {
            thermalData.frames = new ThermalFrame[framesCount];
        }

        // Read keyframes
        bytesRead = 0;
        size = (int)((framesCount - 1) * sizeof(ThermalFrame));
        fixed (ThermalFrame* pFirst = &thermalData.frames[1])
        {
            if (!ReadFile(fileHandle, pFirst, size, &bytesRead, 0))
            {
                UnityDebug.LogError("Couldn't read keyframes!");
                CloseHandle(fileHandle);
                return false;
            }
        }
#if ENABLE_SAFETY_CHECKS
        if (bytesRead != size)
        {
            UnityDebug.LogError("Keyframes: only read " + bytesRead + " bytes out of " + size);
        }
        if (thermalData.frames[framesCount - 1].offset != header.count)
        {
            UnityDebug.LogError("Keyframes: last offset is " + thermalData.frames[framesCount - 1].offset + ", expected " + header.count);
        }
#endif

        if (header.count > 0)
        {
            // Create thermal cells buffer
            if (thermalData.cells == null || thermalData.cells.Length < header.count)
            {
                thermalData.CreateCells(header.count);
            }

            // Read thermal cells
            size = (int)header.count * GetCellSize(thermalData.cells);
            if (!ReadFile(fileHandle, thermalData.GetCellsPointer(), size, &bytesRead, 0))
            {
                UnityDebug.LogError("Couldn't read thermal cells!");
                CloseHandle(fileHandle);
                return false;
            }
#if ENABLE_SAFETY_CHECKS
            if (bytesRead != size)
            {
                UnityDebug.LogError("Heat cells: only read " + bytesRead + " bytes out of " + size);
            }
#endif
        }

        CloseHandle(fileHandle);

        sw.Stop();
        if (sw.Elapsed.TotalMilliseconds > 120)
        {
            UnityDebug.LogWarning("Parsing " + filename + " took " + sw.Elapsed.TotalMilliseconds + "ms.");
        }
        return true;
    }

    private static int GetCellSize<C>(C[] array)
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(C));
    }

    private static bool Open(string FileName, ref IntPtr handle)
    {
        // Open the existing file for reading       
        handle = CreateFile(FileName, GENERIC_READ, 0, 0, OPEN_EXISTING, 0, 0);
        return (handle != System.IntPtr.Zero);
    }

#else

    public static bool LoadFromBinary<C>(string path, uint level, int startTime, int endTime, uint interval, ThermalDataT<C> thermalData)
        where C : struct
    {
        UnityDebug.LogError("Not implemented!");
        return false;
    }

#endif
}
