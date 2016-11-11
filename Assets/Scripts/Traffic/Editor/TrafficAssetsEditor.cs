// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: extends the default inspector for TrafficAssets indicating
//          any error messages

using UnityEditor;

[CustomEditor(typeof(TrafficAssets))]
public class TrafficAssetsEditor : Editor
{
    private TrafficAssets assets;

    public void OnEnable()
    {
        assets = target as TrafficAssets;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        string msg = assets.GetErrorMessage();
        if (msg != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(msg, MessageType.Error, true);
        }
    }

}