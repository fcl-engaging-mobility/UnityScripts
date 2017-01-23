/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Mock class of a Bluefruit Device. Usefull for debugging.
 *          
 */

using UnityEngine;
using SerialFramework;
using System;

[CreateAssetMenu(menuName = "BluefruitMockController")]
public class BluefruitMockController : AbstractBluefruitController
{
    Quaternion q;
    Quaternion reference;
    Quaternion worldToLocal;
    float run = 0;
    static Vector3 v = new Vector3(-1, 1, -1).normalized;

    public override void Init()
    {
        // do nothing for init --> no serial thread will be started
    }

    public override void Shutdown()
    {
        // no init, no shutdown
    }

    void OnEnable()
    {
        q = Quaternion.identity;
        reference = Quaternion.AngleAxis(23, v);
        worldToLocal = Quaternion.Inverse(reference);
    }

    public override Quaternion GetQuaternion()
    {
        if (!isActive)
            return q;

        q = Quaternion.AngleAxis(run, v);

        run = MathUtils.NfMod(++run, 360.0f);
        return q;
    }

    public override void SetReference()
    {
        // ignore
    }

    public override Quaternion GetReference()
    {
        return reference;
    }

    public override Quaternion GetWorldToLocal()
    {
        return worldToLocal;
    }

    protected override void OnSystemEvent(SystemMessage msg)
    {
        // ignore
    }

    protected override void InitInterpreter()
    {
        // no need
    }

    protected override AbstractSerialInterpreter GetInterpreter()
    {
        // no need
        return null;
    }

    public override bool IsSensorReady()
    {
        return true;
    }

    public override float GetAbsoluteAngle()
    {
        return run;
    }
}
