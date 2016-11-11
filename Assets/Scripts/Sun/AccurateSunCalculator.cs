// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: accurate approximation of the sun's elevation/azimuth

using System;

// Notes:
//
// Axial Tilt: angle between Earth's spinning axis and the orbit plane's normal = 23.43713 degrees;
//
// Terrestrial:     Celestial:
// Latitude         Declination 
// Longitude        Hour Angle / Right Ascension
//
// Hour Angle: angle from observer to sun along the celestial equator measured westwards
// Longitude is measured eastwards
// Hour Angle is measured westwards
// Local Hour Angle (LHA): when measured from the observer meridian
// Greenwich Hour Angle (GHA): when measured from the Greenwich meridian
// Because of the Earth rotation the HA is only valid at a specific time
//
// Ecliptic: plane of Earth's orbit around the sun
// Perihelion: point in the orbit of a planet at which it is closest to the sun (4th Jan)
// Aphelion: point in the orbit of a planet at which it is furthest from the sun (3rd July)
// Vernal Equinox (spring): 20 March (in the northern hemisphere)
// Autumnal Equinox: 23 Sept (in the northern hemisphere)
//
// NEP/SEP (North/South Ecliptic Pole): intersection point between the celestial sphere and Earth's orbit plane
// NCP/SCP (North/South Celestial Pole): intersection point between the celestial sphere and Earth's spinning axis
//
// Celestial Equator: Earth's equator projected on the celestial sphere
//
// Gregorian 2000 Jan 1 12.00 <--> Julian Day Time (JDT) 2451545.0
//
// Julian Day Number (JDN): integer representing a whole solar day starting from noon at GMT
// Julian Date (JD): 

public class AccurateSunCalculator : SunCalculator
{
    const double Deg2Rad = 0.01745329251994329576923690768489;
    const double Rad2Deg = 57.295779513082320876798154814105;

    public void GetElevationAzimuth(DateTime local, float latitude, float longitude, float timeZone, ref float elevation, ref float azimuth)
    {
        double jday = JulianDay(local);
        double localTimeInMinutes = local.Hour * 60.0 + local.Minute + local.Second / 60.0;
        double total = jday + localTimeInMinutes / 1440.0 - timeZone / 24.0;
        double T = JulianCenturies(total);

        double obliquityCorrection = ObliquityCorrection(T);
        double geomMeanLong = GeometricMeanLongitudeSun(T);
        double geomMeanAnomalyRad = GeometricMeanAnomalySun(T) * Deg2Rad;
        double eqTime = EquationOfTime(T, geomMeanLong * Deg2Rad, geomMeanAnomalyRad, obliquityCorrection);
        double sunDeclinationRad = SunDeclinationRad(T, geomMeanLong, geomMeanAnomalyRad, obliquityCorrection);

        double solarTimeFix = eqTime + 4.0 * longitude - 60.0 * timeZone;

        double trueSolarTime = (localTimeInMinutes + solarTimeFix) % 1440;

        double hourAngle = trueSolarTime * 0.25 - 180.0;
        if (hourAngle < -180.0)
        {
            hourAngle += 360.0;
        }

        double latitudeRad = latitude * Deg2Rad;
        double haRad = hourAngle * Deg2Rad;
        double csz = Math.Sin(latitudeRad) * Math.Sin(sunDeclinationRad) + Math.Cos(latitudeRad) * Math.Cos(sunDeclinationRad) * Math.Cos(haRad);
        csz = Math.Min(Math.Max(csz, -1.0), 1.0);

        double zenith = Math.Acos(csz) * Rad2Deg;
        double azDenom = Math.Cos(latitudeRad) * Math.Sin(zenith * Deg2Rad);
        if (Math.Abs(azDenom) > 0.001)
        {
            double azRad = ((Math.Sin(latitudeRad) * Math.Cos(zenith * Deg2Rad)) - Math.Sin(sunDeclinationRad)) / azDenom;
            if (Math.Abs(azRad) > 1.0)
            {
                if (azRad < 0.0)
                {
                    azRad = -1.0;
                }
                else
                {
                    azRad = 1.0;
                }
            }

            azimuth = (float)(180.0 - Math.Acos(azRad) * Rad2Deg);

            if (hourAngle > 0)
            {
                azimuth = -azimuth;
            }
        }
        else
        {
            azimuth = latitude > 0 ? 180 : 0;
        }

        if (azimuth < 0)
        {
            azimuth += 360;
        }

        elevation = (float)atmosphericRefractionCorrection(90 - zenith);
    }

    static double JulianDay(DateTime dt)
    {
        if (dt.Month <= 2)
        {
            dt.AddYears(-1);
            dt.AddMonths(12);
        }
        double A = Math.Floor(dt.Year * 0.01);
        double B = 2 - A + Math.Floor(A * 0.25);
        return Math.Floor(365.25 * (dt.Year + 4716)) + Math.Floor(30.6001 * (dt.Month + 1)) + dt.Day + B - 1524.5;
    }

