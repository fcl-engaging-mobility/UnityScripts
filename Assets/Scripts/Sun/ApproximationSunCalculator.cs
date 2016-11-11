// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: fast approximation of the sun's elevation/azimuth

using System;
using UnityEngine;
    
public class ApproximationSunCalculator : SunCalculator
{
    const float kAxialTilt = 23.43713f;
    const float kAxialTiltRad = kAxialTilt * Mathf.Deg2Rad;

    const float kDegreesPerHour = 15;
    const float kRadiansPerHour = kDegreesPerHour * Mathf.Deg2Rad;

    const float kRadiansPerDay = Mathf.Deg2Rad * 360f / 365.25f;

    const float kMinute2Hour = 1f / 60f;
    

    // N: day of the year starting with 0 at midnight UT January 1st
    static float calcSunDeclination(float N)
    {
        return -kAxialTiltRad * Mathf.Cos((N + 10f) * kRadiansPerDay);
        // in radians
    }

    public void GetElevationAzimuth(DateTime local, float latitude, float longitude, float timeZone, ref float elevation, ref float azimuth)
    {
        DateTime UT = local.AddHours(-timeZone);
        float N = (float)(UT - new DateTime(UT.Year, 1, 1)).TotalDays -1;
        float sunDeclinationRad = calcSunDeclination(N);

        // Hour Angle
        //float hourAngle = (local.Hour + local.Minute * kMinute2Hour - timeZone - 12f) * kDegreesPerHour + longitude;
        float hourAngle = (UT.Hour + UT.Minute * kMinute2Hour - 12f) * kDegreesPerHour + longitude;
        if (hourAngle > 180.0f)
            hourAngle -= 360;
        else if (hourAngle < -180.0f)
            hourAngle += 360.0f;

        float latitudeRad = latitude * Mathf.Deg2Rad;
        float hourAngleRad = hourAngle * Mathf.Deg2Rad;

        float sinSunDeclination = Mathf.Sin(sunDeclinationRad);
        float sinLatitude = Mathf.Sin(latitudeRad);
        float cosLatitude = Mathf.Cos(latitudeRad);

        // Solar Zenith Angle: angle between the observer's zenith (up vector) and the sun
        float cosSolarZenith = sinLatitude * sinSunDeclination + cosLatitude * Mathf.Cos(sunDeclinationRad) * Mathf.Cos(hourAngleRad);
        cosSolarZenith = Mathf.Clamp(cosSolarZenith, -1, 1);
        float solarZenithRad = Mathf.Acos(cosSolarZenith);

        // Solar Elevation is the complementary angle of Solar Zenith Angle
        elevation = 90 - solarZenithRad * Mathf.Rad2Deg;

        // Solar Azimuth Angle: angle between the north and the sun, in a clockwise rotation
        float azDenom = Mathf.Sin(solarZenithRad) * cosLatitude;
        if (Mathf.Abs(azDenom) > 0.001f)
        {
            float cosAzimuth = (sinSunDeclination - cosSolarZenith * sinLatitude) / azDenom;
            cosAzimuth = Mathf.Clamp(cosAzimuth, -1, 1);

            azimuth = Mathf.Acos(cosAzimuth) * Mathf.Rad2Deg;
            if (hourAngle > 0)
            {
                azimuth = -azimuth;
            }
            if (azimuth < 0)
            {
                azimuth += 360;
            }
        }
        else
        {
            azimuth = latitude > 0 ? 180 : 0;
        }
    }

}
