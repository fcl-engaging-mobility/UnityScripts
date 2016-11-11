// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: Editor script to add a start/stop tracking toggle button

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MotionTracker))]
public class MotionTrackerEditor : Editor
{
    private MotionTracker tracker;

    private static GUIStyle toggleButtonStyleNormal = null;
    private static GUIStyle toggleButtonStyleToggled = null;

    public void OnEnable()
    {
        tracker = (MotionTracker) target;
    }

    public override void OnInspectorGUI()
    {
        if (toggleButtonStyleNormal == null)
        {
            toggleButtonStyleNormal = "Button";
            toggleButtonStyleToggled = new GUIStyle(toggleButtonStyleNormal);
            toggleButtonStyleToggled.normal.background = toggleButtonStyleToggled.active.background;
        }

        DrawDefaultInspector();

        GUILayout.BeginHorizontal();
        GUI.enabled = EditorApplication.isPlaying;
        bool isTracking = tracker.IsTracking?
            GUILayout.Toggle(true, "Stop Tracking", toggleButtonStyleToggled) : 
            GUILayout.Toggle(false, "Start Tracking", toggleButtonStyleNormal);

        if (isTracking != tracker.IsTracking)
        {
            if (isTracking)
                tracker.StartTracking();
            else
                tracker.StopTracking();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }
}