    // Julian Centuries from 2000
    static double JulianCenturies(double julianDay)
    {
        return (julianDay - 2451545.0) / 36525.0;
    }

    static double MeanObliquityOfEcliptic(double T)
    {
        const double k = 23 + 26.0 / 60.0;
        double seconds = 21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813));
        return k + seconds / 3600.0;
        // in degrees
    }

    static double ObliquityCorrection(double T)
    {
        double e0 = MeanObliquityOfEcliptic(T);
        double omega = 125.04 - 1934.136 * T;
        return e0 + 0.00256 * Math.Cos(omega * Deg2Rad);
        // in degrees
    }

    static double GeometricMeanLongitudeSun(double T)
    {
        // double L0 = 280.46645 + T * (36000.76983 + T * 0.0003032);
        double L0 = 280.46646 + T * (36000.76983 + T * 0.0003032);

        L0 = L0 % 360.0;
        while (L0 < 0.0)
            L0 += 360.0;

        return L0;
        // in degrees
    }

    static double GeometricMeanAnomalySun(double T)
    {
        // return 357.5291 + T * (35999.0503 - T * (0.0001559 +  0.00000048 * T);
        return 357.52911 + T * (35999.05029 - 0.0001537 * T);
        // in degrees
    }

    static double EccentricityEarthOrbit(double T)
    {
        // return 0.016708617 - T * (0.000042037 + T * 0.0000001236);
        return 0.016708634 - T * (0.000042037 + T * 0.0000001267);
    }
    
    static double EquationOfTime(double T, double geomMeanLongRad, double geomMeanAnomalyRad, double obliquityCorrectionRad)
    {
        double e = EccentricityEarthOrbit(T);

        double y = Math.Tan(obliquityCorrectionRad * 0.5);
        y *= y;

        double sinm = Math.Sin(geomMeanAnomalyRad);
        double sin2m = Math.Sin(2.0 * geomMeanAnomalyRad);
        double cos2l0 = Math.Cos(2.0 * geomMeanLongRad);
        double sin2l0 = Math.Sin(2.0 * geomMeanLongRad);
        double sin4l0 = Math.Sin(4.0 * geomMeanLongRad);

        double eot = y * sin2l0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2l0 - 0.5 * y * y * sin4l0 - 1.25 * e * e * sin2m;
        return 4.0 * eot * Rad2Deg;
        // minutes
    }

    static double SunEqOfCenter(double T, double geomMeanAnomalyRad)
    {
        double sinM = Math.Sin(geomMeanAnomalyRad);
        double sin2M = Math.Sin(2.0 * geomMeanAnomalyRad);
        double sin3M = Math.Sin(3.0 * geomMeanAnomalyRad);
        return sinM * (1.914602 - T * (0.004817 + T * 0.000014)) + sin2M * (0.019993 - 0.000101 * T) + sin3M * 0.000289;
        // in degrees
    }

    static double SunTrueLongitude(double T, double geomMeanLong, double geomMeanAnomalyRad)
    {
        double C = SunEqOfCenter(T, geomMeanAnomalyRad);
        double o = (geomMeanLong + C) % 360.0;
        while (o < 0.0) o += 360.0;
        return o;
        // in degrees
    }

    static double SunApparentLongitude(double T, double geomMeanLong, double geomMeanAnomalyRad)
    {
        double o = SunTrueLongitude(T, geomMeanLong, geomMeanAnomalyRad);
        double omega = 125.04 - 1934.136 * T;
        return o - 0.00569 - 0.00478 * Math.Sin(omega * Deg2Rad);
        // in degrees
    }

    static double SunDeclinationRad(double T, double geomMeanLong, double geomMeanAnomalyRad, double obliquityCorrection)
    {
        double lambda = SunApparentLongitude(T, geomMeanLong, geomMeanAnomalyRad);

        double sint = Math.Sin(obliquityCorrection * Deg2Rad) * Math.Sin(lambda * Deg2Rad);
        return Math.Asin(sint);
        // in radians
    }

    static double atmosphericRefractionCorrection(double elevation)
    {
        double refractionCorrection = 0;
        if (elevation <= 85.0)
        {
            double te = Math.Tan(elevation * Deg2Rad);
            if (elevation > 5)
            {
                refractionCorrection = 58.1 / te - 0.07 / Math.Pow(te, 3) + 0.000086 / Math.Pow(te, 5);
            }
            else if (elevation > -0.575)
            {
                refractionCorrection = 1735 + elevation * (-518.2 + elevation * (103.4 + elevation * (-12.79 + elevation * 0.711)));
            }
            else
            {
                refractionCorrection = -20.774 / te;
            }
            refractionCorrection /= 3600.0;
        }
        return elevation + refractionCorrection;
    }

}
