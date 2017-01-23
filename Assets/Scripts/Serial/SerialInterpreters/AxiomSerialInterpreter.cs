/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Extracts data out of the axiom serial stream
 *          
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerialFramework;

public class AxiomSerialInterpreter : AbstractSerialInterpreter
{
    public delegate void AxiomEventDelegate(AxiomEvent aEvent);
    public AxiomEventDelegate axiomEvents;

    const int minDataLength = 10;
    const byte firstDataIndicator = 0xF0;
    const byte lastDataIndicator = 0xF7;
    const int inFirstDataPos = 0;
    const int inLastDataPos = 9;

    int rollerTurnTime;
    int pedalTurnTime;
    int heartRate;

    public override void InterpreteAndAdd(Queue<byte> rawQueue, Queue queue)
    {
        byte tmp;

        while (rawQueue.Count > 0)
        {
            tmp = rawQueue.Peek();

            if (tmp == firstDataIndicator)
            {
                if (rawQueue.Count < minDataLength)
                    return;

                // indicator not needed
                rawQueue.Dequeue();

                switch (rawQueue.Peek())
                {
                    case 0x01:
                        rawQueue.Dequeue();

                        ExtractBooleanEvents(rawQueue.Dequeue());
                        rollerTurnTime = ((rawQueue.Dequeue() << 8) + rawQueue.Dequeue());
                        pedalTurnTime = (rawQueue.Dequeue() << 8) + rawQueue.Dequeue();
                        heartRate = (rawQueue.Dequeue() << 8) + rawQueue.Dequeue();

                        if (rawQueue.Peek() != lastDataIndicator)
                        {
                            Debug.Log("Broken axiom message ");
                            continue;
                        }

                        rawQueue.Dequeue();
                        queue.Enqueue(new AxiomDataPackage(rollerTurnTime, pedalTurnTime, heartRate));

                        break;

                    case 0x03:
                        // Serial number, not needed, not implemented
                        Debug.Log("Axiom serial number package");
                        break;

                    case 0x08:
                        rawQueue.Dequeue();

                        string ans = "Axiom version ";
                        switch (rawQueue.Dequeue())
                        {
                            case AxiomSerialController.identificationNumber * 2:
                                ans += "4";
                                break;
                            case AxiomSerialController.identificationNumber * 2 + 5:
                                ans += "4.5";
                                break;
                            case AxiomSerialController.identificationNumber * 2 + 8:
                                ans += "6";
                                break;
                            default:
                                ans += "Unknown";
                                break;
                        }
                        ans += " identified";

                        SkipUnusedMessage(rawQueue, 7);

                        Debug.Log(ans);
                        break;

                    case 0x09:
                        Debug.Log("Axiom product version package");
                        // Product version, not needed, not implemented
                        break;

                    case firstDataIndicator:
                        rawQueue.Enqueue(tmp);
                        break;
                    default:
                        Debug.Log("Broken data throw away: " + rawQueue.Dequeue());
                        break;
                }
            }
            else
            {
                // throw away invalid data
                Debug.Log("Throw away " + rawQueue.Dequeue());
            }
        }
    }

    private void SkipUnusedMessage(Queue<byte> raw, int steps)
    {
        for(int i = 0; i < steps; ++i)
        {
            raw.Dequeue();
        }
    }

    private void ExtractBooleanEvents(byte b)
    {
        if ((b & 0x01) != 0)
            CreateEvent(AxiomEvent.PlusPressed);

        if ((b & 0x02) != 0)
            CreateEvent(AxiomEvent.MinusPressed);

        if ((b & 0x04) != 0)
            CreateEvent(AxiomEvent.EnterPressed);

        if ((b & 0x08) != 0)
            CreateEvent(AxiomEvent.TabPressed);
    }

    private void CreateEvent(AxiomEvent aEvent)
    {
        if (axiomEvents != null)
            axiomEvents(aEvent);
    }

}
