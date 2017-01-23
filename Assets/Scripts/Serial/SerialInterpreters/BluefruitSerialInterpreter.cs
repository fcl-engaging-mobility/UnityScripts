/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Interpretes the bytestream from a bluefruit sensor device.
 *          
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SerialFramework;

public class BluefruitSerialInterpreter : AbstractSerialInterpreter
{
    public delegate void BluefruitEventDelegate(BluefruitEvent bfEvent);
    public BluefruitEventDelegate bluefruitEvents;

    const char eventPrefix = '#';
    const char eventSuffix = '$';
    const char dataPrefix = '{';
    const char dataSuffix = '}';
    const int minEventMessageSize = 3;
    const int minSensorDataSize = 18;
    const int floatsInQuat = 4;

    static readonly Dictionary<byte, BluefruitEvent> translator = new Dictionary<byte, BluefruitEvent> {
        { (byte)'0', BluefruitEvent.InitDMP },
        { (byte)'1', BluefruitEvent.InitI2C },
        { (byte)'2', BluefruitEvent.EnableDMP },
        { (byte)'3', BluefruitEvent.EnableInterrupt },
        { (byte)'a', BluefruitEvent.InterruptAttachFail },
        { (byte)'b', BluefruitEvent.InterruptDetachFail },
        { (byte)'c', BluefruitEvent.DMPInitFailMemory },
        { (byte)'d', BluefruitEvent.DMPInitFailConfig },
        { (byte)'e', BluefruitEvent.DMPInitFailUnknown },
        { (byte)'f', BluefruitEvent.MPUCOMSuccess },
        { (byte)'g', BluefruitEvent.MPUCOMFail },
        { (byte)'h', BluefruitEvent.FifoOverflow },
        { (byte)'i', BluefruitEvent.InputBufferError },
        { (byte)'j', BluefruitEvent.SensorRdy },
        { (byte)'k', BluefruitEvent.SendingData },
        { (byte)'l', BluefruitEvent.StopSendingData }
    };

    string portName;

    public BluefruitSerialInterpreter(string portName)
    {
        this.portName = portName;
    }

    public override void InterpreteAndAdd(Queue<byte> rawQueue, Queue queue)
    {
        while (rawQueue.Count > 0)
        {
            byte preview = rawQueue.Peek();

            if (preview == (byte)eventPrefix)
            {
                // event extraction
                if (rawQueue.Count < minEventMessageSize)
                    return;

                rawQueue.Dequeue();
                BluefruitEvent msg;
                if (!(translator.TryGetValue(rawQueue.Dequeue(), out msg)) || rawQueue.Peek() != eventSuffix)
                {
                    Debug.Log("Corrupt keyword [" + portName + "]");
                    continue;
                }

                rawQueue.Dequeue();

                if (bluefruitEvents != null)
                    bluefruitEvents(msg);

            }
            else if (preview == (byte)dataPrefix)
            {
                // sensorData extraction
                if (rawQueue.Count < minSensorDataSize)
                    return;

                rawQueue.Dequeue();
                float[] quat = new float[floatsInQuat];
                for (int i = 0; i < floatsInQuat; ++i)
                {
                    byte[] floatArr = new byte[sizeof(float)];
                    for (int j = 0; j < sizeof(float); ++j)
                    {
                        floatArr[j] = rawQueue.Dequeue();
                    }
                    quat[i] = System.BitConverter.ToSingle(floatArr, 0);
                }

                if (rawQueue.Peek() != dataSuffix)
                {
                    Debug.Log("Broken float [" + portName + "]");
                    continue;
                }

                rawQueue.Dequeue();
                queue.Enqueue(new Quaternion(quat[0], quat[1], quat[2], quat[3]));
            }
            else
            {
                // trash
                Debug.Log("Throw away data: " + rawQueue.Dequeue() +  " [" + portName + "]");
            }
        }
    }
}
