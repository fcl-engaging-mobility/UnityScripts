// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to record, load and store a user's motion data

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class MotionData
{
    public int framesPerSecond = 30;
    public List<Vector3> positions = null;
    public List<Quaternion> rotations = null;
}

public class MotionTracker : MonoBehaviour {

    public const string Extension = "trk";

    [Tooltip("in seconds")]
    public int preallocatedTime = 120;
    public int framesPerSecond = 30;
    public string pathToSave = "Trackings";

    private bool isTracking = false;
    private float nextUpdateTime;
    private float nextUpdateOffset;

    private MotionData data = null;

    public bool IsTracking
    {
        get { return isTracking; }
    }

    // Use LateUpdate instead of Update to allow the animation to update the camera's position first
    void LateUpdate()
    {
	    if (isTracking)
        {
            while (Time.time >= nextUpdateTime)
            {
                data.positions.Add(transform.position);
                data.rotations.Add(transform.rotation);
                nextUpdateTime += nextUpdateOffset;
            }
        }
    }

    public void StartTracking()
    {
        if (isTracking)
        {
            Debug.LogError("Already tracking");
            return;
        }

        int samples = framesPerSecond * preallocatedTime;
        data = new MotionData();
        data.framesPerSecond = framesPerSecond;
        data.positions = new List<Vector3>(samples);
        data.rotations = new List<Quaternion>(samples);

        nextUpdateOffset = 1f / framesPerSecond;
        nextUpdateTime = Time.time;
        isTracking = true;
    }

    public void StopTracking()
    {
        if (!isTracking)
        {
            Debug.LogError("Not tracking");
            return;
        }

        isTracking = false;

        SaveData(pathToSave, data);
    }

    public static MotionData LoadData(string filename)
    {
        if (!File.Exists(filename))
            return null;

        MotionData data = new MotionData();

        using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
            data.framesPerSecond = reader.ReadInt32();
            int count = reader.ReadInt32();

            data.positions = new List<Vector3>(count);
            data.rotations = new List<Quaternion>(count);
            for (int i = 0; i < count; i++)
            {
                data.positions.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                data.rotations.Add(new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
        }

        return data;
    }

    public static bool LoadDataHeader(string filename, ref int fps, ref int count)
    {
        if (!File.Exists(filename))
            return false;

        using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
        {
            fps = reader.ReadInt32();
            count = reader.ReadInt32();
        }
        return true;
    }

    public static void SaveData(string path, MotionData data)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        int count = data.positions.Count;
        if (data.positions.Count != data.rotations.Count)
        {
            Debug.LogError(data.positions.Count + " positions vs. " + data.rotations.Count + " rotations!");
            count = Mathf.Min(count, data.rotations.Count);
        }

        string filename = path + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.") + Extension;

        Debug.Log("Saving " + count + " samples to " + filename);

        using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
            writer.Write(data.framesPerSecond);
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                var pos = data.positions[i];
                writer.Write(pos.x);
                writer.Write(pos.y);
                writer.Write(pos.z);

                var rot = data.rotations[i];
                writer.Write(rot.x);
                writer.Write(rot.y);
                writer.Write(rot.z);
                writer.Write(rot.w);
            }
        }
    }

}
