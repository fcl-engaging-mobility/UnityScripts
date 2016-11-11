// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: component used to replay a user's recorded motion data

using UnityEngine;

public class MotionPlayer : MonoBehaviour
{
    public bool autoPlayOnStart = true;

    [SerializeField, HideInInspector]
    private string currentFilename = "";
    [SerializeField, HideInInspector]
    private float currentTime;

    [SerializeField, HideInInspector]
    private bool previewMovement = false;
    [SerializeField, HideInInspector]
    private Vector3 originalPosition;
    [SerializeField, HideInInspector]
    private Quaternion originalRotation;

    // Cached file data
    private MotionData data;
    private float maxTime;

    private bool isPlaying = false;

    public string TrackingFile
    {
        get { return currentFilename; }
    }
    public bool IsPlaying
    {
        get { return isPlaying; }
    }
    public float CurrentTime
    {
        get { return currentTime; }
        set
        {
            currentTime = value;
            if (!isPlaying)
            {
                UpdateTracking();
            }
        }
    }
    public float MaxTime
    {
        get { return maxTime; }
    }
    public bool HasData
    {
        get { return (data != null && data.positions.Count > 0); }
    }
    public int FramesPerSecond
    {
        get { return data.framesPerSecond; }
    }
    public bool PreviewMovement
    {
        get { return previewMovement; }
    }

    void Start()
    {
        if (autoPlayOnStart && !isPlaying)
        {
            StartPlaying();
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            currentTime += Time.deltaTime;
            if (currentTime >= maxTime)
            {
                currentTime = maxTime;
                StopPlaying();
            }
            UpdateTracking();
        }
    }

    public void Refresh()
    {
        UpdateTracking();
    }

    void UpdateTracking()
    {
        float fFrame = currentTime * data.framesPerSecond;
        int frame = Mathf.FloorToInt(fFrame);
        float lerp = fFrame % 1;

        int count = data.positions.Count;
        if (frame + 1 < count)
        {
            transform.position = Vector3.LerpUnclamped(data.positions[frame], data.positions[frame + 1], lerp);
            transform.rotation = Quaternion.SlerpUnclamped(data.rotations[frame], data.rotations[frame + 1], lerp);
        }
        else
        {
            transform.position = data.positions[count - 1];
            transform.rotation = data.rotations[count - 1];
        }
    }

    public void StartPlaying()
    {
        if (isPlaying)
        {
            Debug.LogError("TrackerPlayer is already playing");
            return;
        }

        isPlaying = true;
    }

    public void StopPlaying()
    {
        if (!isPlaying)
        {
            Debug.LogError("TrackerPlayer is not playing");
            return;
        }

        isPlaying = false;
    }

    public void Rewind()
    {
        currentTime = 0;
        if (!isPlaying)
        {
            UpdateTracking();
        }
    }

    public void FastForward()
    {
        currentTime = maxTime;
        if (!isPlaying)
        {
            UpdateTracking();
        }
    }

    public void LoadTrackingData(string filename)
    {
        ResetData();

        data = MotionTracker.LoadData(filename);

        if (data != null)
        {
            currentFilename = filename;
            maxTime = data.positions.Count / data.framesPerSecond;
        }
    }

    public void ResetData()
    {
        currentFilename = "";
        data = null;
        maxTime = 0;
    }

    public void PreviewToggle(bool preview)
    {
        if (previewMovement != preview)
        {
            previewMovement = preview;
            if (preview)
            {
                originalPosition = transform.localPosition;
                originalRotation = transform.localRotation;
                Refresh();
            }
            else
            {
                transform.localPosition = originalPosition;
                transform.localRotation = originalRotation;
            }
        }
    }

}
