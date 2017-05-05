// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: editor window setup and generate vision data

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class VisionTrackerWindow : EditorWindow
{
    private VolumePlaceholder volume;
    private int _volumeInstanceId = 0;
    
    [SerializeField]
    private GameObject[] geometryObjects = null;

    private string projectPath = "";

    private string motionFileName = "";
    private string motionFilePath = "";
    private string motionFps = "";
    private string motionLength = "";
    private List<string> motionFiles = new List<string>();
    private int selectedMotionFile = -1;

    // Serialization
    private SerializedObject so;
    private SerializedProperty geometryObjectsProp;

    // UI
    private Vector2 scrollVector;
    private GUIStyle toggleButtonStyleNormal = null;
    private GUIStyle toggleButtonStyleToggled = null;
    private static GUIStyle selectedListViewItemStyle = null;

    // Thread
    private VisionTracker visionTracker = new VisionTracker();
    private VisionTracker.Progress progress = new VisionTracker.Progress();
    private bool generateSelectedOnly = true;
    private uint resolution = 2775;
    private bool useMotionFPS = true;
    private int customFPS = 90;
    private int spotSize = 11;

    [MenuItem("Window/Vision Tracker")]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        GetWindow<VisionTrackerWindow>("Vision Tracker").Show();
    }

    void Initialize()
    {
        projectPath = System.IO.Directory.GetCurrentDirectory();
        if (!projectPath.EndsWith("\\"))
        {
            projectPath += "\\";
        }

        if (so == null)
        {
            so = new SerializedObject(this);
            geometryObjectsProp = so.FindProperty("geometryObjects");
        }
        so.Update();
    }

    void OnEnable()
    {
        Initialize();
    }

    void OnInspectorUpdate()
    {
        if (visionTracker.HeatmapCoroutine != null)
        {
            visionTracker.HeatmapCoroutine.MoveNext();
            Repaint();
        }
    }

    void OnGUI()
    {
        if (toggleButtonStyleNormal == null)
        {
            toggleButtonStyleNormal = GUI.skin.button;
            toggleButtonStyleToggled = new GUIStyle(toggleButtonStyleNormal);
            toggleButtonStyleToggled.normal.background = toggleButtonStyleToggled.active.background;
        }
        if (selectedListViewItemStyle == null)
        {
            selectedListViewItemStyle = new GUIStyle(GUI.skin.label);
            selectedListViewItemStyle.normal.background = MakeTexture(8, 8, new Color(0.2f, 0.5f, 1f));
            selectedListViewItemStyle.normal.textColor = Color.white;
        }

        volume = EditorUtility.InstanceIDToObject(_volumeInstanceId) as VolumePlaceholder;

        // Header
        GUILayout.Label("Motion Data", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load File\u2026"))
        {
            string file = EditorUtility.OpenFilePanel("Load motion data", motionFilePath, MotionTracker.Extension);
            if (!string.IsNullOrEmpty(file))
            {
                file = CleanupFilename(file, projectPath);
                if (!motionFiles.Contains(file))
                {
                    motionFiles.Add(file);
                }
                if (motionFiles.Count == 1)
                {
                    selectedMotionFile = 0;
                    LoadMotionDataPreview(motionFiles[0]);
                }
            }
        }
        if (GUILayout.Button("Load Folder\u2026"))
        {
            string path = EditorUtility.OpenFolderPanel("Load motion data", motionFilePath, "");
            if (Directory.Exists(path))
            {
                motionFiles.Clear();
                selectedMotionFile = -1;

                string[] files = Directory.GetFiles(path, "*." + MotionTracker.Extension, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        motionFiles.Add(CleanupFilename(file, projectPath));
                    }
                    selectedMotionFile = 0;
                    LoadMotionDataPreview(motionFiles[0]);
                }
            }
        }
        if (GUILayout.Button("Clear"))
        {
            motionFiles.Clear();
            selectedMotionFile = -1;
            ClearMotionFilePreview();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUI.skin.box);
        scrollVector = GUILayout.BeginScrollView(scrollVector, GUILayout.Height(150));
        for (int i = 0; i < motionFiles.Count; i++)
        {
            if (GUILayout.Button(motionFiles[i], i==selectedMotionFile? selectedListViewItemStyle : GUI.skin.label))
            {
                LoadMotionDataPreview(motionFiles[i]);
                selectedMotionFile = i;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical("Box");
        GUILayout.Label("Path:\t" + motionFilePath);
        GUILayout.Label("Name:\t" + motionFileName);
        GUILayout.Label("FPS:\t" + motionFps);
        GUILayout.Label("Length:\t" + motionLength);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // Header
        GUILayout.Label("Vision Heatmap", EditorStyles.boldLabel);

        // Volume
        //EditorGUILayout.PropertyField(volumeProp);
        volume = EditorGUILayout.ObjectField("Volume", volume, typeof(VolumePlaceholder), true) as VolumePlaceholder;
        _volumeInstanceId = volume ? volume.GetInstanceID() : 0;

        // Resolution
        int res = EditorGUILayout.IntField("Resolution", (int)resolution);
        if (res < 1) res = 1;
        resolution = (uint)res;

        // Spot Size
        int _spotSize = EditorGUILayout.IntField("Spot Size", spotSize);
        if (_spotSize < 1) _spotSize = 1;
        spotSize = _spotSize;

        // FPS
        useMotionFPS = EditorGUILayout.Toggle("Use Motion FPS", useMotionFPS);
        bool enabledState = GUI.enabled;
        GUI.enabled = !useMotionFPS;
        customFPS = EditorGUILayout.IntField("Custom FPS", customFPS);
        GUI.enabled = enabledState;

        // Geometry list
        EditorGUILayout.PropertyField(geometryObjectsProp, true);
        EditorGUILayout.Space();

        var buttonWidth = GUILayout.Width(position.width / 2 - 6);
        GUILayout.BeginHorizontal();
        bool isGenerating = visionTracker.HeatmapCoroutine != null;
        enabledState = GUI.enabled;
        if (isGenerating && generateSelectedOnly)
        {
            if (GUILayout.Button("Cancel", toggleButtonStyleToggled, buttonWidth))
            {
                visionTracker.CancelVisionHeatmapGeneration();
            }
        }
        else
        {
            GUI.enabled = !isGenerating;
            if (GUILayout.Button("Generate Selected Only", buttonWidth))
            {
                GenerateVisionData(true);
            }
            GUI.enabled = enabledState;
        }
        if (isGenerating && !generateSelectedOnly)
        {
            if (GUILayout.Button("Cancel", toggleButtonStyleToggled, buttonWidth))
            {
                visionTracker.CancelVisionHeatmapGeneration();
            }
        }
        else
        {
            GUI.enabled = !isGenerating;
            if (GUILayout.Button("Generate All"))
            {
                GenerateVisionData(false);
            }
            GUI.enabled = enabledState;
        }
        GUILayout.EndHorizontal();

        Rect rect = GUILayoutUtility.GetLastRect();
        EditorGUILayout.Space();

        rect.yMax = GUILayoutUtility.GetLastRect().yMax;

        if (isGenerating)
        {
            EditorGUI.ProgressBar(new Rect(rect.xMin, rect.yMax, rect.width, 18), progress.value, progress.info);
        }
        else
        {
            GUILayout.Space(20);
        }

        if (GUI.changed)
        {
            so.ApplyModifiedProperties();
        }
    }

    private void LoadMotionDataPreview(string filename)
    {
        int fps = 0, count = 0;
        if (MotionTracker.LoadDataHeader(filename, ref fps, ref count))
        {
            UpdateMotionFilePreview(filename, count, fps);
        }
        else
        {
            ClearMotionFilePreview();
        }
    }

    private void UpdateMotionFilePreview(string filename, int count, int fps)
    {
        float maxTime = count / fps;

        var fi = new FileInfo(filename);
        motionFileName = fi.Name;
        motionFilePath = fi.DirectoryName;
        motionFps = fps.ToString();
        motionLength = maxTime < 60 ? maxTime + " s." : (int)maxTime / 60 + ":" + maxTime % 60;
    }

    private void ClearMotionFilePreview()
    {
        motionFilePath = motionFileName = motionFps = motionLength = "";
    }

    private void GenerateVisionData(bool selectedOnly)
    {
        if (motionFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Vision Tracker", "Please add motion data files to the list first", "OK");
        }
        else
        {
            generateSelectedOnly = selectedOnly;
            VisionTracker.Parameters parameters = new VisionTracker.Parameters();
            if (selectedOnly)
            {
                parameters.files = new List<string>();
                parameters.files.Add(motionFiles[selectedMotionFile]);
            }
            else
            {
                parameters.files = new List<string>(motionFiles);
            }
            parameters.volumePosition = volume.transform.position + Vector3.Scale(volume.offset, volume.size * 0.5f);
            parameters.volumeRotation = volume.transform.rotation;
            parameters.volumeSize = volume.size;
            parameters.resolution = resolution;
            parameters.customFPS = (uint)(useMotionFPS ? 0 : customFPS);
            parameters.spotSize = (uint)spotSize;
            parameters.geometryObjects = geometryObjects;
            visionTracker.StartVisionHeatmapGeneration(parameters, progress);
        }
    }

    private static Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = col;
        }

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static string CleanupFilename(string filename, string projectPath)
    {
        filename = filename.Replace('/', Path.DirectorySeparatorChar);
        if (filename.StartsWith(projectPath))
            filename = filename.Substring(projectPath.Length);
        return filename;
    }

}
