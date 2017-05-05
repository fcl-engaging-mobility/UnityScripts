// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper classes to store values within a min/max range

using UnityEngine;

[System.Serializable]
public class ValueRangeControllers
{
    [Range(0f, 1f)]
    public float min = 0.0f;
    [Range(0f, 1f)]
    public float max = 1.0f;
    [Range(0f, 1f)]
    public float mid = 0.5f;
    [Range(0f, 1f)]
    public float amplitude = 0.5f;
}

[System.Serializable]
public class ValueRange : ValueRangeControllers
{
    public float minValue = 0f;
    public float maxValue = 1f;
    public bool enabled = false;

    private ValueRangeControllers oldValues = new ValueRangeControllers();

    public ValueRange() { }
    public ValueRange(float minValue, float maxValue)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public Vector2 OutputValues
    {
        get
        {
            if (enabled)
                return new Vector2(Mathf.Lerp(minValue, maxValue, min), Mathf.Lerp(minValue, maxValue, max));
            else
                return new Vector2(minValue, maxValue);
        }
    }
    public Vector2 OutputValuesNormalized
    {
        get
        {
            if (enabled)
                return new Vector2(min, max);
            else
                return new Vector2(0, 1);
        }
    }

    public void UpdateValues()
    {
        if (min != oldValues.min)
        {
            float aux = oldValues.min + oldValues.amplitude * 2f;
            amplitude = (aux - min) * 0.5f;
            mid = min + amplitude;

            oldValues.min = min;
            oldValues.mid = mid;
            oldValues.amplitude = amplitude;
        }
        else if (max != oldValues.max)
        {
            float aux = oldValues.max - oldValues.amplitude * 2f;
            amplitude = (max - aux) * 0.5f;
            mid = max - amplitude;

            oldValues.max = max;
            oldValues.mid = mid;
            oldValues.amplitude = amplitude;
        }
        else if (mid != oldValues.mid)
        {
            min = mid - amplitude;
            max = mid + amplitude;
            oldValues.min = min;
            oldValues.max = max;
            oldValues.mid = mid;
        }
        else if (amplitude != oldValues.amplitude)
        {
            min = mid - amplitude;
            max = mid + amplitude;
            oldValues.min = min;
            oldValues.max = max;
            oldValues.amplitude = amplitude;
        }
    }
}
