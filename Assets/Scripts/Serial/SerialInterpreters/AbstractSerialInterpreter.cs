/**
* Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
* All rights reserved.
* 
* This software may be modified and distributed under the terms
* of the MIT license.See the LICENSE file for details.
* 
* Author:  Filip Schramka (schramka@arch.ethz.ch)
* Summary: Abstract base class for all SerialInterpreters. 
*          
*/
using System.Collections;
using System.Collections.Generic;
using SerialFramework;

public abstract class AbstractSerialInterpreter
{
    public SystemEventDelegate systemEvents;

    public void AddSystemMessage(SystemMessage msg)
    {
        if(systemEvents != null)
        {
            systemEvents(msg);
        }
    }

    public abstract void InterpreteAndAdd(Queue<byte> rawQueue, Queue queue);

}
