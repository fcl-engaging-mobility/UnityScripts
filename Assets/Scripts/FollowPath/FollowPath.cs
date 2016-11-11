// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: the attached game-object will follow a path formed by
//          a series of weighted bezier curves

using UnityEngine;
using System;

public class FollowPath : MonoBehaviour
{
    public enum PlayMode
    {
        Once,
        Loop
    }

    public enum HandleMode
    {
        Free,
        Aligned,
        Mirrored
    }

    [Header("Player")]
    public TimeController timeController;
    public float timeScale = 1f;
    public float timeOffset = 0f;
    public PlayMode mode;
    private float progress;

    [Header("Display")]
    public bool showSpeedGraph;

    [Header("Spline")]
    public bool closed;

    [SerializeField]
    private Vector3[] points;

    [SerializeField]
    private float[] times;

    [SerializeField]
    private HandleMode[] modes;

    private float cachedLastTime = 0f;
    private int cachedStartTimeIndex = 0;
    private int cachedEndTimeIndex = 1;
    private float cachedInverseDeltaTime = 0f;

    private Material material;

    private const float lookAheadTime = 2f;

    private float currentSpeed = 0f;
    public float Speed
    {
        get { return currentSpeed; }
    }

    public int ControlPointCount
    {
        get { return points.Length; }
    }

    private void OnEnable()
    {
        ResetCachedTimes();
    }

    public bool Closed
    {
        get { return closed; }
        set
        {
            closed = value;
            if (closed)
            {
                modes[modes.Length - 1] = modes[0];
                SetControlPoint(0, points[0]);
            }
        }
    }

    private void Update()
    {
        progress = timeScale * timeController.time + timeOffset;
        switch (mode)
        {
            case PlayMode.Once:
                if (progress > cachedLastTime)
                    return;
                progress = Mathf.Clamp(progress, 0, cachedLastTime);
                break;
            case PlayMode.Loop:
                progress = progress % cachedLastTime;
                break;
        }

        Vector3 position = GetPoint(progress);
        currentSpeed = (position - transform.localPosition).magnitude / Time.deltaTime;
        transform.localPosition = position;

        float nextProgress = progress + lookAheadTime;
        if (nextProgress > cachedLastTime)
        {
            Quaternion newRotation = Quaternion.LookRotation(GetVelocity(progress));
            transform.localRotation = Quaternion.Lerp(transform.localRotation, newRotation, 0.1f * Time.deltaTime);
        }
        else
        {
            transform.LookAt(GetPoint(nextProgress));
        }
    }

    private void ResetCachedTimes()
    {
        cachedStartTimeIndex = 0;
        cachedEndTimeIndex = 1;
        cachedLastTime = times[times.Length - 1];
        cachedInverseDeltaTime = 1f / (times[cachedEndTimeIndex] - times[cachedStartTimeIndex]);
    }

    public float GetTime(int index)
    {
        return times[(index + 1) / 3];
    }

    public void SetTime(int index, float time)
    {
        times[(index + 1) / 3] = time;
    }

    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    public void SetControlPoint(int index, Vector3 point)
    {
        if (index % 3 == 0)
        {
            Vector3 delta = point - points[index];
            if (closed)
            {
                if (index == 0)
                {
                    points[1] += delta;
                    points[points.Length - 2] += delta;
                    points[points.Length - 1] = point;
                }
                else if (index == points.Length - 1)
                {
                    points[0] = point;
                    points[1] += delta;
                    points[index - 1] += delta;
                }
                else
                {
                    points[index - 1] += delta;
                    points[index + 1] += delta;
                }
            }
            else
            {
                if (index > 0)
                {
                    points[index - 1] += delta;
                }
                if (index + 1 < points.Length)
                {
                    points[index + 1] += delta;
                }
            }
        }
        points[index] = point;
        EnforceMode(index);
    }

    public HandleMode GetControlPointMode(int index)
    {
        return modes[(index + 1) / 3];
    }

    public void SetControlPointMode(int index, HandleMode mode)
    {
        int modeIndex = (index + 1) / 3;
        modes[modeIndex] = mode;
        if (closed)
        {
            if (modeIndex == 0)
            {
                modes[modes.Length - 1] = mode;
            }
            else if (modeIndex == modes.Length - 1)
            {
                modes[0] = mode;
            }
        }
        EnforceMode(index);
    }

