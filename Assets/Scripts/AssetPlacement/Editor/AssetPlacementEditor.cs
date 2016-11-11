// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: custom inspector UI for asset placement

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssetPlacement))]
public class AssetPlacementEditor : Editor
{
    private AssetPlacement placement;

    public void OnEnable()
    {
        placement = target as AssetPlacement;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        switch (placement.positionMode)
        {
            case AssetPlacement.Mode.Fixed:
                EditorGUILayout.Space();
                placement.positionOffset = EditorGUILayout.Vector3Field("Position", placement.positionOffset);
                break;
            case AssetPlacement.Mode.Offset:
                EditorGUILayout.Space();
                placement.positionOffset = EditorGUILayout.Vector3Field("Position Offset", placement.positionOffset);
                break;
            case AssetPlacement.Mode.RangedRandom:
            case AssetPlacement.Mode.OffsetRangedRandom:
                EditorGUILayout.Space();
                placement.positionMin = EditorGUILayout.Vector3Field("Position Min Offset", placement.positionMin);
                placement.positionMax = EditorGUILayout.Vector3Field("Position Max Offset", placement.positionMax);
                break;
        }
        switch (placement.rotationMode)
        {
            case AssetPlacement.Mode.Fixed:
                EditorGUILayout.Space();
                placement.rotationOffset = EditorGUILayout.Vector3Field("Rotation", placement.rotationOffset);
                break;
            case AssetPlacement.Mode.Offset:
                EditorGUILayout.Space();
                placement.rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", placement.rotationOffset);
                break;
            case AssetPlacement.Mode.RangedRandom:
            case AssetPlacement.Mode.OffsetRangedRandom:
                EditorGUILayout.Space();
                placement.rotationMin = EditorGUILayout.Vector3Field("Rotation Min Offset", placement.rotationMin);
                placement.rotationMax = EditorGUILayout.Vector3Field("Rotation Max Offset", placement.rotationMax);
                break;
        }
        switch (placement.scaleMode)
        {
            case AssetPlacement.Mode.Fixed:
                EditorGUILayout.Space();
                placement.scaleOffset = EditorGUILayout.Vector3Field("Scale", placement.scaleOffset);
                break;
            case AssetPlacement.Mode.Offset:
                EditorGUILayout.Space();
                placement.scaleOffset = EditorGUILayout.Vector3Field("Scale Offset", placement.scaleOffset);
                break;
            case AssetPlacement.Mode.RangedRandom:
            case AssetPlacement.Mode.OffsetRangedRandom:
                EditorGUILayout.Space();
                placement.scaleMin = EditorGUILayout.Vector3Field("Scale Min Offset", placement.scaleMin);
                placement.scaleMax = EditorGUILayout.Vector3Field("Scale Max Offset", placement.scaleMax);
                break;
        }

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Reload"))
        {
            placement.Load();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Save"))
        {
            placement.Save();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Clear"))
        {
            placement.Clear();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

}