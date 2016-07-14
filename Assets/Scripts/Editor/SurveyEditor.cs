// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: adds a "Take Screenshot" button on the Inspector view for Survey instances

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Survey))]
public class SurveyEditor : Editor
{
    private Survey mgr;

    public void OnEnable()
    {
        mgr = (Survey) target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Take Screenshot"))
        {
            mgr.TakeScreenShot();
        }
    }

}
