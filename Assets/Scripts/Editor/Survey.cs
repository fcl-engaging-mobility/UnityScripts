// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to generate the survey images.

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

public class Survey : MonoBehaviour
{
    public Camera surveyCamera = null;
    public string outputPath = "SurveyScreenshots";
    public Transform currentExperiment = null;
    public bool onlyCurrent = true;
    private float offsetCars = 0;
    private float offsetCyclists = 0;
    private const float SpeedCars = 0.12f;
    private const float SpeedCyclists = 0.03f;
    private const float NumberOfFrames = 50;
    private const float CameraZDistance = 7.5f;
    private const float MaxCameraZPos = 2.5f;
    private bool bUpdateVehicles = false;
    private IEnumerator experiments = null;

    void Start()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        if (onlyCurrent)
        {
            if (currentExperiment == null)
            {
                Debug.LogError("currentExperiment is null!");
                EditorApplication.isPlaying = false;
                return;
            }
            StartExperiment();
        }
        else
        {
            experiments = Experiments();
            StartCoroutine(ExportNextExperiment());
        }
    }

    void Update()
    {
        if (bUpdateVehicles)
        {
            UpdateVehicles();
        }
    }

    IEnumerator ExportNextExperiment()
    {
        if (currentExperiment != null)
        {
            yield return new WaitForSeconds(0.05f);
            Destroy(currentExperiment.gameObject);
            yield return new WaitForSeconds(0.3f);
        }

        if (!experiments.MoveNext())
        {
            Debug.Log("Done!");

            EditorApplication.isPlaying = false;
            //Application.Quit();
        }
    }

    IEnumerator Experiments()
    {
        string sSurveyFolder = "_Survey";
        string sFolderPath = Application.dataPath + Path.DirectorySeparatorChar + sSurveyFolder;
        string[] aFilePaths = Directory.GetFiles(sFolderPath);

        foreach (string sFilePath in aFilePaths)
        {
            if (sFilePath.EndsWith(".fbx"))
            {
                string sAssetPath = sFilePath.Substring(Application.dataPath.Length - 6);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(sAssetPath);
                var experiment = Instantiate(go);

                currentExperiment = experiment.transform;
                StartExperiment();

                yield return experiment;
            }
        }
    }

    void StartExperiment()
    {
        //InitCamera();
        offsetCars = SpeedCars * NumberOfFrames;
        offsetCyclists = SpeedCyclists * NumberOfFrames;
        bUpdateVehicles = true;
    }

    void UpdateVehicles()
    {
        offsetCars -= SpeedCars;
        offsetCyclists -= SpeedCyclists;
        if (offsetCars <= 0 || offsetCyclists <= 0)
        {
            bUpdateVehicles = false;

            TakeScreenShot();

            if (experiments != null)
            {
                StartCoroutine(ExportNextExperiment());
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }

        for (int i = 0; i < currentExperiment.childCount; i++)
        {
            var child = currentExperiment.GetChild(i);
            if (child.name.StartsWith("X.") && child.childCount > 0)
            {
                var grandChild = child.GetChild(0);
                var newPos = child.localPosition;
                float offset = offsetCyclists;
                if (/*child.childCount != 1 ||*/ grandChild.childCount < 7)
                {
                    offset = offsetCars;
                }
                newPos.z = (Mathf.Abs(grandChild.localRotation.y) > 0.1f) ? offset : -offset;
                child.localPosition = newPos;
            }
            else if (child.name.StartsWith("Cycle"))
            {
                var newPos = child.localPosition;
                newPos.z = (Mathf.Abs(child.GetChild(0).localRotation.y) > 0.1f) ? offsetCyclists : -offsetCyclists;
                child.localPosition = newPos;
            }
        }
    }

    public void TakeScreenShot()
    {
        string filename = outputPath + Path.DirectorySeparatorChar + currentExperiment.name; //.Remove(currentExperiment.name.Length - 7);
        Application.CaptureScreenshot(filename + ".png");
    }

    private void InitCamera()
    {
        float minX = float.MaxValue;
        for (int i = 0; i < currentExperiment.childCount; i++)
        {
            var child = currentExperiment.GetChild(i);
            var grandChild = child.GetChild(0);
            if (Mathf.Abs(grandChild.localRotation.y) < 0.1f ||
                grandChild.position.z + CameraZDistance > MaxCameraZPos)
                continue;

            if (child.name.StartsWith("X.") && child.childCount > 0)
            {
                if (grandChild.childCount >= 7)
                {
                    minX = Mathf.Min(minX, grandChild.position.x);
                }
            }
            else if (child.name.StartsWith("Cycle"))
            {
                minX = Mathf.Min(minX, grandChild.position.x);
            }
        }
        float maxX = minX;
        float maxZ = float.MinValue;
        for (int i = 0; i < currentExperiment.childCount; i++)
        {
            var child = currentExperiment.GetChild(i);
            var grandChild = child.GetChild(0);
            if (Mathf.Abs(grandChild.localRotation.y) < 0.1f ||
                grandChild.position.z + CameraZDistance > MaxCameraZPos)
                continue;

            if (child.name.StartsWith("X.") && child.childCount > 0)
            {
                if (grandChild.childCount >= 7)
                {
                    if (grandChild.position.x < minX + 3.0)
                    {
                        //maxX = Mathf.Max(maxX, grandChild.position.x);
                        maxZ = Mathf.Max(maxZ, grandChild.position.z);
                    }
                }
            }
            else if (child.name.StartsWith("Cycle"))
            {
                if (grandChild.position.x < minX + 3.0)
                {
                    //maxX = Mathf.Max(maxX, grandChild.position.x);
                    maxZ = Mathf.Max(maxZ, grandChild.position.z);
                }
            }
        }

        Vector3 camPos = surveyCamera.transform.position;
        camPos.x = (maxX + minX) * 0.5f;// + 1;
        camPos.z = maxZ + CameraZDistance;
        if (camPos.z > 2.5f)
        {
            Debug.LogWarning("UGH!");
            camPos.z = 2.5f;
        }
        surveyCamera.transform.position = camPos;

        var b = surveyCamera.GetComponent<BoxCollider>().bounds;
        for (int i = 0; i < currentExperiment.childCount; i++)
        {
            var child = currentExperiment.GetChild(i);
            if (Intersects(child, b) &&
                (child.name.StartsWith("Cycle") ||
                (child.name.StartsWith("X.") && child.GetChild(0).childCount >= 7)))
            {
                child.gameObject.SetActive(false);
            }
        }

        var euler = surveyCamera.transform.localEulerAngles;
        euler.y = camPos.x < 0 ? 175 : 185;
        surveyCamera.transform.localEulerAngles = euler;
    }

    static bool Intersects(Transform t, Bounds b)
    {
        var rrr = t.GetComponent<Renderer>();
        if (rrr != null)
        {
            return rrr.bounds.Intersects(b);
        }
        for (int i = 0; i < t.childCount; i++)
        {
            if (Intersects(t.GetChild(i), b))
            {
                return true;
            }
        }
        return false;
    }
}