    private void EnforceMode(int index)
    {
        int modeIndex = (index + 1) / 3;
        HandleMode mode = modes[modeIndex];
        if (mode == HandleMode.Free || !closed && (modeIndex == 0 || modeIndex == modes.Length - 1))
        {
            return;
        }

        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;
        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;
            if (fixedIndex < 0)
            {
                fixedIndex = points.Length - 2;
            }
            enforcedIndex = middleIndex + 1;
            if (enforcedIndex >= points.Length)
            {
                enforcedIndex = 1;
            }
        }
        else
        {
            fixedIndex = middleIndex + 1;
            if (fixedIndex >= points.Length)
            {
                fixedIndex = 1;
            }
            enforcedIndex = middleIndex - 1;
            if (enforcedIndex < 0)
            {
                enforcedIndex = points.Length - 2;
            }
        }

        Vector3 middle = points[middleIndex];
        Vector3 enforcedTangent = middle - points[fixedIndex];
        if (mode == HandleMode.Aligned)
        {
            enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
        }
        points[enforcedIndex] = middle + enforcedTangent;
    }

    public Vector3 GetPoint(float t)
    {
        float alpha;
        int i = GetIndexFromTime(t, out alpha);
        return Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], alpha);
    }

    public Vector3 GetVelocity(float t)
    {
        float alpha;
        int i = GetIndexFromTime(t, out alpha);
        return Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], alpha);
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    public void AddCurve()
    {
        Vector3 point = points[points.Length - 1];
        Array.Resize(ref points, points.Length + 3);
        point.x += 1f;
        points[points.Length - 3] = point;
        point.x += 1f;
        points[points.Length - 2] = point;
        point.x += 1f;
        points[points.Length - 1] = point;

        Array.Resize(ref times, times.Length + 1);
        times[times.Length - 1] = times[times.Length - 2] + 1f;

        Array.Resize(ref modes, modes.Length + 1);
        modes[modes.Length - 1] = modes[modes.Length - 2];

        EnforceMode(points.Length - 4);

        if (closed)
        {
            points[points.Length - 1] = points[0];
            modes[modes.Length - 1] = modes[0];
            EnforceMode(0);
        }

        ResetCachedTimes();
    }

    public void InsertPoint(int index)
    {
        if (index < 0 || index >= points.Length - 2)
            return;

        int pointIndex = (index + 1) / 3;

        Array.Resize(ref points, points.Length + 3);
        int last = (pointIndex + 2) * 3;
        for (int i = points.Length - 1; i >= last; i--)
        {
            points[i] = points[i - 3];
            i--;
            points[i] = points[i - 3];
            i--;
            points[i] = points[i - 3];
        }
        int nextIndex = (pointIndex + 1) * 3;
        Vector3 point1 = points[pointIndex * 3];
        Vector3 point2 = points[nextIndex];
        points[nextIndex] = (point1 + point2) * 0.5f;
        points[nextIndex - 1] = Vector3.Lerp(point1, point2, 0.333f);
        points[nextIndex + 1] = Vector3.Lerp(point1, point2, 0.666f);

        Array.Resize(ref times, times.Length + 1);
        last = pointIndex + 2;
        for (int i = times.Length - 1; i >= last; i--)
        {
            times[i] = times[i - 1];
        }
        times[pointIndex + 1] = (times[pointIndex] + times[pointIndex + 1]) * 0.5f;

        Array.Resize(ref modes, modes.Length + 1);
        last = pointIndex + 2;
        for (int i = modes.Length - 1; i >= last; i--)
        {
            modes[i] = modes[i - 1];
        }
        modes[pointIndex + 1] = HandleMode.Aligned;

        //+ TODO: fix this
        EnforceMode(points.Length - 4);

        if (closed)
        {
            points[points.Length - 1] = points[0];
            modes[modes.Length - 1] = modes[0];
            EnforceMode(0);
        }

        ResetCachedTimes();
    }

    public void RemovePoint(int index)
    {
        if (index < 0 || index > points.Length - 1)
            return;

        if (points.Length <= 4)
            return;

        if (index < points.Length - 2)
        {
            int pointIndex = (index + 1) / 3;

            int start = Mathf.Max(0, pointIndex * 3 - 1);
            for (int i = points.Length - 4; i >= start; i--)
            {
                points[i] = points[i + 3];
            }
            for (int i = times.Length - 2; i >= pointIndex; i--)
            {
                times[i] = times[i + 1];
                modes[i] = modes[i + 1];
            }
        }

        Array.Resize(ref points, points.Length - 3);
        Array.Resize(ref times, times.Length - 1);
        Array.Resize(ref modes, modes.Length - 1);

        //+ TODO: fix this
        EnforceMode(points.Length - 4);

        if (closed)
        {
            points[points.Length - 1] = points[0];
            modes[modes.Length - 1] = modes[0];
            EnforceMode(0);
        }

        ResetCachedTimes();
    }

    public void Reset()
    {
        points = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(3f, 0f, 0f)
        };
        times = new float[]
        {
            0f, 1f
        };
        modes = new HandleMode[]
        {
            HandleMode.Aligned,
            HandleMode.Aligned
        };
    }

    private int GetIndexFromTime(float t, out float alpha)
    {
        if (t >= times[times.Length - 1])
        {
            alpha = 1f;
            return points.Length - 4;
        }
        else if (t < 0)
        {
            alpha = 0f;
            return 0;
        }
        else
        {
            if (t >= times[cachedEndTimeIndex])
            {
                cachedEndTimeIndex++;
                while (cachedEndTimeIndex < times.Length && times[cachedEndTimeIndex] <= t)
                    cachedEndTimeIndex++;

                cachedStartTimeIndex = cachedEndTimeIndex - 1;
                cachedInverseDeltaTime = 1f / (times[cachedEndTimeIndex] - times[cachedStartTimeIndex]);
            }
            else if (t < times[cachedStartTimeIndex])
            {
                cachedStartTimeIndex--;
                while (cachedStartTimeIndex > 0 && t < times[cachedStartTimeIndex])
                    cachedStartTimeIndex--;

                cachedEndTimeIndex = cachedStartTimeIndex + 1;
                cachedInverseDeltaTime = 1f / (times[cachedEndTimeIndex] - times[cachedStartTimeIndex]);
            }

            alpha = (t - times[cachedStartTimeIndex]) * cachedInverseDeltaTime;
            return cachedStartTimeIndex * 3;
        }
    }

    //void OnDrawGizmos()
    //{
    //    DrawSpeedGraph();
    //}

    public void DrawSpeedGraph()
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        if (material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
        }

        // Apply the material
        material.SetPass(0);

        DrawLines();

        GL.PopMatrix();
    }

    private const int graphBottomMargin = 10;
    private const int graphLeftMargin = 20;
    private const int graphRightMargin = 20;
    private const int graphSideMargins = graphLeftMargin + graphRightMargin;

    private void DrawLines()
    {
        GL.Begin(GL.LINES);

        int graphWidth = Screen.width - graphSideMargins;
        float xScale = graphWidth / times[times.Length - 1];
        float yScale = 75f;

        float speedLimitY = graphBottomMargin + 4f * yScale;
        GL.Color(Color.red);
        GL.Vertex3(graphLeftMargin, speedLimitY, 0);
        GL.Vertex3(Screen.width - graphRightMargin, speedLimitY, 0);

        GL.Color(Color.blue);
        int count = times.Length;
        for (int i = 0; i < count; i++)
        {
            float x = graphLeftMargin + times[i] * xScale;
            GL.Vertex3(x, graphBottomMargin, 0);
            GL.Vertex3(x, speedLimitY, 0);
        }

        int steps = graphWidth / 3;
        float timePerStep = times[times.Length - 1] / steps;
        float invTimePerStep = 1f / timePerStep;
        Vector3 p0 = GetPoint(0);
        float x0 = graphLeftMargin;
        float y0 = graphBottomMargin;
        GL.Color(Color.white);
        for (int i = 0; i <= steps; i++)
        {
            float t = i * timePerStep;
            Vector3 p1 = GetPoint(t);
            float speed = (p0 - p1).magnitude * invTimePerStep;
            float x1 = graphLeftMargin + t * xScale;
            float y1 = graphBottomMargin + speed * yScale;
            GL.Vertex3(x0, y0, 0);
            GL.Vertex3(x1, y1, 0);
            p0 = p1;
            x0 = x1;
            y0 = y1;
        }
        GL.Vertex3(x0, y0, 0);
        GL.Vertex3(Screen.width - graphRightMargin, graphBottomMargin, 0);
        GL.Vertex3(Screen.width - graphRightMargin, graphBottomMargin, 0);
        GL.Vertex3(graphLeftMargin, graphBottomMargin, 0);

        GL.End();
    }

    void OnValidate()
    {
        Closed = closed; // Force the check
    }

}