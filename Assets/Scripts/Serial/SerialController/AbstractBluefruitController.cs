/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Abstract class for Bluefruit sensor controllers
 *          
 */

using UnityEngine;

public abstract class AbstractBluefruitController : SerialController
{
    public abstract Quaternion GetQuaternion();
    public abstract void SetReference();
    public abstract Quaternion GetReference();
    public abstract Quaternion GetWorldToLocal();
    public abstract bool IsSensorReady();
    public abstract float GetAbsoluteAngle();
    public string GetName()
    {
        return name;
    }
}

