/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: 
 *          
 */

using UnityEngine;
using System.Collections;

public class SensorResponseTest : MonoBehaviour {

    [Tooltip("The bluefruit controller needed for data")]
    public BluefruitSerialController bfController;

    bool first;
    Quaternion reference;

    void OnEnable()
    {
        bfController.Init();

        first = true;
    }

    void OnDisable()
    {
        bfController.Shutdown();
    }

    // Update is called once per frame
    void Update()
    {
        // test first quaternion
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Start");
            GetComponent<Renderer>().material.color = new Color(0, 255, 0);
            bfController.ChangeLEDState();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            bfController.StopSensorData();
        }

        if (!bfController.IsSensorReady())
        {
            return;
        }

        if (first)
        {
            //bfController.StopSensorData();
            reference = bfController.GetQuaternion();
            first = false;
        }

        if (bfController.GetQuaternion() != reference)
        {
            GetComponent<Renderer>().material.color = new Color(255, 0, 0);
        }

    }
}
