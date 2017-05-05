/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Simple Data Listener which shows the data 
 *          in a unity text component.
 *         
 */

using UnityEngine;
using UnityEngine.UI;

public class SimpleInfoListener : MonoBehaviour
{
    public AxiomSerialController ctrl;

    Text txt;
    AxiomDataPackage data;

    void OnEnable()
    {
        ctrl.Init();
        txt = gameObject.GetComponent<Text>();
    }

    void OnDisable()
    {
        ctrl.Shutdown();
    }

    void Update()
    {
        data = ctrl.getAxiomData();

        txt.text = " PedalTurnTime: " + (data.IsPedalTurning() ? "" + data.pedalTurnTime : "No Turn") + 
            "\n WheelTurnTime: " + (data.IsRollerTurning() ? "" + data.rollerTurnTime : "No Turn") + 
            "\n HeartFrequency: " + data.heartRate;
    }
}
