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

public enum AssetType : byte
{
    None = 0,
    MalePedestrian = 1,
    FemalePedestrian = 2,
    Cyclist = 10,
    Car1 = 20,
    Car2 = 21,
    Motorbike = 22,
    HGV = 30,
    Minivan = 31,
    Bus = 32,
    Bus_SBS = 33,
}

[Serializable]
public class AssetReplacement
{
    public AssetType type;
    public GameObject[] prefabs;
}

[Serializable]
public class CustomAssetReplacement
{
    public byte type;
    public GameObject[] prefabs;
}

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
    public string dataFile = "C:/Data/Mobility/20160401/Vehicles.fzp";
    public char columnSeparator = ';';
    public char vectorSeparator = ' ';
    public bool firstFrameIsStartTime = true;

    [Header("Asset Replacement")]
    public GameObject defaultPrefab;
    public AssetReplacement[] assets;
    public CustomAssetReplacement[] customAssets;

    [Header("Traffic Animation")]
    public TimeController timeController = null;

    [Header("Heat Map")]
    public bool generateOnStart = false;
    public int heatmapResolution = 4096;
    public float pointRadius = 2f;
    public bool logarithmic = false;
    public OcclusionArea bounds;
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

    private static readonly System.Random random = new System.Random(13);

    private TrafficData data = null;
    private Dictionary<byte, GameObject[]> assetTypeToPrefab = new Dictionary<byte, GameObject[]>();
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

    void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            UpdateAssetReplacementMap();
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
            data = TrafficIO.Load(dataFile, columnSeparator, vectorSeparator, firstFrameIsStartTime);
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
        List<CustomAssetReplacement> newCustomAssets = new List<CustomAssetReplacement>(customAssets);
        foreach (var assetType in data.assetTypes)
        {
            if (assetType == (byte)AssetType.None)
                continue;

            bool found = false;
            foreach (var asset in assets)
            {
                if (assetType == (int)asset.type)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                foreach (var v in newCustomAssets)
                {
                    if (assetType == v.type)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogWarning("Asset type " + assetType + " was not found in Asset Replacement. Adding it to Custom Assets");
                    newCustomAssets.Add(new CustomAssetReplacement
                    {
                        type = assetType,
                        prefabs = new GameObject[] { defaultPrefab }
                    });
                }
            }
        }
        customAssets = newCustomAssets.ToArray();
        UpdateAssetReplacementMap();
    }

    void UpdateAssetReplacementMap()
    {
        assetTypeToPrefab.Clear();
        foreach (var asset in assets)
        {
            assetTypeToPrefab.Add((byte)asset.type, asset.prefabs);
        }
        foreach (var asset in customAssets)
        {
            assetTypeToPrefab.Add(asset.type, asset.prefabs);
        }

        // Safety check: prefabs need to have a SimulationElement component
        foreach (var entry in assetTypeToPrefab)
        {
            foreach (var prefab in entry.Value)
            {
                if (prefab == null)
                {
                    Debug.LogError(name + ": asset type " + entry.Key + " has an empty prefab!");
                }
                else if (prefab.GetComponent<SimulationElement>() == null)
                {
                    Debug.LogWarning(name + ": prefab " + prefab.name + " doesn't have SimulationElement component!");
                }
            }
        }
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
                    GameObject[] gos = assetTypeToPrefab[assetType];

                    int index = random.Next(0, gos.Length);
                    GameObject go = Instantiate(gos[index]) as GameObject;
                    go.transform.SetParent(transform);
                    go.transform.localPosition = keyframe.position;
                    go.transform.localRotation = keyframe.rotation;

                    simElement = go.GetComponent<SimulationElement>();
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

                    var audioSource = simElement.GetComponent<AudioSource>();
                    float sqrDst = (simElement.targetPosition - Camera.main.transform.position).sqrMagnitude;
                    audioSource.priority = (int)Mathf.Lerp(50, 250, Mathf.Clamp01(sqrDst * 0.00005f));
                }
            }

            // Random honk/bell
            if (random.Next() > 1000)
            {
                offset = data.frameOffsets[lowframe];
                nextOffset = data.frameOffsets[lowframe + 1];
                simElement = activeSimElements[data.keyframes[random.Next(offset, nextOffset)].vehicleID];
                simElement.SoundSignal();
            }
        }
    }

    void GenerateHeatMap()
    {
        Bounds aabb = new Bounds(bounds.center, bounds.size);
        if (heatmapPasses == null || heatmapPasses.Length == 0)
        {
            Heatmap.GenerateHeatmap(data, heatmapResolution, pointRadius, aabb, logarithmic, name + "-Heatmap");
        }
        else
        {
            for (int i = 0; i < heatmapPasses.Length; i++)
            {
                Heatmap.GenerateHeatmap(data, heatmapResolution, pointRadius, aabb, logarithmic, name + "-Heatmap" + (i + 1), heatmapPasses[i].assetTypes, heatmapPasses[i].include);
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