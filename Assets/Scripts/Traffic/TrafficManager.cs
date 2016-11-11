// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a TrafficManager is in charge of loading traffic data as well as
//          animating traffic elements smoothly

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeatmapPass
{
    public bool include;
    public AssetType[] assetTypes;
}


public class TrafficManager : MonoBehaviour
{
    private static readonly uint INTERVAL = 250;
    private static readonly int CONTINUOS_TO_DISCRETE_TIME = 1000;
    private static readonly float TIME_TO_FRAME = CONTINUOS_TO_DISCRETE_TIME / INTERVAL;

    [Header("Traffic Data File")]
    public string dataFile = "Vehicles.fzp";
    public char columnSeparator = ';';
    public char vectorSeparator = ' ';
    public bool firstFrameIsStartTime = true;

    [Header("Traffic Assets")]
    public TrafficAssets trafficAssets;

    [Header("Traffic Animation")]
    public TimeController timeController = null;

    [Header("Heat Map")]
    public bool generateOnStart = false;
    public int heatmapResolution = 4096;
    public float pointRadius = 2f;
    public bool logarithmic = false;
    public Transform area;
    public HeatmapPass[] heatmapPasses;

    public int CurrentObjectCount
    {
        get { return transform.childCount; }
    }

    public int TotalObjectCount
    {
        get { return data == null? 0 : data.keyframes[data.keyframes.Length-1].vehicleID; }
    }

    public float AnimationLength
    {
        get { return animationLength; }
    }

    private TrafficData data = null;
    private Dictionary<int, SimulationElement> activeSimElements = new Dictionary<int, SimulationElement>();
    private Dictionary<int, SimulationElement> previousSimElements = new Dictionary<int, SimulationElement>();
    private float animationLength = 0;

    private int currentLowFrame = -1;
    private int currentHighFrame = -1;

    void Start()
    {
        LoadData();

        if (generateOnStart)
        {
            GenerateHeatMap();
        }
    }

    void Update()
    {
        Update(timeController.time % animationLength);
    }

    void LoadData()
    {
        try
        {
            if (dataFile.EndsWith(TrafficIO.BINARY_VEHICLES_EXTENSION) || dataFile.EndsWith(TrafficIO.BINARY_PEDESTRIANS_EXTENSION))
            {
                data = TrafficIO.LoadBinary(dataFile, firstFrameIsStartTime);
            }
            else if (dataFile.EndsWith(TrafficIO.VISSIM_VEHICLES_EXTENSION) || dataFile.EndsWith(TrafficIO.VISSIM_PEDESTRIANS_EXTENSION))
            {
                data = TrafficIO.Load(dataFile, columnSeparator, vectorSeparator, firstFrameIsStartTime);
            }
            else
            {
                Debug.LogError("Invalid traffic data extension: " + System.IO.Path.GetFileName(dataFile));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e, this);
        }

        if (data == null)
        {
            Debug.LogWarning("Could not load traffic data for " + name);
            enabled = false;
            return;
        }

        animationLength = (data.frameOffsets.Length - 2) / TIME_TO_FRAME;

        // Check if loaded data has asset types that were not setup in the traffic asset replacement lists.
        trafficAssets.Check(data.assetTypes, true);
    }
    
