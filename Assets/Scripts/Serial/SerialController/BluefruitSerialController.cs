/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: This class controlls the data flow between the Adafruit
 *          Feather + Bluefruit EZ-Link Hardware and Unity.
 *          
 */

using UnityEngine;
using DriverFramework;
using SerialFramework;

[CreateAssetMenu(menuName = "BluefruitSerialController")]
public class BluefruitSerialController : AbstractBluefruitController
{
    [Header("General sensor options")]
    [Space(10)]

    [Tooltip("Rotation Axis - find this in SensorTestArea Scene")]
    public RotationAxis sensorAxis = RotationAxis.X;

    [Header("Sensor timeout options")]
    [Space(10)]

    [Tooltip("If activated reference will auto reset")]
    public bool refAutoReset;
    [Tooltip("Resets after amount of frames")]
    public int resetInterval = 500;
    [Tooltip("Connection loss will be detected after this amount of frames without new data")]
    public int connectionTimeout = 20;
    [Tooltip("Declares the time for the I2C timeout")]
    public int i2cTimeoutTime = 500;

    Quaternion q;
    float lastFrameNr;
    byte[] outBuff;

    bool dataIncomming;
    bool first;

    float angle;
    float angleOffset;

    const int bufferSize = 64;
    const char packagePrefix = '#';
    const char packageSuffix = '$';

    bool i2CRoutine;

    BluefruitSerialInterpreter interpreter;
    BluefruitTimeoutHandler timeoutHandler;

    Quaternion reference;
    Quaternion worldToLocal;

    BluefruitSerialController()
    {
        outBuff = new byte[3];
        outBuff[0] = (byte)packagePrefix;
        outBuff[2] = (byte)packageSuffix;
    }

    void OnEnable()
    {
        q = Quaternion.identity;

        dataIncomming = false;
        lastFrameNr = -1;

        reference = Quaternion.identity;
        worldToLocal = Quaternion.Inverse(reference);

        first = true;

        angle = 0;
        angleOffset = 0;

        timeoutHandler = new BluefruitTimeoutHandler();
        timeoutHandler.systemEvents += OnSystemEvent;
        timeoutHandler.SetConnectionTimeout(connectionTimeout);
        if(refAutoReset)
            timeoutHandler.SetReferenceResetFrameTime(resetInterval);

        timeoutHandler.CheckStateMachine(portName);
    }

    protected override void InitInterpreter()
    {
        interpreter = new BluefruitSerialInterpreter(portName);
        interpreter.bluefruitEvents += OnBluefruitEvent;
    }

    public override Quaternion GetQuaternion()
    {
        if (i2CRoutine)
            timeoutHandler.UpdateI2CTimeout();

        if (!isActive || !dataIncomming)
            return q;

        // same frame, return same quaternion
        if (Time.frameCount == lastFrameNr)
            return q;

        lastFrameNr = Time.frameCount;

        while(GetSerialIO().GetInputQueueSize() > 0)
        {
            timeoutHandler.ResetConnectionTimeout();
            q = (Quaternion)GetSerialIO().GetNextInput();
            if (first)
            {
                SetReference();
                first = false;
            }
        }

        timeoutHandler.UpdateTimeout(this);

        return q;
    }

    public override void SetReference()
    {
        angleOffset = angle;
        reference = q;
        worldToLocal = Quaternion.Inverse(reference);
    }

    public override Quaternion GetReference()
    {
        return reference;
    }

    public override Quaternion GetWorldToLocal()
    {
        return worldToLocal;
    }

    public void ResetSensor()
    {
        PackAndSend('r');
    }

    public void ChangeLEDState()
    {
        PackAndSend('b');
    }

    public void RequestSensorData()
    { 
        PackAndSend('c');
    }

    public void StopSensorData()
    {
        PackAndSend('s');
    }

    private void PackAndSend(char c)
    {
        outBuff[1] = (byte)c;
        GetSerialIO().SendBuffer(outBuff);
    }

    protected override AbstractSerialInterpreter GetInterpreter()
    {
        return interpreter;
    }

    public override bool IsSensorReady()
    {
        return dataIncomming;
    }

