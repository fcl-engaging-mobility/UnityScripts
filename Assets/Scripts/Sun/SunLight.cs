// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: will set the game-object's position and rotation according to
//          the SunCalculator's outcoming elevation and azimuth to simulate
//          the sun on a given location and date/time

using UnityEngine;
using System;

public class SunLight : MonoBehaviour
{
    public enum Method
    {
        Approximation,
        Accurate
    }

    [Header("Location")]
    public float latitude = 0;
    public float longitude = 0;

    [Header("Date")]
    public int year = 2016;
    public int month = 9;
    public int day = 16;
    [Range(1, 365)]
    public int dayOfTheYear = 250;

    [Header("Time")]
    public float timeZone = 8;
    [Range(0, 23)]
    public int hour = 12;
    [Range(0, 59)]
    public int minute = 00;
    [Range(0f, 24f)]
    public float timeOfTheDay = 12.0f;

    [Header("Sun")]
    public float domeRadius = 0;
    public Method method = Method.Accurate;
    [ReadOnly]
    public float elevation;
    [ReadOnly]
    public float azimuth;

    [Header("Trajectory")]
    public bool showDayTrajectory = false;
    public int startHour = 8;
    public int endHour = 18;
    [Range(1, 6)]
    public int resolution = 2;
    public bool showHourDivisions = false;
    public Material material;

    private float[] dayElevationAzimuth = new float[0];

    private int _DayOfTheYear;
    private float _TimeOfTheDay;
    private Method _method;

    private SunCalculator sunCalculator;

    void OnValidate()
    {
        latitude = Math.Min(Math.Max(latitude, -90.0f), 90.0f);
        longitude = Math.Min(Math.Max(longitude, -180.0f), 180.0f);

        year = Mathf.Clamp(year, 1, 9999);
        month = (month - 1) % 12 + 1;
        day = Mathf.Clamp(day, 1, DateTime.DaysInMonth(year, month));

        dayOfTheYear = (dayOfTheYear - 1) % (DateTime.IsLeapYear(year) ? 366 : 365) + 1;
        if (dayOfTheYear != _DayOfTheYear)
        {
            int d = dayOfTheYear;
            for (int i = 1; i <= 12; i++)
            {
                int days = DateTime.DaysInMonth(year, i);
                if (d <= days)
                {
                    day = d;
                    month = i;
                    break;
                }
                d -= days;
            }
        }
        else
        {
            dayOfTheYear = new DateTime(year, month, day).DayOfYear;
        }
        _DayOfTheYear = dayOfTheYear;

        timeOfTheDay = timeOfTheDay % 24f;
        if (timeOfTheDay != _TimeOfTheDay)
        {
            hour = Mathf.FloorToInt(timeOfTheDay);
            minute = Mathf.FloorToInt((timeOfTheDay % 1) * 60f);
        }
        else
        {
            timeOfTheDay = hour + minute / 60f;
        }
        _TimeOfTheDay = timeOfTheDay;

        if (method != _method || sunCalculator == null)
        {
            _method = method;
            switch (method)
            {
                case Method.Approximation:
                    sunCalculator = new ApproximationSunCalculator();
                    break;
                case Method.Accurate:
                    sunCalculator = new AccurateSunCalculator();
                    break;
            }
        }

        sunCalculator.GetElevationAzimuth(new DateTime(year, month, day, hour, minute, 0), latitude, longitude, timeZone, ref elevation, ref azimuth);

        transform.localRotation = Quaternion.Euler(elevation, -azimuth, 0);
        transform.localPosition = -transform.forward * domeRadius;

        if (showDayTrajectory && material && !Application.isPlaying)
        {
            UpdateSunForTheDay();
        }
    }

    void UpdateSunForTheDay()
    {
        if (startHour >= endHour)
        {
            dayElevationAzimuth = new float[0];
            return;
        }

        int hours = endHour - startHour;
        dayElevationAzimuth = new float[2 * (resolution * hours + 1)];
        float e = 0, a = 0;
        int index = 0;
        float mins = 60f / resolution;
        DateTime dt = new DateTime(year, month, day, startHour, 0, 0);
        for (int i = 0; i < hours; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                sunCalculator.GetElevationAzimuth(dt.AddMinutes(j * mins), latitude, longitude, timeZone, ref e, ref a);
                dayElevationAzimuth[index++] = e;
                dayElevationAzimuth[index++] = a;
            }
            dt = dt.AddHours(1);
        }
        sunCalculator.GetElevationAzimuth(dt, latitude, longitude, timeZone, ref e, ref a);
        dayElevationAzimuth[index++] = e;
        dayElevationAzimuth[index++] = a;
    }

    void OnDrawGizmos()
    {
        if (showDayTrajectory)
        {
            int index = 0;
            int hour = resolution * 2;
            Vector3 center = Vector3.zero;

            GL.PushMatrix();
            if (transform.parent)
            {
                center = transform.parent.position;
                GL.MultMatrix(transform.parent.localToWorldMatrix);
            }
            material.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            Vector3 pos1 = Quaternion.Euler(dayElevationAzimuth[index++], dayElevationAzimuth[index++], 0) * new Vector3(0, 0, -domeRadius);
            if (showHourDivisions)
            {
                Debug.DrawLine(center, center + pos1, material.color);
            }
            while (index < dayElevationAzimuth.Length)
            {
                Vector3 pos2 = Quaternion.Euler(dayElevationAzimuth[index++], dayElevationAzimuth[index++], 0) * new Vector3(0, 0, -domeRadius);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(pos1.x, pos1.y, pos1.z);
                GL.Vertex3(pos2.x, pos2.y, pos2.z);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(pos2.x, pos2.y, pos2.z);
                GL.Vertex3(pos1.x, pos1.y, pos1.z);
                pos1 = pos2;
                if (showHourDivisions)
                {
                    if ((index - 2) % hour == 0)
                        Debug.DrawLine(center, center + pos1, material.color);
                    else
                        Debug.DrawLine(center, center + pos1, material.color * 0.5f);
                }
            }
            GL.End();
            GL.PopMatrix();
        }
    }

}
