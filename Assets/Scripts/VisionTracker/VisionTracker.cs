// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: generates volumetric heatmaps according to the user's
//          motion data (position, rotation, etc.)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionTracker
{
    public class Parameters
    {
        public List<string> files;
        public Vector3 volumePosition;
        public Quaternion volumeRotation;
        public Vector3 volumeSize;
        public uint resolution;
        public uint customFPS;
        public uint spotSize;
        public GameObject[] geometryObjects;
    }

    public class Progress
    {
        public float value;
        public string info;
    }

    public delegate void OnComplete();
    private delegate void AddPosition(Vector3 position, Dictionary<uint, float> values);

    // Constants
    public const string Extension = "viz";
    private const float valueIncrement = 0.1f;

    // Params
    private Parameters parameters;
    private Progress progress;

    // Pre-computed constants
    private uint resolution2;
    private uint spotLeftHalf;
    private uint spotRightHalf;
    private float spotMaxRadius;
    private float invCellSize;
    private float invMaxDistance;
    private Vector3 volumeMin;
    private float maxRaycastDistance;
    private Vector3 maxRaycastForward;

    // Values modified by the coroutine
    private float minValue;
    private float maxValue;

    private bool cancelHeatmapGeneration = false;

    private IEnumerator heatmapCoroutine = null;
    public IEnumerator HeatmapCoroutine
    {
        get { return heatmapCoroutine; }
    }

    public void StartVisionHeatmapGeneration(Parameters parameters, Progress progress)
    {
        if (heatmapCoroutine != null)
        {
            Debug.LogError("Vision heatmap generation is still running");
            return;
        }

        this.parameters = parameters;
        this.progress = progress;

        progress.info = "";
        progress.value = 0;

        // Pre-compute constants
        resolution2 = parameters.resolution * parameters.resolution;
        spotLeftHalf = parameters.spotSize / 2;
        spotRightHalf = parameters.spotSize - spotLeftHalf;
        spotMaxRadius = Mathf.Sqrt(spotLeftHalf * spotLeftHalf * 3f);
        invCellSize = parameters.resolution / parameters.volumeSize.x;
        invMaxDistance = valueIncrement / spotMaxRadius;
        volumeMin = parameters.volumePosition - parameters.volumeRotation * (parameters.volumeSize * 0.5f);
        maxRaycastDistance = Mathf.Sqrt(parameters.volumeSize.x * parameters.volumeSize.x + parameters.volumeSize.y * parameters.volumeSize.y + parameters.volumeSize.z * parameters.volumeSize.z);
        maxRaycastForward = Vector3.forward * maxRaycastDistance;

        cancelHeatmapGeneration = false;
        heatmapCoroutine = GenerateHeatmap();
    }

    public void CancelVisionHeatmapGeneration()
    {
        if (heatmapCoroutine == null)
        {
            Debug.LogError("Vision heatmap generation is not running");
            return;
        }
        cancelHeatmapGeneration = true;
    }

    IEnumerator GenerateHeatmap()
    {
        progress.info = "Adding mesh colliders ...";
        progress.value = 0;

        IEnumerator enableCollision = EnableCollision(true);
        while (enableCollision.MoveNext()) yield return null;

        VisionDataIO.Header header;
        Dictionary<uint, float> values = new Dictionary<uint, float>();

        foreach (string file in parameters.files)
        {
            if (cancelHeatmapGeneration)
                break;

            progress.info = "Loading motion data ...";
            progress.value = 0.5f;

            yield return null;

            MotionData data = MotionTracker.LoadData(file);
            if (data == null)
            {
                Debug.LogError("Couldn't load data for " + file);
                continue;
            }

            if (cancelHeatmapGeneration)
                break;

            if (data.positions.Count > 1)
            {
                progress.info = "Generating heatmap ...";
                progress.value = 0;

                // Reset
                values.Clear();
                minValue = float.PositiveInfinity;
                maxValue = 0f;

                IEnumerator generateValues = GenerateValues(data, values, parameters.customFPS);
                while (generateValues.MoveNext()) yield return null;

                if (cancelHeatmapGeneration)
                    break;

                string visionFile = file.Substring(0, file.Length - 3) + Extension;
                progress.info = "Saving heatmap ...";
                progress.value = 0.5f;

                yield return null;

                header.resolution = parameters.resolution;
                header.minValue = minValue;
                header.maxValue = maxValue;
                header.sizeX = parameters.volumeSize.x;
                header.sizeY = parameters.volumeSize.y;
                header.sizeZ = parameters.volumeSize.z;
                VisionDataIO.SaveToBinary(visionFile, header, values);
            }
        }

        progress.info = "Removing mesh colliders ...";
        progress.value = 0;

        enableCollision = EnableCollision(false);
        while (enableCollision.MoveNext()) yield return null;

        progress.value = 1f;
        heatmapCoroutine = null;
    }

    private IEnumerator EnableCollision(bool enable)
    {
        const int iterationSize = 1000;
        if (parameters.geometryObjects != null)
        {
            int goCount = parameters.geometryObjects.Length;
            for (int g = 0; g < goCount; g++)
            {
                GameObject go = parameters.geometryObjects[g];
                if (enable)
                {
                    MeshRenderer[] meshes = go.GetComponentsInChildren<MeshRenderer>();
                    int meshCount = meshes.Length;
                    float invMeshCount = 1f / meshCount;
                    for (int m = 0; m < meshCount;)
                    {
                        int max = Mathf.Min(m + iterationSize, meshCount);
                        for (; m < max; m++)
                        {
                            meshes[m].gameObject.AddComponent<MeshCollider>();
                        }
                        progress.value = m * invMeshCount;
                        yield return null;
                    }
                }
                else
                {
                    MeshCollider[] colliders = go.GetComponentsInChildren<MeshCollider>();
                    int colliderCount = colliders.Length;
                    float invColliderCount = 1f / colliderCount;
                    for (int c = 0; c < colliderCount; )
                    {
                        int max = Mathf.Min(c + iterationSize, colliderCount);
                        for (; c < max; c++)
                        {
                            UnityEngine.Object.DestroyImmediate(colliders[c]);
                        }
                        progress.value = c * invColliderCount;
                        yield return null;
                    }
                }
            }
        }
    }

    private IEnumerator GenerateValues(MotionData data, Dictionary<uint, float> values, uint fps)
    {
        if (fps == 0)
        {
            return GenerateValues(data, values);
        }
        else
        {
            return GenerateInterpolatedValues(data, values, fps);
        }
    }

    private IEnumerator GenerateValues(MotionData data, Dictionary<uint, float> values)
    {
        const int iterationSize = 100;

        int count = data.positions.Count;
        float progressPerSample = 1f / count;
        RaycastHit hit;
        AddPosition addPosition;
        if (parameters.spotSize == 0) addPosition = AddCell;
        else addPosition = AddSpot;

        for (int i = 0; i < count;)
        {
            int max = Mathf.Min(i + iterationSize, count);
            for (; i < max; i++)
            {
                Vector3 collision;
                if (Physics.Raycast(data.positions[i], data.rotations[i] * Vector3.forward, out hit, maxRaycastDistance))
                {
                    collision = hit.point - volumeMin;
                }
                else
                {
                    collision = data.positions[i] + data.rotations[i] * maxRaycastForward - volumeMin;
                }

                addPosition(collision, values);
            }
            progress.value = i * progressPerSample;
            if (cancelHeatmapGeneration)
            {
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator GenerateInterpolatedValues(MotionData data, Dictionary<uint, float> values, uint fps)
    {
        const int iterationSize = 100;
        int count = data.positions.Count;
        float progressPerSample = count;
        float timePerMotionFrame = 1f / data.framesPerSecond;
        float time = 0;
        float startTime = 0, endTime = 0;
        int index = -1;
        Vector3 pos1 = data.positions[0], pos2 = data.positions[0];
        Quaternion rot1 = data.rotations[0], rot2 = data.rotations[0];
        uint vissionFrameCount = 0;
        float invFPS = 1f / fps;
        RaycastHit hit;
        AddPosition addPosition;
        if (parameters.spotSize == 0) addPosition = AddCell;
        else addPosition = AddSpot;

        while (true)
        {
            for (int i = 0; i < iterationSize; i++)
            {
                time = vissionFrameCount * invFPS;

                if (time >= endTime)
                {
                    index++;
                    int nextIndex = index + 1;
                    if (nextIndex >= count)
                        yield break;

                    pos1 = pos2;
                    rot1 = rot2;
                    pos2 = data.positions[nextIndex];
                    rot2 = data.rotations[nextIndex];
                    startTime = endTime;
                    endTime = nextIndex * timePerMotionFrame;
                }

                float lerp = Mathf.InverseLerp(startTime, endTime, time);
                Vector3 pos = Vector3.Lerp(pos1, pos2, lerp);
                Quaternion rot = Quaternion.Lerp(rot1, rot2, lerp);

                Vector3 collision;
                if (Physics.Raycast(pos, rot * Vector3.forward, out hit, maxRaycastDistance))
                {
                    collision = hit.point - volumeMin;
                }
                else
                {
                    collision = pos + rot * maxRaycastForward - volumeMin;
                }

                addPosition(collision, values);

                vissionFrameCount++;
            }
            progress.value = index * progressPerSample;
            if (cancelHeatmapGeneration)
            {
                yield break;
            }
            yield return null;
        }
    }

    private void AddCell(Vector3 position, Dictionary<uint, float> values)
    {
        uint xCell = (uint)Mathf.Clamp(Mathf.FloorToInt(position.x * invCellSize), 0, parameters.resolution);
        uint yCell = (uint)Mathf.Clamp(Mathf.FloorToInt(position.y * invCellSize), 0, parameters.resolution);
        uint zCell = (uint)Mathf.Clamp(Mathf.FloorToInt(position.z * invCellSize), 0, parameters.resolution);
        uint cellIndex = xCell + yCell * resolution2 + zCell * parameters.resolution;
        AddValue(cellIndex, values);
    }

    private void AddSpot(Vector3 position, Dictionary<uint, float> values)
    {
        uint xCell = Math.Min((uint)Mathf.Max(Mathf.FloorToInt(position.x * invCellSize), 0), parameters.resolution);
        uint yCell = Math.Min((uint)Mathf.Max(Mathf.FloorToInt(position.y * invCellSize), 0), parameters.resolution);
        uint zCell = Math.Min((uint)Mathf.Max(Mathf.FloorToInt(position.z * invCellSize), 0), parameters.resolution);
        uint minX = (uint)Mathf.Max(xCell - spotLeftHalf, 0);
        uint minY = (uint)Mathf.Max(yCell - spotLeftHalf, 0);
        uint minZ = (uint)Mathf.Max(zCell - spotLeftHalf, 0);
        uint maxX = Math.Min(xCell + spotRightHalf, parameters.resolution);
        uint maxY = Math.Min(yCell + spotRightHalf, parameters.resolution);
        uint maxZ = Math.Min(zCell + spotRightHalf, parameters.resolution);
        float spotMaxRadius = Mathf.Sqrt(spotLeftHalf * spotLeftHalf * 3f);
        for (uint x = minX; x < maxX; x++)
        {
            for (uint y = minY; y < maxY; y++)
            {
                for (uint z = minZ; z < maxZ; z++)
                {
                    uint xDiff = x - xCell, yDiff = y - yCell, zDiff = z - zCell;
                    float radius = Mathf.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);
                    uint cellIndex = x + y * resolution2 + z * parameters.resolution;
                    float value = (spotMaxRadius - radius) * invMaxDistance;
                    if (value > 0)
                    {
                        AddValue(cellIndex, values, value);
                    }
                }
            }
        }
    }

    private void AddValue(uint index, Dictionary<uint, float> values, float addValue = valueIncrement)
    {
        float value;
        if (values.TryGetValue(index, out value))
        {
            value += addValue;
        }
        else
        {
            value = addValue;
        }
        values[index] = value;
        minValue = Mathf.Min(minValue, value);
        maxValue = Mathf.Max(maxValue, value);
    }

}
