// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: adds a "Capture Frame" button on the Inspector view for Capture instances

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Capture))]
public class CaptureEditor : Editor
{
    private Capture capture;

    public void OnEnable()
    {
        capture = (Capture) target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Capture Frame"))
        {
            capture.CaptureFrame();
        }
    }

}
