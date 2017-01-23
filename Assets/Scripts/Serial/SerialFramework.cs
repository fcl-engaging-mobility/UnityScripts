/**
* Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
* All rights reserved.
* 
* This software may be modified and distributed under the terms
* of the MIT license.See the LICENSE file for details.
* 
* Author:  Filip Schramka (schramka@arch.ethz.ch)
* Summary: Has all global declarations for the serial components.
*          
*/

namespace SerialFramework
{
    public enum SystemMessage
    {
        Connection_Up, Connection_Down, Connection_Lost, SensorNoResponseAfterI2C
    }

    public enum BluefruitEvent
    {
        InitDMP,
        InitI2C,
        EnableDMP,
        EnableInterrupt,
        InterruptAttachFail,
        InterruptDetachFail,
        DMPInitFailMemory,
        DMPInitFailConfig,
        DMPInitFailUnknown,
        MPUCOMFail,
        FifoOverflow,
        InputBufferError,
        MPUCOMSuccess,
        SensorRdy,
        SendingData,
        StopSendingData
    }

    public enum AxiomEvent
    {
        PlusPressed,
        MinusPressed,
        EnterPressed,
        TabPressed,
    }

    public delegate void SystemEventDelegate(SystemMessage msg);

}
