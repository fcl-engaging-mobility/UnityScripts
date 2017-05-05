// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: an abstract implementation of a thermal data provider

using UnityEngine;

public abstract class ThermalDataProvider : ScriptableObject
{
    [Range(1, 3)]
    public int pipelineVersion = 3;

    // The delegate the subscribers must implement
    public delegate void OnDataChange(ThermalDataProvider provider);

    // Instance of the delegate
    private OnDataChange dataChangeEvent;

    public abstract uint Resolution { get; }

    public abstract void Reset();

    public abstract void LoadThermalData<T, C>()
        where T : ThermalDataT<C>, new()
        where C : struct;

    public abstract ThermalRenderingData<C> GetThermalData<T, C>()
        where T : ThermalDataT<C>, new()
        where C : struct;

    public void Subscribe(OnDataChange dataChangeSubscriber)
    {
        dataChangeEvent += dataChangeSubscriber;
    }

    public void Unsubscribe(OnDataChange dataChangeSubscriber)
    {
        dataChangeEvent -= dataChangeSubscriber;
    }

    public void Notify()
    {
        // Check if there are any subscribers
        if (dataChangeEvent != null)
        {
            dataChangeEvent(this);
        }
    }

    void OnValidate()
    {
        Notify();
    }
}
