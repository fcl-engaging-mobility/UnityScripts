// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: interface to define what is required of a sun calculator:
//          It should calculate the sun's elevation and azimuth from
//          a given latitude, longitude, date, time, and time-zone

using System;

interface SunCalculator
{
    void GetElevationAzimuth(DateTime local, float latitude, float longitude, float timeZone, ref float elevation, ref float azimuth);
}
