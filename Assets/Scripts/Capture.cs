// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to capture one or several images at a fixed frame rate.
//          Specially usefull when required to record at high-res resolution (e.g. 4K)

using UnityEngine;
using System.IO;

public class Capture : MonoBehaviour
{
    public int startFrame = 3;
    public int skipFrames = 0;
    public int stopFrame = 360;
    public int skipFrameRate = 20;
    public int frameRate = 60;
    public string folderName = "Video1";
    bool recording = false;

    void Start()
    {
        Time.captureFramerate = skipFrameRate;
        Directory.CreateDirectory(folderName);
    }

    void Update()
    {
        if (recording)
        {
            if (Time.frameCount < stopFrame)
            {
                CaptureFrame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }
        }
        else
        {
            if (Time.frameCount - startFrame >= skipFrames)
            {
                Time.captureFramerate = frameRate;
                recording = true;
                CaptureFrame();
            }
        }
    }

    public void CaptureFrame()
    {
        string name = string.Format("{0}/img{1:D05}.png", folderName, (Time.frameCount - startFrame));
        Application.CaptureScreenshot(name);
    }
}
