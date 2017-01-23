/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Class which opens the serial communication.
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

using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using SerialFramework;

/**
 * This class contains methods that must be run from inside a thread and others
 * that must be invoked from Unity. Both types of methods are clearly marked in
 * the code, although you, the final user of this library, don't need to even
 * open this file unless you are introducing incompatibilities for upcoming
 * versions.
 */
public class SerialIO
{
    // Parameters passed from SerialController, used for connecting to the
    // serial device as explained in the SerialController documentation.
    private string portName;
    private int baudRate;
    private Parity paritySetting;
    private int dataBitCount;
    private StopBits stopBitSetting;
    private int delayBeforeReconnecting;

    // received data will be read into this buffer
    private byte[] rawSerialBuffer;
    private const int serialBufferSize = 256;
    private Queue<byte> rawQueue;

    // Object from the .Net framework used to communicate with serial devices.
    private SerialPort serialPort;

    // Amount of milliseconds alloted to a single read or connect. An
    // exception is thrown when such operations take more than this time
    // to complete.
    private const int readTimeout = 100;

    // Amount of milliseconds alloted to a single write. An exception is thrown
    // when such operations take more than this time to complete.
    private const int writeTimeout = 100;

    // Internal synchronized queues used to send and receive messages from the
    // serial device. They serve as the point of communication between the
    // Unity thread and the SerialComm thread.
    private Queue inputQueue, outputQueue;

    // Indicates when this thread should stop executing. When SerialController
    // invokes 'RequestStop()' this variable is set.
    private bool stopRequested = false;

    // interprets the data before enqueue them into the outputqueue
    private AbstractSerialInterpreter interpreter;

    //- TODO remove
    // signal counter variables
    //private const int maxHashCount = 5;
    //private int hashCount = 0;
    //private int hashTimeout = readTimeout;
    //private bool hashTimeoutTrigger = false;

