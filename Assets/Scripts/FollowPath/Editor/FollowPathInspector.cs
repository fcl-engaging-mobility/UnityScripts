// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: adds a custom inspector UI for easy path manipulation

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FollowPath))]
public class FollowPathInspector : Editor
{
	private static Color[] handleColors =
    {
		Color.red,
		Color.blue,
		Color.green
	};

    private const float handleScale = 0.05f;
    private const float pickScale = 0.07f;

    private FollowPath path;
	private Transform handleTransform;
	private Quaternion handleRotation;
	private int selectedIndex = -1;

    public void OnEnable()
    {
        path = target as FollowPath;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        bool hasValidSelectedIndex = selectedIndex >= 0 && selectedIndex < path.ControlPointCount;
        bool hasValidSelectedPoint = hasValidSelectedIndex && selectedIndex % 3 == 0;
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(path, "Add Point");
            path.AddCurve();
        }
        GUILayout.EndHorizontal();
        bool guiEnabled = GUI.enabled;
        GUI.enabled = hasValidSelectedPoint;
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Insert Point After Selected"))
        {
            Undo.RecordObject(path, "Insert Point");
            path.InsertPoint(selectedIndex);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        GUI.enabled = hasValidSelectedPoint && path.ControlPointCount > 4;
        if (GUILayout.Button("Remove Selected Point"))
        {
            Undo.RecordObject(path, "Remove Point");
            path.RemovePoint(selectedIndex);
        }
        GUILayout.EndHorizontal();
        GUI.enabled = guiEnabled;

        if (hasValidSelectedIndex)
        {
            DrawSelectedPointInspector();
        }
    }

    private void DrawSelectedPointInspector()
    {
        EditorGUILayout.Space();
        GUILayout.Label("Selected Point", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float time = EditorGUILayout.FloatField("Time", path.GetTime(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(path, "Move Time");
            path.SetTime(selectedIndex, time);
        }
        EditorGUI.BeginChangeCheck();
		Vector3 point = EditorGUILayout.Vector3Field("Position", path.GetControlPoint(selectedIndex));
		if (EditorGUI.EndChangeCheck())
        {
			Undo.RecordObject(path, "Move Point");
            path.SetControlPoint(selectedIndex, point);
		}
		EditorGUI.BeginChangeCheck();
        FollowPath.HandleMode mode = (FollowPath.HandleMode)EditorGUILayout.EnumPopup("Mode", path.GetControlPointMode(selectedIndex));
		if (EditorGUI.EndChangeCheck())
        {
			Undo.RecordObject(path, "Change Point Mode");
            path.SetControlPointMode(selectedIndex, mode);
		}
	}

    private void OnSceneGUI()
    {
        if (path.enabled)
        {
            handleTransform = path.transform;
            handleRotation = Tools.pivotRotation == PivotRotation.Local ?
                handleTransform.rotation : Quaternion.identity;

            Vector3 p0 = ShowPoint(0);
            for (int i = 1; i < path.ControlPointCount; i += 3)
            {
                Vector3 p1 = ShowPoint(i);
                Vector3 p2 = ShowPoint(i + 1);
                Vector3 p3 = ShowPoint(i + 2);

                Handles.color = Color.gray;
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p2, p3);

                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
                p0 = p3;
            }

            if (path.showSpeedGraph)
            {
                path.DrawSpeedGraph();
            }
        }
    }

	private Vector3 ShowPoint(int index)
    {
        Vector3 point = path.GetControlPoint(index);
        float size = HandleUtility.GetHandleSize(point);
		if (index == 0)
			size *= 2f;

        Handles.color = (index % 3 == 0)? Color.cyan : handleColors[(int)path.GetControlPointMode(index)];
        if (Handles.Button(point, handleRotation, size * handleScale, size * pickScale, Handles.DotCap))
        {
            selectedIndex = index;
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Point");
                path.SetControlPoint(index, point);
            }
        }
        return point;
    }

}