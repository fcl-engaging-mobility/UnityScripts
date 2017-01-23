/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: This abstract class controlls the data flow between any
 *          serial COM port. 
 *          
 * Credit:
 *          This piece of software was modified from its original 
 *          form which was written by 
 * 
 *          SerialCommUnity (Serial Communication for Unity)
 *          Author: Daniel Wilches <dwilches@gmail.com>
 *
 *          This work is released under the Creative Commons 
 *          Attributions license.
 *          https://creativecommons.org/licenses/by/2.0/
 * 
 */

using UnityEngine;
using System.Threading;
using SerialFramework;

public abstract class SerialController : ScriptableObject{

    [Header("Serial options")]
    [Space(10)]

    [Tooltip("Port name with which the SerialPort object will be created.")]
    public string portName = "COM?";

    [Tooltip("Baud rate that the serial device is using to transmit data.")]
    public int baudRate = 9600;

    [Tooltip("Parity checks used by the serial bus system.")]
    public System.IO.Ports.Parity parity = System.IO.Ports.Parity.None;

    [Tooltip("Data bits used by the serial bus system.")]
    public int dataBits = 8;

    [Tooltip("Stop bits used by the serial bus system.")]
    public System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.None;

    [Tooltip("After an error in the serial communication, or an unsuccessful " +
             "connect, how many milliseconds we should wait.")]
    public int reconnectionDelay = 1000;

    [Tooltip("Will start a serial connection if active")]
    public bool isActive = true;

    // Internal reference to the Thread and the object that runs in it.
    Thread thread;
    SerialIO serialThread;

    public virtual void Init()
    {
        if (!isActive)
        {
            return;
        }

        InitInterpreter();
        GetInterpreter().systemEvents += OnSystemEvent;

        // if an old one is running shut it down before starting a new connection
        if (serialThread != null)
            Shutdown();

        serialThread = new SerialIO(portName, baudRate, parity, dataBits, stopBits, reconnectionDelay, GetInterpreter());
        thread = new Thread(new ThreadStart(serialThread.RunForever));

        thread.Name = "SerialThread " + portName;
        thread.Start();
    }

    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is deactivated.
    // It stops and destroys the thread that was reading from the serial device.
    // ------------------------------------------------------------------------
    public virtual void Shutdown()
    {
        // The serialThread reference should never be null at this point,
        // unless an Exception happened in the OnEnable(), in which case I've
        // no idea what face Unity will make.
        if (serialThread != null)
        {
            serialThread.RequestStop();
            serialThread = null;
        }

        if (thread != null)
        {
            thread.Join();
            thread = null;
        }
    }

    protected void Reconnect()
    {
        Shutdown();
        Init();
    }

    protected SerialIO GetSerialIO()
    {
        return serialThread;
    }

    public bool IsSerialPortOpen()
    {
        return serialThread.isSerialPortOpen();
    }

    protected abstract void OnSystemEvent(SystemMessage msg);
    protected abstract void InitInterpreter();
    protected abstract AbstractSerialInterpreter GetInterpreter();

}