    /**************************************************************************
     * Methods intended to be invoked from the Unity thread.
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Constructs the thread object. This object is not a thread actually, but
    // its method 'RunForever' can later be used to create a real Thread.
    // ------------------------------------------------------------------------
    public SerialIO(string portName, int baudRate, Parity paritySetting, int dataBitCount,
                        StopBits stopBitSetting, int delayBeforeReconnecting, AbstractSerialInterpreter interpreter)
    {
        Debug.Assert(interpreter != null);

        this.portName = portName;
        this.baudRate = baudRate;
        this.paritySetting = paritySetting;
        this.dataBitCount = dataBitCount;
        this.stopBitSetting = stopBitSetting;
        this.delayBeforeReconnecting = delayBeforeReconnecting;
        this.interpreter = interpreter;

        inputQueue = Queue.Synchronized(new Queue());
        outputQueue = Queue.Synchronized(new Queue());

        rawSerialBuffer = new byte[serialBufferSize];
        rawQueue = new Queue<byte>();
    }

    // ------------------------------------------------------------------------
    // Returns the count of the bytes in the input queue
    // ------------------------------------------------------------------------
    public int GetInputQueueSize()
    {
        return inputQueue.Count;
    }

    // ------------------------------------------------------------------------
    // Returns the count of byte[] in the output queue
    // ------------------------------------------------------------------------
    public int GetOutputQueueSize()
    {
        return outputQueue.Count;
    }

    // ------------------------------------------------------------------------
    // Dequeues the next value
    // ------------------------------------------------------------------------
    public object GetNextInput()
    {
        return inputQueue.Dequeue();
    }

    // ------------------------------------------------------------------------
    // Peeks into the queue
    // ------------------------------------------------------------------------
    public object checkNextInput()
    {
        return inputQueue.Peek();
    }


    // ------------------------------------------------------------------------
    // Enqueues a character for sending
    // ------------------------------------------------------------------------
    public void SendCharacter(char c)
    {
        outputQueue.Enqueue(new byte[] { (byte)c });
    }

    // ------------------------------------------------------------------------
    // Enqueues the whole buffer for sending
    // ------------------------------------------------------------------------
    public void SendBuffer(byte[] b)
    {
        outputQueue.Enqueue(b);
    }

    // ------------------------------------------------------------------------
    // Invoked to indicate to this thread object that it should stop.
    // ------------------------------------------------------------------------
    public void RequestStop()
    {
        interpreter.AddSystemMessage(SystemMessage.Connection_Down);

        lock (this)
        {
            stopRequested = true;
        }
    }

    /**************************************************************************
     * Methods intended to be invoked from the SerialComm thread (the one
     * created by the SerialController).
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Enters an almost infinite loop of attempting conenction to the serial
    // device, reading messages and sending messages. This loop can be stopped
    // by invoking 'RequestStop'.
    // ------------------------------------------------------------------------
    public void RunForever()
    {
        // This try is for having a log message in case of an unexpected
        // exception.
        try
        {
            while (!IsStopRequested())
            {
                try
                {
                    // Try to connect
                    AttemptConnection();

                    // Enter the semi-infinite loop of reading/writing to the
                    // device.
                    while (!IsStopRequested())
                        RunOnce();

                    // send the finish commands to the device
                    Thread.Sleep(100);
                    RunOnce();
                }
                catch (Exception ioe)
                {
                    // request stop if port doesnt exist
                    if (ioe.Message.Contains(portName + "' does not exist"))
                    {
                        RequestStop();
                        Debug.Log(portName + " does not exist");
                    }
                    else if (ioe.Message.Contains("is not connected"))
                    {
                        Debug.Log("Device on " + portName + " is not connected");
                    }
                    else
                        Debug.LogWarning("Exception: " + ioe.Message + " StackTrace: " + ioe.StackTrace);

                    Debug.Log("Dissconnected from " + portName);

                    // As I don't know in which stage the SerialPort threw the
                    // exception I call this method that is very safe in
                    // disregard of the port's status
                    CloseDevice();

                    // Don't attempt to reconnect just yet, wait some
                    // user-defined time. It is OK to sleep here as this is not
                    // Unity's thread, this doesn't affect frame-rate
                    // throughput.
                    Thread.Sleep(delayBeforeReconnecting);
                }
            }

            // Attempt to do a final cleanup. This method doesn't fail even if
            // the port is in an invalid status.
            CloseDevice();
        }
        catch (Exception e)
        {
            Debug.LogError("Unknown exception: " + e.Message + " " + e.StackTrace);
        }
    }

    // ------------------------------------------------------------------------
    // Try to connect to the serial device. May throw IO exceptions.
    // ------------------------------------------------------------------------
    private void AttemptConnection()
    {
        serialPort = new SerialPort(portName, baudRate, paritySetting, dataBitCount, stopBitSetting);
        serialPort.ReadTimeout = readTimeout;
        serialPort.WriteTimeout = writeTimeout;
        serialPort.Open();

        Debug.Log("Connected to Port " + portName);

        interpreter.AddSystemMessage(SystemMessage.Connection_Up);
    }

    public bool isSerialPortOpen()
    {
        return serialPort != null && serialPort.IsOpen;
    }

    // ------------------------------------------------------------------------
    // Release any resource used, and don't fail in the attempt.
    // ------------------------------------------------------------------------
    private void CloseDevice()
    {
        if (serialPort == null)
            return;

        try
        {
            serialPort.Close();
        }
        catch (IOException)
        {
            // Nothing to do, not a big deal, don't try to cleanup any further.
        }

        serialPort = null;
    }

    // ------------------------------------------------------------------------
    // Just checks if 'RequestStop()' has already been called in this object.
    // ------------------------------------------------------------------------
    private bool IsStopRequested()
    {
        lock (this)
        {
            return stopRequested;
        }
    }

    // ------------------------------------------------------------------------
    // A single iteration of the semi-infinite loop. Attempt to read/write to
    // the serial device. If there are more lines in the queue than we may have
    // at a given time, then the newly read lines will be discarded. This is a
    // protection mechanism when the port is faster than the Unity progeram.
    // If not, we may run out of memory if the queue really fills.
    // ------------------------------------------------------------------------
    private void RunOnce()
    {
        try
        {
            // Send a byte[]
            if (outputQueue.Count != 0)
            {
                byte[] message = (byte[])outputQueue.Dequeue();
                serialPort.Write(message, 0, message.Length);
            }

            // receive and interprete data
            int l = serialPort.Read(rawSerialBuffer, 0, serialBufferSize);

            for (int i = 0; i < l; ++i)
                rawQueue.Enqueue(rawSerialBuffer[i]);

            interpreter.InterpreteAndAdd(rawQueue, inputQueue);
        }
        catch (TimeoutException)
        {
            // This is normal, not everytime we have a report from the serial device
        }
    }

}
