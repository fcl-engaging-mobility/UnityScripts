// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: Component that renders thermal data on top of the scene.
//          Needs to be attached to a camera

using UnityEngine;
using System.Collections.Generic;

public class ThermalCamera : MonoBehaviour
{
    public enum ColorGradient
    {
        Monochromatic,
        Purple,
        Rainbow_Fast,
        Rainbow_Accurate
    }

    public Shader postProcessShader;
    public ColorGradient gradient = ColorGradient.Purple;
    public Color tint = Color.white;

    public bool useCellType = false;
    public CellType thermalCellType = CellType.Billboard;

    [Range(0f, 1f)]
    public float thermalOpacity = 0.5f;
    public bool useTemperatureAsOpacity = false;

    public ValueRange temperatureRange;
    public ValueRange xRange = new ValueRange(-1000, 1000);
    public ValueRange yRange = new ValueRange(-1000, 1000);
    public ValueRange zRange = new ValueRange(-1000, 1000);

    private RenderTexture rtThermalImage;
    private Material postProcessMaterial;

    private int shaderTextureID;
    private int shaderOpacityID;
    private int individualOpacityID;
    private int tintID;

    private List<ThermalDrawer> thermalDrawers = new List<ThermalDrawer>();


    void OnEnable()
    {
        Init();
    }

    void OnDisable()
    {
        ReleaseResources();
    }

    void OnValidate()
    {
        temperatureRange.UpdateValues();
        xRange.UpdateValues();
        yRange.UpdateValues();
        zRange.UpdateValues();
        foreach (var drawer in thermalDrawers)
        {
            SetupDrawer(drawer);
        }
        if (postProcessMaterial != null)
        {
            postProcessMaterial.SetFloat(shaderOpacityID, thermalOpacity);
            postProcessMaterial.SetFloat(individualOpacityID, useTemperatureAsOpacity ? 1 : 0);
            postProcessMaterial.SetColor(tintID, tint);
            gradient = (ColorGradient)((int)gradient % postProcessMaterial.passCount);
        }
    }

    void OnPostRender()
    {
        if (rtThermalImage.width != Screen.width || rtThermalImage.height != Screen.height)
        {
            CreateRenderTexture();
            postProcessMaterial.SetTexture(shaderTextureID, rtThermalImage);
        }

        RenderTexture rt = RenderTexture.active;
        Graphics.SetRenderTarget(rtThermalImage.colorBuffer, rt.depthBuffer);
        GL.Clear(false, true, Color.clear);

        RenderThermalData();

        Graphics.SetRenderTarget(rt);
    }

    private void RenderThermalData()
    {
        CellType? type = useCellType ? (CellType?) thermalCellType : null;
        foreach (var drawer in thermalDrawers)
        {
            if (drawer.Enabled)
            {
                drawer.Draw(type, GetComponent<Camera>());
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, postProcessMaterial, (int)gradient);
    }

    public ThermalDrawer CreateThermalDrawer<T>(CellType type, T[] array, Shader shader)
    {
        int elementSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        ThermalDrawer thermalDrawer = new ThermalDrawer(array.Length, elementSize, shader, type);
        RegisterThermalDrawer(thermalDrawer);
        return thermalDrawer;
    }

    void Init()
    {
        // Check if project settings are correct
        if (QualitySettings.antiAliasing > 0 &&
            GetComponent<Camera>().actualRenderingPath != RenderingPath.DeferredShading)
        {
            Debug.LogWarning("Anti-aliasing is enabled and the camera's rendering path is not deferred shading!");
            GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;
        }

        CreateMaterials();

        InitMaterialProperties();

        CreateRenderTexture();
        postProcessMaterial.SetTexture(shaderTextureID, rtThermalImage);
        postProcessMaterial.SetFloat(shaderOpacityID, thermalOpacity);
        postProcessMaterial.SetFloat(individualOpacityID, useTemperatureAsOpacity ? 1 : 0);
        postProcessMaterial.SetColor(tintID, tint);
    }

    void ReleaseResources()
    {
        ReleaseRenderTexture();
        ReleaseMaterials();
    }

    void InitMaterialProperties()
    {
        ThermalDrawer.CheckShaderProperty(postProcessMaterial, "ThermalTexture", ref shaderTextureID);
        ThermalDrawer.CheckShaderProperty(postProcessMaterial, "Opacity", ref shaderOpacityID);
        ThermalDrawer.CheckShaderProperty(postProcessMaterial, "IndividualOpacity", ref individualOpacityID);
        ThermalDrawer.CheckShaderProperty(postProcessMaterial, "Tint", ref tintID);
    }

    void CreateRenderTexture()
    {
        ReleaseRenderTexture();
        rtThermalImage = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        rtThermalImage.hideFlags = HideFlags.HideAndDontSave;
        rtThermalImage.enableRandomWrite = true;
        rtThermalImage.anisoLevel = 0;
        rtThermalImage.filterMode = FilterMode.Point;
        rtThermalImage.useMipMap = false;
        rtThermalImage.generateMips = false;
        rtThermalImage.Create();
    }

    void ReleaseRenderTexture()
    {
        if (rtThermalImage != null)
        {
            DestroyImmediate(rtThermalImage);
            rtThermalImage = null;
        }
    }

    void CreateMaterials()
    {
        ReleaseMaterials();
        postProcessMaterial = new Material(postProcessShader);
        postProcessMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    void ReleaseMaterials()
    {
        if (postProcessMaterial != null)
        {
            DestroyImmediate(postProcessMaterial);
            postProcessMaterial = null;
        }
    }

    void RegisterThermalDrawer(ThermalDrawer drawer)
    {
        if (thermalDrawers.Contains(drawer))
        {
            Debug.LogWarning("Trying to add an already registered thermal drawer");
            return;
        }
        thermalDrawers.Add(drawer);
        SetupDrawer(drawer);
    }

    public void UnregisterThermalDrawer(ThermalDrawer drawer)
    {
        if (!thermalDrawers.Contains(drawer))
        {
            Debug.LogWarning("Couldn't find thermal drawer in the registered list");
            return;
        }
        thermalDrawers.Remove(drawer);
    }

    void SetupDrawer(ThermalDrawer drawer)
    {
        drawer.SetTemperatureMinMax(temperatureRange.minValue, temperatureRange.maxValue);
        drawer.FilterTemperatures(temperatureRange.OutputValuesNormalized);
        drawer.FilterPositions(xRange.OutputValues, yRange.OutputValues, zRange.OutputValues);
    }

}
