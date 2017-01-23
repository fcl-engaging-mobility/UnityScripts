// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a specializad implementation of a thermal data provider for
//          the CCS project

using UnityEngine;
using System;

[CreateAssetMenu(menuName = "ThermalDataProvider/CCS")]
public class ThermalDataProviderCCS : ThermalDataProvider
{
    [Header("Thermal Data")]
    public int firstFileStartTime = 0;
    public int sampleInterval = 100;
    public int fileTimeInterval = 5000;
    public int numberOfTimeFiles = 20;
    public uint minLevel = 0;
    public uint maxLevel = 10;
    public string thermalFilesPath = "Assets/HeatData/";

    private ThermalDataBase[] thermalData;

    [NonSerialized] public int currentTime = 0;
    [NonSerialized] public uint currentLevel = 0;
    private int lastFileEndTime = 0;

    public override uint Resolution
    {
        get { return (uint)Math.Pow(2, currentLevel); }
    }

    void OnEnable()
    {
        currentLevel = maxLevel;
        currentTime = firstFileStartTime;

        Init();
    }

    public override void Reset()
    {
        Init();
    }

    private void Init()
    {
        // Init thermal data
        thermalData = new ThermalDataBase[maxLevel + 1];

        lastFileEndTime = firstFileStartTime + fileTimeInterval * numberOfTimeFiles;
    }

    public override void LoadThermalData<T, C>()
    {
        LoadThermalData<T, C>(currentLevel, currentTime);
    }

    public override ThermalRenderingData<C> GetThermalData<T, C>()
    {
        return GetThermalData<T, C>(currentLevel, currentTime);
    }

    public void LoadThermalData<T, C>(uint level, int time)
        where T : ThermalDataT<C>, new()
        where C : struct
    {
        int index = (time - firstFileStartTime) / fileTimeInterval;
        int minTime = (firstFileStartTime + index * fileTimeInterval);
        int maxTime = minTime + fileTimeInterval;

        T data = null;
        if (thermalData[level] == null || !(thermalData[level] is T))
        {
            thermalData[level] = data = new T();
        }
        else
        {
            data = (T)thermalData[level];
        }

        string path = thermalFilesPath + "v" + pipelineVersion + "/";
        ThermalDataIO.LoadFromBinary(path, level, minTime, maxTime, (uint)sampleInterval, data);

        currentLevel = level;
        currentTime = time;
    }

    public ThermalRenderingData<C> GetThermalData<T, C>(uint level, int time)
        where T : ThermalDataT<C>, new()
        where C : struct
    {
        ThermalDataBase levelData = thermalData[level];
        if (levelData == null || !(levelData is T) ||
            time < levelData.startTime ||
            time >= levelData.endTime)
        {
            LoadThermalData<T, C>(level, time);
            levelData = (ThermalDataT<C>)thermalData[level];

            if (levelData == null || levelData.Cells<C>() == null)
            {
                Debug.LogWarning("No thermal data found for level " + level);
                return null;
            }
        }

        int sampleIndex = (time % fileTimeInterval) / sampleInterval;
        ThermalFrame frame = levelData.frames[sampleIndex + 1];

        ThermalRenderingData<C> renderData = new ThermalRenderingData<C>();
        renderData.cells = levelData.Cells<C>();
        renderData.cellType = level < maxLevel ? CellType.GroundQuad : CellType.Billboard;
        renderData.startIndex = levelData.frames[sampleIndex].offset;
        renderData.endIndex = frame.offset;
        renderData.minTemperature = frame.minTemperature;
        renderData.maxTemperature = frame.minTemperature + frame.temperatureRange;
        renderData.resolution = (uint)Math.Pow(2, level);

        return renderData;
    }

    public int getTimeFromPercentage()
    {
        return getTimeFromPercentage(currentTime);
    }

    public int getTimeFromPercentage(float percentage)
    {
        int value = (int)Mathf.Lerp(firstFileStartTime, lastFileEndTime, percentage);
        int index = (value - firstFileStartTime) / sampleInterval;
        return firstFileStartTime + index * sampleInterval;
    }

    public float getPercentageFromTime(int time)
    {
        return Mathf.InverseLerp(firstFileStartTime, lastFileEndTime, time);
    }

    public int getLevelFromPercentage(float percentage)
    {
        return (int)Mathf.Lerp(minLevel, maxLevel, percentage);
    }

    public float getPercentageFromLevel(int level)
    {
        return Mathf.InverseLerp(minLevel, maxLevel, level);
    }

    public int getLevelFromPercentage()
    {
        return getLevelFromPercentage(currentLevel);
    }

}
