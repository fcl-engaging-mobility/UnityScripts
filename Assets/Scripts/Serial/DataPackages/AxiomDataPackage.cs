/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: Wrapper for the incomming serial data, extracts and
 *          converts it into readable format.
 * 
 */

public class AxiomDataPackage {

    // time multiplicator for the turning value
    const float turningPeriodMs = 0.0264f;
    // covered distance in m with one axiom period 
    const float turningDistance = 0.126f;
    // maximal returned value from the axiom
    const int maxVal = ((2 << 15) - 1);
    // maximal time which the axiom can have in ms, this indicates no movement
    const float maxTurningTime = maxVal * turningPeriodMs;

    // input times
    public float rollerTurnTime = 0;
    public int pedalTurnTime = 0;
    public int heartRate = 0;

    public AxiomDataPackage()
    {
        // all maxValues indicate no movement
        rollerTurnTime = maxTurningTime;
        pedalTurnTime = maxVal;
        heartRate = maxVal;
    }

    public AxiomDataPackage(int roller, int pedal, int heart)
    {
        rollerTurnTime = roller * turningPeriodMs;
        pedalTurnTime = pedal;
        heartRate = heart;
    }

    public float GetSpeedInMPerS()
    {
        return IsRollerTurning() ? turningDistance / (rollerTurnTime / 1000.0f) : 0;
    }

    public float GetSpeedInKmPerH()
    {
        return GetSpeedInMPerS() * 3.6f;
    }

    public bool IsHeartRateAvailable()
    {
        return heartRate != maxVal;
    }

    public bool IsPedalTurning()
    {
        return pedalTurnTime != maxVal;
    }

    public bool IsRollerTurning()
    {
        return rollerTurnTime < maxTurningTime && rollerTurnTime > float.Epsilon;
    }
}
