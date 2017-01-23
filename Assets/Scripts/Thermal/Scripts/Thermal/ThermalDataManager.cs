// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: Key component that connects a data provider with the thermal camera

using UnityEngine;

public class ThermalDataManager : MonoBehaviour
{
    private static readonly uint CELL_BUFFER_SIZE = 1048576;

    private int currentPipelineVersion = -1;

    [Header("Thermal Camera")]
    public ThermalCamera thermalCamera;
    public bool updateTemperatureRanges = false;

    [Header("Thermal Volume")]
    public VolumePlaceholder thermalVolumeLocation;
    public float thermalVolumeSize = 153.60f;
    public float thermalVolumeHeight = 5.40f;

    [Header("Thermal Data")]
    public ThermalDataProvider dataProvider;
    private ThermalDataProvider _dataProvider;

    private ThermalDrawer thermalDrawer;
    private ThermalCells thermalCells;

    public int TotalCellsCount
    {
        get { return thermalDrawer.Count; }
    }

    void OnEnable()
    {
        _dataProvider = dataProvider;

        Restart();

        if (dataProvider)
        {
            dataProvider.Subscribe(OnDataChange);

            DisplayHeatData();
        }
    }

    void OnDisable()
    {
        if (dataProvider)
        {
            dataProvider.Unsubscribe(OnDataChange);
        }
        Shutdown();
    }

    private void Restart()
    {
        Shutdown();

        ValidateResources();

        if (dataProvider)
        {
            dataProvider.Reset();

            Init();
        }
    }

    private void Init()
    {
        if (dataProvider)
        {
            InitThermalDrawer(dataProvider.pipelineVersion);
        }
    }

    private void Shutdown()
    {
        ReleaseThermalDrawer();
    }

    private void InitThermalDrawer(int version)
    {
        switch (version)
        {
            case 1:
                InitThermalDrawerT<PipelineV1.ThermalCell>(version);
                break;
            case 2:
                InitThermalDrawerT<PipelineV2.ThermalCell>(version);
                break;
            case 3:
                InitThermalDrawerT<uint>(version);
                break;
            default:
                Debug.LogWarning("Wrong pipeline version!");
                break;
        }
    }

    private void InitThermalDrawerT<C>(int version)
        where C : struct
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("InitThermalDrawerT shouldn't be called in edit mode");
            return;
        }

        currentPipelineVersion = version;

        ThermalCellsT<C> tc = new ThermalCellsT<C>(CELL_BUFFER_SIZE);
        Shader shader = Shader.Find("Thermal/ThermalCellsV" + version);
        if (shader == null)
        {
            Debug.LogError("Couldn't find ThermalCellsV" + version + " shader");
            return;
        }

        thermalDrawer = thermalCamera.CreateThermalDrawer(CellType.Billboard, tc.cells, shader);
        thermalCells = tc;
    }

    void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            ValidateResources();

            if (_dataProvider != dataProvider)
            {
                if (_dataProvider != null)
                {
                    _dataProvider.Unsubscribe(OnDataChange);
                    Shutdown();
                }
                _dataProvider = dataProvider;
                if (dataProvider != null)
                {
                    Init();
                    dataProvider.Subscribe(OnDataChange);
                }
            }

            DisplayHeatData();
        }
    }

    void OnDataChange(ThermalDataProvider provider)
   {
        if (currentPipelineVersion != dataProvider.pipelineVersion)
        {
            Restart();
        }
        if (Application.isPlaying && isActiveAndEnabled)
        {
            DisplayHeatData();
        }
    }

    private void ValidateResources()
    {
        if (thermalCamera == null)
        {
            Debug.LogWarning("No thermal camera has been assigned to " + name);
        }
        if (dataProvider == null)
        {
            Debug.LogWarning("No data provider has been assigned to " + name);
        }
    }

    private void ReleaseThermalDrawer()
    {
        if (thermalDrawer != null)
        {
            if (thermalCamera != null)
            {
                thermalCamera.UnregisterThermalDrawer(thermalDrawer);
            }
            thermalDrawer.Release();
            thermalDrawer = null;
        }
    }

    public void DisplayHeatData()
    {
        switch (dataProvider.pipelineVersion)
        {
            case 1:
                DisplayHeatDataT<PipelineV1.ThermalData, PipelineV1.ThermalCell>();
                break;
            case 2:
                DisplayHeatDataT<PipelineV2.ThermalData, PipelineV2.ThermalCell>();
                break;
            case 3:
                DisplayHeatDataT<PipelineV3.ThermalData, uint>();
                break;
            default:
                Debug.LogWarning("Wrong pipeline version!");
                break;
        }
    }

    private void DisplayHeatDataT<T, C>()
        where T : ThermalDataT<C>, new()
        where C : struct
    {
        ThermalRenderingData<C> data = dataProvider.GetThermalData<T, C>();

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        //+ Use fast array copy!
        int newCount = 0;
        C[] cells = thermalCells.Cells<C>();
        for (uint i = data.startIndex; i < data.endIndex; ++i)
        {
            cells[newCount++] = data.cells[i];
        }

        thermalDrawer.SetDataFast(cells, newCount);
        thermalDrawer.Type = data.cellType;

        if (updateTemperatureRanges)
        {
            thermalCamera.temperatureRange.minValue = data.minTemperature;
            thermalCamera.temperatureRange.maxValue = data.maxTemperature;
            thermalDrawer.SetTemperatureMinMax(data.minTemperature, data.maxTemperature);
        }

        UpdateDrawerCellSize(data.resolution);

        sw.Stop();
        if (sw.Elapsed.TotalMilliseconds > 20)
        {
            Debug.LogWarning("Displaying thermal data took " + sw.Elapsed.TotalMilliseconds + "ms.");
        }
    }


    private void UpdateDrawerCellSize(uint resolution)
    {
        float volumeSize;
        float volumeHeight;
        if (thermalVolumeLocation != null)
        {
            Vector3 position = thermalVolumeLocation.transform.position - Vector3.Scale(Vector3.one - thermalVolumeLocation.offset, thermalVolumeLocation.size * 0.5f);
            thermalDrawer.SetLocation(position, thermalVolumeLocation.transform.rotation);
            volumeSize = thermalVolumeLocation.size.x;
            volumeHeight = thermalVolumeLocation.size.y;
        }
        else
        {
            volumeSize = thermalVolumeSize;
            volumeHeight = thermalVolumeHeight;
        }

        float cellSize = volumeSize / resolution;
        float cellHeight = thermalDrawer.Type == CellType.GroundQuad ? volumeHeight * 0.5f : cellSize;

        thermalDrawer.SetCellSize(cellSize, cellHeight);
        thermalDrawer.SetResolution((int)resolution);
    }

}
