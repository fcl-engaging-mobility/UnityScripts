// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: editor window with several traffic-related tools:
//          - Convert Vissim CSV to binary file
//          - Generate traffic heatmaps
//          - Adjust traffic data elevation

using UnityEngine;
using UnityEditor;
using System.IO;

public class TrafficToolsWindow : EditorWindow
{

    // Vissim Files
    private char columnSeparator = ';';
    private char vectorSeparator = ' ';

    // Heatmap
    private TrafficManager trafficMgr;
    private int _trafficMgrId = 0;
    private int resolution = 4096;
    private int pointSize = 4;
    private bool logarithmic = false;
    private Transform area;
    private int _areaInstanceId = 0;

    // Elevation Adjustment

    [SerializeField]
    private GameObject groundMesh = null;
    [SerializeField]
    private Transform movementManager = null;


    private SerializedObject so;
    private SerializedProperty geometryObjectsProp;
    private SerializedProperty managerProp;

    private string adjustmentFilePath = "";
    private string trimmedAdjFilePath = "";
    private char adjColumnSeparator = ';';
    private char adjVectorSeperator = ' ';
    private float adjHeightOffset = 1.5f;

    private string trafficFilePath = "";
    private string projectPath = "";

    private static AssetType[] cyclists;


    [MenuItem("Window/Traffic Tools")]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        GetWindow<TrafficToolsWindow>("Traffic Tools").Show();
    }

    void Initialize()
    {
        projectPath = Directory.GetCurrentDirectory();
        if (!projectPath.EndsWith("\\"))
        {
            projectPath += "\\";
        }

        if (so == null)
        {
            so = new SerializedObject(this);
            geometryObjectsProp = so.FindProperty("groundMesh");
            managerProp = so.FindProperty("movementManager");
        }
        so.Update();

    }

    void OnEnable()
    {
        Initialize();
    }

    void OnGUI()
    {
        trafficMgr = EditorUtility.InstanceIDToObject(_trafficMgrId) as TrafficManager;
        area = EditorUtility.InstanceIDToObject(_areaInstanceId) as Transform;

        // Header
        GUILayout.Label("Vissim Files", EditorStyles.boldLabel);

        string sep = EditorGUILayout.TextField("Column Separator", columnSeparator.ToString());
        columnSeparator = (string.IsNullOrEmpty(sep))? ';' : sep[0];

        sep = EditorGUILayout.TextField("Vector Separator", vectorSeparator.ToString());
        vectorSeparator = (string.IsNullOrEmpty(sep)) ? ' ' : sep[0];

        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Convert to Binary \u2026"))
        {
            string file = EditorUtility.OpenFilePanel("Select traffic data", trafficFilePath, TrafficIO.VISSIM_VEHICLES_EXTENSION + "," + TrafficIO.VISSIM_PEDESTRIANS_EXTENSION);
            if (!string.IsNullOrEmpty(file))
            {
                trafficFilePath = Path.GetDirectoryName(file);
                ConvertToBinary(CleanupFilename(file, projectPath));
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Header
        GUILayout.Label("Heatmap", EditorStyles.boldLabel);

        // Offset
        trafficMgr = EditorGUILayout.ObjectField("Traffic Manager", trafficMgr, typeof(TrafficManager), true) as TrafficManager;
        _trafficMgrId = trafficMgr ? trafficMgr.GetInstanceID() : 0;

        // Resolution
        int res = EditorGUILayout.IntField("Resolution", resolution);
        if (res < 1) res = 1;
        resolution = res;

        // Point Size
        int _pointSize = EditorGUILayout.IntField("Point Size", pointSize);
        if (_pointSize < 1) _pointSize = 1;
        pointSize = _pointSize;

        // Logarithmic
        logarithmic = EditorGUILayout.Toggle("Logarithmic", logarithmic);

        // Area
        area = EditorGUILayout.ObjectField("Area", area, typeof(Transform), true) as Transform;
        _areaInstanceId = area ? area.GetInstanceID() : 0;

        // Generate button
        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Generate \u2026"))
        {
            string path = EditorUtility.OpenFolderPanel("Load traffic data", trafficFilePath, "");
            if (Directory.Exists(path))
            {
                trafficFilePath = path;
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    GenerateHeatmap(CleanupFilename(file, projectPath));
                }
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Header
        GUILayout.Label("Elevation Adjustsment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(geometryObjectsProp, true);
        EditorGUILayout.PropertyField(managerProp, true);

        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Path to traffic data", trimmedAdjFilePath, GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 30));

        if (GUILayout.Button("\u2026"))
        {
            string path = EditorUtility.OpenFilePanel("Load traffic data", adjustmentFilePath, "");

            if (File.Exists(path) && (path.EndsWith(TrafficIO.VISSIM_PEDESTRIANS_EXTENSION) ||
                    path.EndsWith(TrafficIO.VISSIM_VEHICLES_EXTENSION)))
            {
                adjustmentFilePath = path;
                trimmedAdjFilePath = Path.GetFileName(path);
            }
            else
            {
                adjustmentFilePath = "";
                trimmedAdjFilePath = "";
            }
        }

        GUILayout.EndHorizontal();

        sep = EditorGUILayout.TextField("Column Separator", adjColumnSeparator.ToString());
        adjColumnSeparator = (string.IsNullOrEmpty(sep)) ? ';' : sep[0];

        sep = EditorGUILayout.TextField("Vector Separator", adjVectorSeperator.ToString());
        adjVectorSeperator = (string.IsNullOrEmpty(sep)) ? ';' : sep[0];

        adjHeightOffset = EditorGUILayout.FloatField("Height offset", adjHeightOffset);

        GUILayout.BeginHorizontal();

        GUILayout.Space(EditorGUIUtility.labelWidth);
        if (GUILayout.Button("Adjust Elevation \u2026"))
        {
            if (adjustmentFilePath != "" && geometryObjectsProp != null && managerProp != null)
            {
                if (EnableCollison(true))
                {
                    AdjustHeight();
                }
                EnableCollison(false);
                Repaint();
            }
            else
            {
                Debug.LogError("Missing elevation data");
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //if (GUILayout.Button("Enable collision \u2026"))
        //{
        //    EnableCollison(true);
        //}

        //if (GUILayout.Button("Disable collision \u2026"))
        //{
        //    EnableCollison(false);
        //}

        if (GUI.changed)
        {
            so.ApplyModifiedProperties();
        }
    }

    private bool AdjustHeight()
    {
        Vector3 front;
        Vector3 back;
        string line;
        string[] data;
        string[] vecData;

        bool isVehicles = adjustmentFilePath.EndsWith(TrafficIO.VISSIM_VEHICLES_EXTENSION);

        string msg = "Adjusting height in " + Path.GetFileName(adjustmentFilePath) + " : ";

        using (StreamReader reader = new StreamReader(File.OpenRead(adjustmentFilePath)))
        using (StreamWriter writer = new StreamWriter(File.Open(CreateFixedPath(adjustmentFilePath), FileMode.Create)))
        {
            // Copy the header
            int next = reader.Peek();
            while (!reader.EndOfStream && !char.IsDigit((char)next))
            {
                writer.WriteLine(reader.ReadLine());
                next = reader.Peek();
            }

            int dataRead = 0;
            int pass = 0;
            RaycastHit hit;

            float loadLength = reader.BaseStream.Length;
            float invLoadLength = 1f / loadLength;

            while (!reader.EndOfStream)
            {
                if (pass % 5000 == 0)
                {
                    if (DisplayProgressBar(msg, dataRead * invLoadLength))
                    {
                        return false;
                    }
                }

                // split data
                line = reader.ReadLine();
                data = line.Split(adjColumnSeparator);
                dataRead += line.Length;
                pass++;

                // adjust first point
                vecData = data[3].Split(adjVectorSeperator);
                front = new Vector3(float.Parse(vecData[0]), float.Parse(vecData[2]) + adjHeightOffset, float.Parse(vecData[1]));

                front = movementManager.TransformPoint(front);

                if (Physics.Raycast(front, Vector3.down, out hit, 10))
                {
                    front.y = hit.point.y;
                }
                else
                {
                    Debug.LogWarning("No front collision within 10m for line " + pass);
                    ShowConflict(front);
                    ClearProgressBar();
                    return false;
                }

                front = movementManager.InverseTransformPoint(front);


                // adjust second point
                vecData = data[4].Split(adjVectorSeperator);
                back = new Vector3(float.Parse(vecData[0]), float.Parse(vecData[2]) + adjHeightOffset, float.Parse(vecData[1]));

                back = movementManager.TransformPoint(back);

                if (Physics.Raycast(back, Vector3.down, out hit, 10))
                {
                    back.y = hit.point.y;
                }
                else
                {
                    Debug.LogWarning("No back collision within 10m for line " + pass);
                    ShowConflict(back);
                    ClearProgressBar();
                    return false;
                }

                back = movementManager.InverseTransformPoint(back);

                if (!isVehicles)
                {
                    front.y = back.y = (front.y + back.y) * 0.5f;
                }

                // reassemble data
                line = data[0] + adjColumnSeparator + 
                       data[1] + adjColumnSeparator + 
                       data[2] + adjColumnSeparator + 
                       front.x + adjVectorSeperator + 
                       front.z + adjVectorSeperator +
                       front.y + adjColumnSeparator +
                       back.x + adjVectorSeperator +
                       back.z + adjVectorSeperator +
                       back.y;

                // only cars need a 6th field
                if (isVehicles)
                {
                    line += adjColumnSeparator + data[5];
                }

                // write
                writer.WriteLine(line);
            }
        }

        ClearProgressBar();
        return true;
    }

    private bool EnableCollison(bool enable)
    {
        if (enable)
        {
            MeshRenderer[] meshes = groundMesh.GetComponentsInChildren<MeshRenderer>();

            int meshCount = meshes.Length;
            float invMeshCount = 1f / meshCount;

            for (int m = 0; m < meshCount; ++m)
            {
                meshes[m].gameObject.AddComponent<MeshCollider>();

                if (m % 100 == 0)
                {
                    if (DisplayProgressBar("Adding Colliders", m * invMeshCount))
                        return false;
                }
            }
        }
        else
        {
            MeshCollider[] colliders = groundMesh.GetComponentsInChildren<MeshCollider>();

            int colliderCount = colliders.Length;
            float invCollCount = 1f / colliderCount;

            for (int c = 0; c < colliderCount; ++c)
            {
                DestroyImmediate(colliders[c]);

                if (c % 100 == 0)
                {
                    if (DisplayProgressBar("Removing Colliders", c * invCollCount))
                        return false;
                }
            }
        }
        ClearProgressBar();
        return true;
    }

    private bool DisplayProgressBar(string msg, float progress)
    {
        if (EditorUtility.DisplayCancelableProgressBar("Loading", msg + "  " + (int)(progress * 100) + " %", progress))
        {
            EditorUtility.ClearProgressBar();
            EditorApplication.isPlaying = false;
            return true;
        }
        return false;
    }

    private void ClearProgressBar()
    {
        EditorUtility.ClearProgressBar();
    }
    
    private string CreateFixedPath(string path)
    {
        return Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_fixed" + Path.GetExtension(path);
    }

    private void GenerateHeatmap(string filename)
    {
        if (cyclists == null)
        {
            cyclists = new AssetType[1];
            cyclists[0] = AssetType.Cyclist;
        }

        if (area == null)
        {
            Debug.LogError("Can't generate heatmap with unassigned area");
            return;
        }

        Bounds aabb = new Bounds(area.position, area.localScale);
        if (filename.EndsWith(TrafficIO.VISSIM_VEHICLES_EXTENSION))
        {
            TrafficData data = TrafficIO.Load(filename, columnSeparator, vectorSeparator, true);
            if (data == null) return;
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Vehicles_Heatmap", cyclists, false);
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Cyclists_Heatmap", cyclists, true);
        }
        else if(filename.EndsWith(TrafficIO.VISSIM_PEDESTRIANS_EXTENSION))
        {
            TrafficData data = TrafficIO.Load(filename, columnSeparator, vectorSeparator, true);
            if (data == null) return;
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Pedestrians_Heatmap");
        }
        else if (filename.EndsWith(TrafficIO.BINARY_VEHICLES_EXTENSION))
        {
            TrafficData data = TrafficIO.LoadBinary(filename, true);
            if (data == null) return;
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Vehicles_Heatmap", cyclists, false);
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Cyclists_Heatmap", cyclists, true);
        }
        else if (filename.EndsWith(TrafficIO.BINARY_PEDESTRIANS_EXTENSION))
        {
            TrafficData data = TrafficIO.LoadBinary(filename, true);
            if (data == null) return;
            Heatmap.GenerateHeatmap(data, trafficMgr.transform.position, resolution, pointSize * 0.5f, aabb, logarithmic, "Pedestrians_Heatmap");
        }
        AssetDatabase.Refresh();
    }

    private void ConvertToBinary(string filename)
    {
        if (filename.EndsWith(TrafficIO.VISSIM_VEHICLES_EXTENSION))
        {
            TrafficData data = TrafficIO.Load(filename, columnSeparator, vectorSeparator, true);
            if (data == null) return;
            filename = filename.Remove(filename.Length - TrafficIO.VISSIM_VEHICLES_EXTENSION.Length) + TrafficIO.BINARY_VEHICLES_EXTENSION;
            TrafficIO.SaveBinary(data, filename);
        }
        else if (filename.EndsWith(TrafficIO.VISSIM_PEDESTRIANS_EXTENSION))
        {
            TrafficData data = TrafficIO.Load(filename, columnSeparator, vectorSeparator, true);
            if (data == null) return;
            filename = filename.Remove(filename.Length - TrafficIO.VISSIM_PEDESTRIANS_EXTENSION.Length) + TrafficIO.BINARY_PEDESTRIANS_EXTENSION;
            TrafficIO.SaveBinary(data, filename);
        }
    }

    private static string CleanupFilename(string filename, string projectPath)
    {
        filename = filename.Replace('/', Path.DirectorySeparatorChar);
        if (filename.StartsWith(projectPath))
            filename = filename.Substring(projectPath.Length);
        return filename;
    }

    private void ShowConflict(Vector3 pos)
    {
        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.transform.position = pos + Vector3.down * adjHeightOffset;
        cap.transform.localScale = new Vector3(0.02f, 2f, 0.02f);
        Selection.activeGameObject = cap;
        SceneView.FrameLastActiveSceneView();
    }

}
