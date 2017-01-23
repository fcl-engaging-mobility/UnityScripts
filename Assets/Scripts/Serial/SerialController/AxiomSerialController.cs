/**
* Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
* All rights reserved.
* 
* This software may be modified and distributed under the terms
* of the MIT license.See the LICENSE file for details.
* 
* Author:  Filip Schramka (schramka@arch.ethz.ch)
* Summary: This class controlls the data flow between the Elite
*          Axiom Hardware and Unity.
*          
*/

using UnityEngine;
using SerialFramework;

[CreateAssetMenu(menuName = "AxiomSerialController")]
public class AxiomSerialController : SerialController
{ 
    // constants for the data identification
    const int inDataLength = 10;
    const byte firstDataIndicator = 0xF0;
    const byte lastDataIndicator = 0xF7;
    const int inFirstDataPos = 0;
    const int inLastDataPos = 9;

    // id
    public const byte identificationNumber = 32;

    // Latest data package and indicators
    AxiomDataPackage axiomData;
    AxiomSerialInterpreter interpreter;
    float oldTime;

    AxiomSerialController()
    {
        parity = System.IO.Ports.Parity.Even;
        stopBits = System.IO.Ports.StopBits.One;
        dataBits = 8;
        baudRate = 9600;
        axiomData = new AxiomDataPackage();
    }

    public AxiomDataPackage getAxiomData()
    {
        if (!isActive)
            return axiomData;

        if (Time.time == oldTime)
            return axiomData;

        oldTime = Time.time;            

        while(GetSerialIO().GetInputQueueSize() > 0)
        {
            axiomData = (AxiomDataPackage)GetSerialIO().GetNextInput();
        }

        // not enough new data, return the old one
        return axiomData;
    }

    public void RequestResistance(int val)
    {
        byte[] msg = GetMessageHull();
        msg[1] = 0x01;
        msg[2] = (byte)val;
        GetSerialIO().SendBuffer(msg);
    }

    public void RequestSerialNumber()
    {
        byte[] msg = GetMessageHull();
        msg[1] = 0x03;
        GetSerialIO().SendBuffer(msg);
    }

    public void RequestIdentification()
    {
        byte[] msg = GetMessageHull();
        msg[1] = 0x08;
        msg[2] = identificationNumber;
        GetSerialIO().SendBuffer(msg);
    }

    public void RequestProductVersion()
    {
        byte[] msg = GetMessageHull();
        msg[1] = 0x09;
        GetSerialIO().SendBuffer(msg);
    }

    private byte[] GetMessageHull()
    {
        byte[] ret = new byte[6];

        ret[0] = 0xF0;
        ret[5] = 0xF7;

        return ret;
    }

    protected override AbstractSerialInterpreter GetInterpreter()
    {
        return interpreter;
    }

    protected override void OnSystemEvent(SystemMessage msg)
    {
        // ignore messages, axiom is doing what it wants anyway
    }

    protected override void InitInterpreter()
    {
        interpreter = new AxiomSerialInterpreter();
        interpreter.axiomEvents += OnAxiomEvent;
    }

    private void OnAxiomEvent(AxiomEvent aEvent)
    {
        if (aEvent == AxiomEvent.EnterPressed)
            Debug.Log("Axiom Enter button pressed");
        else if (aEvent == AxiomEvent.TabPressed)
            Debug.Log("Axiom Tabulator button pressed");
        else if (aEvent == AxiomEvent.PlusPressed)
            Debug.Log("Axiom Plus button pressed");
        else if (aEvent == AxiomEvent.MinusPressed)
            Debug.Log("Axiom Minus button pressed");
        else
            Debug.LogWarning("Broken AxiomEvent");
    }
}