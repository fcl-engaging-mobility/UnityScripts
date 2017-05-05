/**
* Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
* All rights reserved.
* 
* This software may be modified and distributed under the terms
* of the MIT license.See the LICENSE file for details.
* 
* Author:  Filip Schramka (schramka@arch.ethz.ch)
* Summary: Lets the raw data through. Uses generic packages which are just byte wrappers.
*          
*/
using System.Collections;
using System.Collections.Generic;

public class LetThroughInterpreter : AbstractSerialInterpreter {
    
    public override void InterpreteAndAdd(Queue<byte> rawQueue, Queue queue)
    {
        while (rawQueue.Count > 0)
            queue.Enqueue(new GenericDataPackage(rawQueue.Dequeue()));
    }
}
