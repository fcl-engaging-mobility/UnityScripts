// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: wrapper class for VISSIM API

using System;
using System.Runtime.InteropServices;

namespace Vissim
{
    public enum TurningIndicatorType
    {
        TurningIndicatorLeft = 1,
        TurningIndicatorNone = 0,
        TurningIndicatorRight = -1
    };

    public enum SignalStateType
    {
        SignalStateRed = 1,
        SignalStateRedAmber = 2,
        SignalStateGreen = 3,
        SignalStateAmber = 4,
        SignalStateOff = 5,
        SignalStateUndefined = 6,
        SignalStateFlashingAmber = 7,
        SignalStateFlashingRed = 8,
        SignalStateFlashingGreen = 9,
        SignalStateAlternatingRedGreen = 10,
        SignalStateGreenAmber = 11
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct DriverData
    {
        public double Position_X;     // in m
        public double Position_Y;     // in m
        public double Position_Z;     // in m
        public double Orient_Heading; // in radians
        public double Orient_Pitch;   // in radians
        public double Speed;          // in m/s
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VehicleData
    {
        public int VehicleID;
        public int VehicleType;                         // vehicle type number from VISSIM
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string ModelFileName;                    // *.v3d
        public int color;                               // RGB
        public double Position_X;                       // in m
        public double Position_Y;                       // in m
        public double Position_Z;                       // in m
        public double Orient_Heading;                   // in radians
        public double Orient_Pitch;                     // in radians
        public double Speed;                            // in m/s
        public int LeadingVehicleID;                    // relevant vehicle in front
        public int TrailingVehicleID;                   // next vehicle back on the same lane
        public int LinkID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string LinkName;                         // empty if not set in VISSIM
        public double LinkCoordinate;                   // in m
        public int LaneIndex;                           // 0 = rightmost
        public TurningIndicatorType TurningIndicator;   // 1 = left, 0 = none, -1 = right
    };


    public static class DrivingSimulatorProxy
    {
        // Called by the simulator to establish a connection between the DLL and VISSIM.
        // This starts VISSIM and passes the global data handle for the shared memory to VISSIM.
        // unsigned short versionNo is the Vissim version to be used, e.g. 800 for Vissim 8
        // string* networkFileName is the .inp VisSim traffic network filename to start VISSIM with.
        // string* snapFileName can be used to start VisSim from a predetermined point in the
        // simulation where traffic has already been generated (only Vissim 5.40, ignored in later versions).
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_Connect", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool Connect(ushort versionNo, string networkFileName, string snapshotFile);

        // Disconnects the simulator from VISSIM.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_Disconnect")]
        public extern static bool Disconnect();

        // Called ten times per second. DriverData[0] to DriverData[Num_Vehicles - 1] are provided.
        // VISSIM starts the calculation of the next time step immediately after receipt of this call.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_SetDriverVehicles")]
        public extern static bool SetDriverVehicles(int Num_Vehicles, DriverData[] DriverData);

        // Can be called before the call of VISSIM_SetDriverVehicles().
        // Causes the VISSIM detector <DetectorID> of the controller <ControllerID>
        // to behave as if a vehicle arrives on the detector during the next time step.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_SetDetection")]
        public extern static void SetDetection(long DetectorID, long ControllerID);

        // Can be called to determine if VISSIM has completed the calculation of the time step.
        // Does not block.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_DataReady")]
        public extern static bool DataReady();

        // *Num_Vehicles is set to the number of all VISSIM vehicles (excluding driver vehicle(s))
        // in the network; (*TrafficData)[0] to (*TrafficData)[*Num_Vehicles - 1] contain their data.
        // Doesn't block after VISSIM_DataReady() has returned true.
        // Blocks if the calculation of the time step in VISSIM has not finished yet.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_GetTrafficVehicles")]
        public extern static void GetTrafficVehicles(
            out int Num_Vehicles,
            out IntPtr TrafficData);

        // *NumNew is set to the number of new VISSIM vehicles created in the last time step,
        // (*NewId)[0] to (*NewId)[NumNew - 1] contain their IDs,
        // (*NewVehType)[0] to (*NewVehType)[NumNew - 1] contain their vehicle type numbers;
        // *NumMoved is set to the number of VISSIM vehicles that have moved in the last time step,
        // (*MovedId)[0] to (*MovedId)[NumMoved - 1] contain their IDs,
        // *NumDeleted is set to the number of VISSIM vehicles that have been deleted in the last time step,
        // (*DeletedId)[0] to (*DeletedId)[NumDeleted - 1] contain their IDs,
        // Doesn’t block after VISSIM_DataReady() has returned true. Might block before this.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "VISSIM_GetVehicleLists")]
        public extern static void GetVehicleLists(
            out int NumNew,
            out IntPtr NewIds,
            out IntPtr NewVehType,
            out int NumMoved,
            out IntPtr MovedIds,
            out int NumDeleted,
            out IntPtr DeletedIds);

        // Doesn’t block after VISSIM_DataReady() has returned true. Might block before this.
        //[DllImport("DrivingSimulatorProxy.dll")]
        //public extern static void VISSIM_GetSignalStates(ref int NumSignals, ref VISSIM_Sig_Data SignalStateData);

        // Returns the last error message.
        [DllImport("DrivingSimulatorProxy.dll", EntryPoint = "GetLastErrorMessage")]
        public extern static string GetLastErrorMessage();

    }
}
