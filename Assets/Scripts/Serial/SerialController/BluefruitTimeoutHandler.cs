/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Handles all timeouts a Bluefruit device can have.
 *          
 */

using UnityEngine;
using SerialFramework;

public class BluefruitTimeoutHandler {

    public SystemEventDelegate systemEvents;

    int connectionTimeoutTime;
    int connectionTimeoutCount;

    bool useReferenceReset;
    bool enabled;
    int referenceResetTime;

    int i2CTimeoutTime;
    int i2CTimeoutCount;

    public void CheckStateMachine(string comName)
    {
        string stdErrorPrefix = "BluefruitTimeoutHandler of ["+ comName + "] :";

        if(systemEvents == null)
        {
            Debug.LogError(stdErrorPrefix + " no systemEventListener ");
            enabled = false;
            return;
        }

        if(connectionTimeoutTime <= 0)
        {
            Debug.LogError(stdErrorPrefix + " connection timeoutTime invalid = " + connectionTimeoutTime);
            enabled = false;
            return;
        }

        if(useReferenceReset && referenceResetTime <= 0)
        {
            Debug.LogError(stdErrorPrefix + " reference reset time invalid = " + referenceResetTime);
            enabled = false;
            return;
        }

        enabled = true;
    }
	
	// Update is called once per frame
	public void UpdateTimeout (AbstractBluefruitController ctrl) {

        if (!enabled)
            return;

        // check connection timeout
        connectionTimeoutCount = Mathf.Max(connectionTimeoutCount - 1, 0);

        if(connectionTimeoutCount <= 0)
        {
            ConnectionTimeoutRoutine();
        }

        //check reference resetTime
        if (useReferenceReset && Time.frameCount % referenceResetTime == 0)
            ctrl.SetReference();

	}

    public void UpdateI2CTimeout()
    { 
        i2CTimeoutCount++;

        if (i2CTimeoutCount > i2CTimeoutTime)
        {
            AddSystemMessage(SystemMessage.SensorNoResponseAfterI2C);
        }

    }

    private void AddSystemMessage(SystemMessage msg)
    {
        if (systemEvents != null)
        {
            systemEvents(msg);
        }
    }

    void ConnectionTimeoutRoutine()
    {
        AddSystemMessage(SystemMessage.Connection_Lost);
    }

    public void SetConnectionTimeout(int timeout)
    {
        connectionTimeoutTime = timeout;
        ResetConnectionTimeout();
    }

    public void ResetConnectionTimeout()
    {
        connectionTimeoutCount = connectionTimeoutTime;
    }

    public void SetReferenceResetFrameTime(int frametime)
    {
        referenceResetTime = frametime;
        useReferenceReset = true;
    }

    public void SetI2CTimeoutTime(int timeout)
    {
        i2CTimeoutTime = timeout;
        i2CTimeoutCount = 0;
    }

}
