/**
* Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
* All rights reserved.
* 
* This software may be modified and distributed under the terms
* of the MIT license.See the LICENSE file for details.
* 
* Author:  Filip Schramka (schramka@arch.ethz.ch)
* Summary: Changes the transform of a gameobject due to the sensor rotations.
*          
*/

using UnityEngine;

public class VisualSensor : MonoBehaviour
{
    [Tooltip("The bluefruit controller needed for data")]
    public AbstractBluefruitController bfController;

    float angle;

    IGameProfiler profiler;

    void OnEnable()
    {
        bfController.Init();

        if (bfController.isActive)
        {
            profiler = FindObjectOfType<GameProfiler>();
            profiler.Add(bfController.GetName());
            var set = profiler.GetSettings(bfController.GetName());
            set.min = 0;
            set.max = 360;
        }

        angle = 0f;
    }

    void OnDisable()
    {
        bfController.Shutdown();
    }

    // Update is called once per frame
    void Update()
    {
        if (!bfController.IsSensorReady())
        {
            return;
        }

        Quaternion q = bfController.GetWorldToLocal() * bfController.GetQuaternion();

        angle = bfController.GetAbsoluteAngle();

        transform.localRotation = q;

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Manual reset reference");
            bfController.SetReference();
        }
    }

    void LateUpdate()
    {
        if(bfController.isActive)
            profiler.Update(bfController.GetName(), angle);
    }
}