    private void OnConnectionLost()
    {
        Debug.Log("Connection to " + portName + " lost, try to reconnect...");
        Reconnect();
        dataIncomming = false;
        first = true;
        timeoutHandler.ResetConnectionTimeout();
    }

    protected override void OnSystemEvent(SystemMessage msg)
    {
        switch (msg)
        {
            case SystemMessage.Connection_Up:
                RequestSensorData();
                break;

            case SystemMessage.Connection_Down:
                StopSensorData();
                break;

            case SystemMessage.Connection_Lost:
                OnConnectionLost();
                break;

            case SystemMessage.SensorNoResponseAfterI2C:
                OnI2CError();
                break;

            default:
                Debug.LogWarning("unknown system message");
                break;
        } 
    }

    private void OnI2CError()
    {
        Debug.LogError("[" + portName + "] Sensor did not respond after I2C init. Unplug sensor from power and try it again");
        i2CRoutine = false;
    }

    private void OnBluefruitEvent(BluefruitEvent msg)
    {
        string str = "[" + portName + "] ";

        i2CRoutine = false;

        switch (msg)
        {
            case BluefruitEvent.InitDMP:
                str += "Initializing DMP...";
                break;
            case BluefruitEvent.EnableDMP:
                str += "Enabling DMP...";
                break;
            case BluefruitEvent.InterruptAttachFail:
                str += "Failed interrupt attach! Make sure a adafruit feather is used, if not change the firmware.";
                break;
            case BluefruitEvent.InterruptDetachFail:
                str += "Failed interrupt detach! Make sure a adafruit feather is used, if not change the firmware.";
                break;
            case BluefruitEvent.EnableInterrupt:
                str += "Enabling interrupt for MPU6050 sensor...";
                break;
            case BluefruitEvent.DMPInitFailMemory:
                str += "DMP failed to initialize! Initial memory load failed";
                break;
            case BluefruitEvent.DMPInitFailConfig:
                str += "DMP failed to initialize! DMP configuration updates failed";
                break;
            case BluefruitEvent.InitI2C:
                str += "Initializing I2C connection to MPU6050...";
                i2CRoutine = true;
                timeoutHandler.SetI2CTimeoutTime(i2cTimeoutTime);
                break;
            case BluefruitEvent.MPUCOMFail:
                str += "MPU6050 I2C connection failed! Check the physical connection to the sensor";
                break;
            case BluefruitEvent.MPUCOMSuccess:
                str += "MPU6050 I2C connection successful";
                break;
            case BluefruitEvent.SensorRdy:
                str += "Sensor is ready to provide data";
                RequestSensorData();
                break;
            case BluefruitEvent.SendingData:
                str += "Sensor starts sending data...";
                dataIncomming = true;
                break;
            case BluefruitEvent.StopSendingData:
                str += "Sensor stops sending data...";
                dataIncomming = false;
                break;
            case BluefruitEvent.FifoOverflow:
                str += "MPU6050 fifo buffer overflow! Try to reset buffer...";
                break;
            case BluefruitEvent.InputBufferError:
                str += "Serial input buffer error! Transmitted data may have overflown the 32byte buffer";
                break;
            default:
                str += "Unknown Event? Firmware changed?";
                break;
        }

        Debug.Log(str);
    }

    public override float GetAbsoluteAngle()
    {
        // get rotation quaternion in local space

        Quaternion q = GetQuaternion();

        q = GetWorldToLocal() * q;

        angle = Mathf.Rad2Deg;

        if (sensorAxis == RotationAxis.X)
        {
            Vector3 v = q * Vector3.forward;
            angle *= Mathf.Atan2(v.y, v.z);
        }
        else if (sensorAxis == RotationAxis.Y)
        {
            Vector3 v = q * Vector3.forward;
            angle *= Mathf.Atan2(v.x, v.z);
        }
        else if (sensorAxis == RotationAxis.Z)
        {
            Vector3 v = q * Vector3.up;
            angle *= Mathf.Atan2(v.x, v.y);
        }
        else
        {
            angle = 0.0f;
        }

        angle = MathUtils.NfMod(angleOffset + angle, 360.0f);

        return angle;
    }
}