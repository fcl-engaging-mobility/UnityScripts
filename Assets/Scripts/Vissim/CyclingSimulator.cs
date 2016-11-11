// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: communicates directly with VISSIM to get real-time
//          updates of traffic. It also sends the drivers position
//          and orientation for traffic interaction.

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Vissim;

public class CyclingSimulator : MonoBehaviour
{
    const int NAME_MAX_LENGTH = 100;

    [Header("Simulation")]
    public string networkFilename = "Testrun_Unity_v2.inpx";
    public bool useDeadReckoning;

    [Header("Driver")]
    public Transform driver;
    public bool calculateSpeed = false;

    [Header("Traffic Assets")]
    public TrafficAssets trafficAssets;

    private DriverData[] driverData = new DriverData[] {
        new DriverData()
    };

    private Dictionary<int, GameObject> vehicles = new Dictionary<int, GameObject>();
    private Dictionary<int, VehicleData> updatedVehicles = new Dictionary<int, VehicleData>();

    //private float nextUpdateTime;
    private Vector3 lastUpdatePosition;
    private float lastUpdateTime;
    private int totalVehicleCount = 0;
    private int Driver_Veh_Data_Size = 0;

    private bool loaded = false;
    private int notReadyCounter = 0;

    private const float profileFrequency = 1f;
    private int simulationUpdateCounter = 0;
    private float lastProfileTime;
    private string networkFileFullPath;

    private IGameProfiler profiler;
    void OnEnable()
    {
        profiler = GameProfiler.Get("GameProfiler");
        var settings = profiler.Add("notReadyCounter");
        settings.min = 0;
        settings.max = 100;
        settings.height = 401;
    }

    void LateUpdate()
    {
        profiler.Update("notReadyCounter", notReadyCounter);
    }

    void Start()
    {
        lastUpdatePosition = driver.position;
        lastUpdateTime = Time.time;
        lastProfileTime = Time.time;

        VehicleData vehicleData = new VehicleData();
        Driver_Veh_Data_Size = Marshal.SizeOf(vehicleData);

        networkFileFullPath = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + networkFilename;

        new Thread(Connect).Start();
    }

    public void Connect()
    {
        Debug.Log("Loading " + networkFileFullPath);
        if (!DrivingSimulatorProxy.Connect(800, networkFileFullPath, null))
        {
            Debug.LogError("Could not establish connection to VISSIM");
            return;
        }

        Debug.Log("Network loaded. Starting simulation");
        loaded = true;
    }

    void Update()
    {
        if (loaded)// && nextUpdateTime < Time.time)
        {
            //nextUpdateTime = Time.time + 0.2f;
            lastUpdateTime = Time.time;
            if (DrivingSimulatorProxy.DataReady())
            {
                notReadyCounter = 0;
                IntPtr ptr;
                DrivingSimulatorProxy.GetTrafficVehicles(out totalVehicleCount, out ptr);
                if (totalVehicleCount > 0)
                {
                    updatedVehicles.Clear();
                    long iptr = ptr.ToInt64();
                    long iCurOffset = 0;
                    for (int i = 0; i < totalVehicleCount; i++, iCurOffset += Driver_Veh_Data_Size)
                    {
                        VehicleData v = (VehicleData)Marshal.PtrToStructure(new IntPtr(iptr + iCurOffset), typeof(VehicleData));
                        updatedVehicles.Add(v.VehicleID, v);
                    }

                    int numNew = 0, numMoved = 0, numDeleted = 0;
                    IntPtr pNewIds, pNewVehTypes, pMovedIds, pDeletedIds;
                    DrivingSimulatorProxy.GetVehicleLists(out numNew, out pNewIds, out pNewVehTypes, out numMoved, out pMovedIds, out numDeleted, out pDeletedIds);

                    int[] newIds = new int[numNew];
                    int[] newVehTypes = new int[numNew];
                    int[] movedIds = new int[numMoved];
                    int[] deletedIds = new int[numDeleted];
                    Marshal.Copy(pNewIds, newIds, 0, numNew);
                    Marshal.Copy(pNewVehTypes, newVehTypes, 0, numNew);
                    Marshal.Copy(pMovedIds, movedIds, 0, numMoved);
                    Marshal.Copy(pDeletedIds, deletedIds, 0, numDeleted);

                    for (int i = 0; i < numNew; i++)
                    {
                        int vehicleId = newIds[i];
                        GameObject go = trafficAssets.Instantiate((byte)newVehTypes[i], transform);
                        vehicles.Add(vehicleId, go);

                        go.GetComponent<SimulationElement>().Initialize(vehicleId);
                    }
                    for (int i = 0; i < numMoved; i++)
                    {
                        int vehicleId = movedIds[i];
                        GameObject go = vehicles[vehicleId];
                        VehicleData v = updatedVehicles[vehicleId];
                        go.transform.localPosition = new Vector3((float)v.Position_X, (float)v.Position_Z, (float)v.Position_Y);
                        go.transform.localRotation = Quaternion.Euler(0f, 90f - (float)v.Orient_Heading * Mathf.Rad2Deg, 0f);
                    }
                    for (int i = 0; i < numDeleted; i++)
                    {
                        int vehicleId = deletedIds[i];
                        GameObject go;
                        if (vehicles.TryGetValue(vehicleId, out go))
                        {
                            go.SetActive(false);
                            go.transform.parent = null;
                            Destroy(go);
                            vehicles.Remove(vehicleId);
                        }
                    }
                }

                Vector3 driverPosition = driver.localPosition + driver.forward * 0.5f;  //+ Replace this magic number with vehicle's half length
                driverData[0].Position_X = driverPosition.x;
                driverData[0].Position_Y = driverPosition.z;
                driverData[0].Orient_Heading = (90f - driver.rotation.eulerAngles.y) * Mathf.Deg2Rad;

                if (calculateSpeed)
                {
                    driverData[0].Speed = (driver.position - lastUpdatePosition).magnitude / (Time.time - lastUpdateTime);
                    lastUpdatePosition = driver.position;
                    lastUpdateTime = Time.time;
                }

                DrivingSimulatorProxy.SetDriverVehicles(1, driverData);

                simulationUpdateCounter++;
            }
            else
            {
                if (useDeadReckoning)
                {
                    foreach (var vehicle in updatedVehicles.Values)
                    {
                        int vehicleId = vehicle.VehicleID;
                        float speed = (float)vehicle.Speed;
                        GameObject go = vehicles[vehicleId];
                        go.transform.localPosition += go.transform.forward * speed * Time.deltaTime;
                    }
                }
                notReadyCounter++;
                if (notReadyCounter > 1000)
                {
                    Debug.LogWarning("VISSIM wasn't ready for too long!");
                    Destroy(this);
                }
            }
        }
    }

    static readonly Rect labelRect = new Rect(10, 10, 300, 20);
    string labelText = "";

    void OnGUI()
    {
        if (Time.time - lastProfileTime > profileFrequency)
        {
            labelText = "VISSIM: " + totalVehicleCount + " vehicles, " + simulationUpdateCounter + " steps/s";
            simulationUpdateCounter = 0;
            lastProfileTime = Time.time;
        }
        GUI.Label(labelRect, labelText);
    }

    void OnDestroy()
    {
        if (loaded)
        {
            loaded = false;
            Debug.Log("Disconnecting VISSIM");
            DrivingSimulatorProxy.Disconnect();
        }
    }
}