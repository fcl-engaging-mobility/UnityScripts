// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: custom inspector implementation for a MotionPlayer

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MotionPlayer))]
public class MotionPlayerEditor : Editor
{
    private MotionPlayer player;

    private Texture play;
    private Texture stop;
    private Texture rewind;
    private Texture fastforward;

    private string projectPath = "";
    private string loadingPath = "";

    public void OnEnable()
    {
        player = (MotionPlayer) target;

        play = Resources.Load<Texture>("GUI/play");
        stop = Resources.Load<Texture>("GUI/stop");
        rewind = Resources.Load<Texture>("GUI/rewind");
        fastforward = Resources.Load<Texture>("GUI/fastforward");

        projectPath = System.IO.Directory.GetCurrentDirectory();
        if (!projectPath.EndsWith("\\"))
        {
            projectPath += "\\";
        }

        string trackingFile = player.TrackingFile;
        if (string.IsNullOrEmpty(trackingFile))
        {
            loadingPath = "";
        }
        else
        {
            loadingPath = new System.IO.FileInfo(trackingFile).DirectoryName;

            // Reload the file if cached data has been reset
            if (!player.HasData)
            {
                player.LoadTrackingData(trackingFile);
                if (player.PreviewMovement)
                {
                    player.Refresh();
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tracking Data File");
        if (GUILayout.Button("\u2026", GUILayout.ExpandWidth(false)))
        {
            string filename = EditorUtility.OpenFilePanel("Load tracking data", loadingPath, MotionTracker.Extension);
            if (!string.IsNullOrEmpty(filename))
            {
                player.LoadTrackingData(filename);
                if (player.PreviewMovement)
                {
                    player.Refresh();
                }
            }
        }
        GUI.enabled = player.HasData;
        if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
        {
            player.ResetData();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        string trackingFile = player.TrackingFile;
        string fps = "";
        string length = "";
        if (string.IsNullOrEmpty(trackingFile))
        {
            loadingPath = "";
            trackingFile = "";
        }
        else
        {
            var fi = new System.IO.FileInfo(trackingFile);
            loadingPath = fi.DirectoryName;
            if (loadingPath.StartsWith(projectPath))
                loadingPath = loadingPath.Substring(projectPath.Length);

            trackingFile = fi.Name;
            fps = player.FramesPerSecond.ToString();
            length = player.MaxTime < 60? player.MaxTime + " s." : (int)player.MaxTime / 60 + ":" + player.MaxTime % 60;
        }

        GUILayout.BeginVertical("Box");
        GUILayout.Label("Path:\t" + loadingPath);
        GUILayout.Label("Name:\t" + trackingFile);
        GUILayout.Label("FPS:\t" + fps);
        GUILayout.Label("Length:\t" + length);
        GUILayout.EndVertical();

        EditorGUILayout.Space();
        player.autoPlayOnStart = EditorGUILayout.Toggle("Auto Play On Start", player.autoPlayOnStart);
        EditorGUILayout.Space();

        player.PreviewToggle(EditorGUILayout.BeginToggleGroup("Preview Movement", player.PreviewMovement));

        GUILayout.BeginVertical("Box");
        bool isPlaying = EditorApplication.isPlaying;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool enabledState = GUI.enabled;
        GUI.enabled = enabledState && isPlaying && player.HasData;
        if (GUILayout.Button(rewind, GUILayout.ExpandWidth(false)))
        {
            player.Rewind();
        }
        GUI.enabled = enabledState && isPlaying && player.HasData && player.IsPlaying;
        if (GUILayout.Button(stop, GUILayout.ExpandWidth(false)))
        {
            player.StopPlaying();
        }
        GUI.enabled = enabledState && isPlaying && player.HasData && !player.IsPlaying;
        if (GUILayout.Button(play, GUILayout.ExpandWidth(false)))
        {
            player.StartPlaying();
        }
        GUI.enabled = enabledState && isPlaying && player.HasData;
        if (GUILayout.Button(fastforward, GUILayout.ExpandWidth(false)))
        {
            player.FastForward();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUI.enabled = enabledState && player.PreviewMovement;
        float newValue = GUILayout.HorizontalSlider(player.CurrentTime, 0, player.MaxTime);
        if (newValue != player.CurrentTime)
        {
            player.CurrentTime = newValue;
        }
        GUILayout.EndVertical();
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();
        GUI.enabled = true;
    }
}
