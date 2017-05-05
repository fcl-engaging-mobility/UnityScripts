// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: vission data is a form of thermal data, since it's going
//          to be displayed as a volumetric thermography

using UnityEngine;

[CreateAssetMenu(menuName = "ThermalDataProvider/Vision Data")]
public class VisionDataProvider : ThermalDataProvider
{
    [Header("Vision Data")]
    public string visionFile = "Tracking/";

    private VisionDataIO.Header dataHeader;
    private ThermalDataBase thermalData = null;

    public override uint Resolution
    {
        get { return dataHeader.resolution; }
    }

    void OnEnable()
    {
        Init();
    }

    public override void Reset()
    {
        Init();
    }

    private void Init()
    {
        // Reset thermal data
        thermalData = null;
    }

    public override void LoadThermalData<T, C>()
    {
        if (thermalData == null || !(thermalData is T))
        {
            thermalData = new T();
        }

        VisionDataIO.LoadFromBinary(visionFile, thermalData as T, ref dataHeader);
    }

    public override ThermalRenderingData<C> GetThermalData<T, C>()
    {
        if (thermalData == null || !(thermalData is T))
        {
            LoadThermalData<T, C>();
            
            if (thermalData == null || thermalData.Cells<C>() == null)
            {
                Debug.LogWarning("No vision data was loaded");
                return null;
            }
        }

        ThermalFrame frame = thermalData.frames[1];

        ThermalRenderingData<C> data = new ThermalRenderingData<C>();
        data.cells = thermalData.Cells<C>();
        data.cellType = CellType.Billboard;
        data.startIndex = thermalData.frames[0].offset;
        data.endIndex = frame.offset;
        data.minTemperature = frame.minTemperature;
        data.maxTemperature = frame.minTemperature + frame.temperatureRange;
        data.resolution = dataHeader.resolution;

        return data;
    }
    
}