    void Update(float time)
    {
        float frame = time * TIME_TO_FRAME;
        int lowframe = Mathf.FloorToInt(frame);
        int highframe = Mathf.CeilToInt(frame);

        // Interpolate between low and high frame
        float lerp = frame - lowframe; //- Mathf.InverseLerp(lowframe, highframe, frame);

        int offset = data.frameOffsets[lowframe];
        int nextOffset = data.frameOffsets[lowframe + 1];

        SimulationElement simElement;

        if (currentLowFrame == lowframe && currentHighFrame == highframe)
        {
            for (; offset < nextOffset; offset++)
            {
                simElement = activeSimElements[data.keyframes[offset].vehicleID];
                SimulationKeyframe from = data.keyframes[simElement.fromKeyframe];
                SimulationKeyframe to = data.keyframes[simElement.toKeyframe];
                simElement.targetPosition = Vector3.Lerp(from.position, to.position, lerp);
                simElement.targetRotation = Quaternion.Slerp(from.rotation, to.rotation, lerp);
                simElement.MoveToTarget();
            }
        }
        else
        {
            currentLowFrame = lowframe;
            currentHighFrame = highframe;

            var temp = previousSimElements;
            previousSimElements = activeSimElements;
            activeSimElements = temp;

            // Update existing elements and create new ones
            for (; offset < nextOffset; offset++)
            {
                SimulationKeyframe keyframe = data.keyframes[offset];
                if (previousSimElements.TryGetValue(keyframe.vehicleID, out simElement))
                {
                    previousSimElements.Remove(keyframe.vehicleID);
                }
                else
                {
                    byte assetType = data.assetTypes[keyframe.vehicleID];
                    GameObject go = trafficAssets.Instantiate(assetType, transform);
                    go.transform.localPosition = keyframe.position;
                    go.transform.localRotation = keyframe.rotation;

                    simElement = go.GetComponent<SimulationElement>();
                    simElement.Initialize(keyframe.vehicleID);
                }

                simElement.fromKeyframe = offset;
                simElement.targetPosition = keyframe.position;
                simElement.targetRotation = keyframe.rotation;
                activeSimElements.Add(keyframe.vehicleID, simElement);
            }

            // Remove elements that have ceased to exist
            foreach (var previousChild in previousSimElements.Values)
            {
                previousChild.gameObject.SetActive(false);
                previousChild.transform.parent = null;
                Destroy(previousChild.gameObject);
            }
            previousSimElements.Clear();

            offset = nextOffset;
            nextOffset = data.frameOffsets[highframe + 1];

            // Find the next frame position/rotation and interpolate
            for (; offset < nextOffset; offset++)
            {
                SimulationKeyframe keyframe = data.keyframes[offset];
                if (activeSimElements.TryGetValue(keyframe.vehicleID, out simElement))
                {
                    simElement.toKeyframe = offset;
                    simElement.SetSpeed(Vector3.Distance(simElement.targetPosition, keyframe.position) * TIME_TO_FRAME);
                    simElement.targetPosition = Vector3.Lerp(simElement.targetPosition, keyframe.position, lerp);
                    simElement.targetRotation = Quaternion.Slerp(simElement.targetRotation, keyframe.rotation, lerp);
                    simElement.MoveToTarget();

                    //var audioSource = simElement.GetComponent<AudioSource>();
                    //float sqrDst = (simElement.targetPosition - Camera.main.transform.position).sqrMagnitude;
                    //if (audioSource)
                    //{
                    //    audioSource.priority = (int)Mathf.Lerp(50, 250, Mathf.Clamp01(sqrDst * 0.00005f));
                    //}
                }
            }

            // Random honk/bell
            //if (random.Next() > 1000)
            //{
            //    offset = data.frameOffsets[lowframe];
            //    nextOffset = data.frameOffsets[lowframe + 1];
            //      vvv OUT OF RANGE EXCEPTION vvv
            //    simElement = activeSimElements[data.keyframes[random.Next(offset, nextOffset)].vehicleID];
            //    simElement.SoundSignal();
            //}
        }
    }

    public bool GetPosition(int id, float time, ref Vector3 pos)
    {
        int frame = Mathf.FloorToInt(time * TIME_TO_FRAME);
        int offset = data.frameOffsets[frame];
        int nextOffset = data.frameOffsets[frame + 1];

        for (; offset < nextOffset; offset++)
        {
            if (data.keyframes[offset].vehicleID == id)
            {
                pos = data.keyframes[offset].position;
                return true;
            }
        }
        return false;
    }

    void GenerateHeatMap()
    {
        Bounds aabb = new Bounds(area.position, area.localScale);
        if (heatmapPasses == null || heatmapPasses.Length == 0)
        {
            Heatmap.GenerateHeatmap(data, transform.position, heatmapResolution, pointRadius, aabb, logarithmic, name + "-Heatmap");
        }
        else
        {
            for (int i = 0; i < heatmapPasses.Length; i++)
            {
                Heatmap.GenerateHeatmap(data, transform.position, heatmapResolution, pointRadius, aabb, logarithmic, name + "-Heatmap" + (i + 1), heatmapPasses[i].assetTypes, heatmapPasses[i].include);
            }
        }
    }

}

public static class RendererExtensions
{
    public static bool IsVisibleFrom(this Renderer renderer, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}