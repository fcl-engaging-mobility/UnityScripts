// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a wrapper class for thermal rendering code

using UnityEngine;

public enum CellType
{
    GroundQuad,
    Billboard,
    Cube
}

public class ThermalDrawer
{
    private ComputeBuffer buffer;
    private Material material;
    private CellType type;
    private int resolutionID;
    private int halfSizeID;
    private int halfHeightID;
    private int minTemperatureID;
    private int invTemperatureRangeID;
    private int filterMinTemperatureID;
    private int filterMaxTemperatureID;
    private int filterMinXID;
    private int filterMaxXID;
    private int filterMinYID;
    private int filterMaxYID;
    private int filterMinZID;
    private int filterMaxZID;

    // Keep the count as we might draw fewer cells than the buffer's capacity
    private int count = 0;
    public int Count { get { return count; } }
    public int Capacity { get { return buffer.count; } }
    public bool Enabled = true;
    public CellType Type
    {
        get { return type; }
        set { type = value; }
    }

    private Matrix4x4 modelMatrix = Matrix4x4.identity;

    public ThermalDrawer(int capacity, int strideSize, Shader shader, CellType type)
    {
        this.type = type;

        CreateMaterial(shader);
        CreateThermalComputeBuffer(capacity, strideSize);
    }

    public void SetData(System.Array data, int newCount)
    {
        if (newCount > buffer.count)
        {
            CreateThermalComputeBuffer(newCount, buffer.stride);
        }

        buffer.SetData(data);
        count = newCount;
    }

    public void SetDataFast(System.Array data, int newCount)
    {
        buffer.SetData(data);
        count = newCount;
    }

    public void SetLocation(Vector3 position, Quaternion rotation)
    {
        modelMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
    }

    public void SetCellSize(float size, float height)
    {
        material.SetFloat(halfSizeID, size * 0.5f);
        material.SetFloat(halfHeightID, height * 0.5f);
    }

    public void SetResolution(int resolution)
    {
        material.SetInt(resolutionID, resolution);
    }

    public void SetTemperatureMinMax(float min, float max)
    {
        SetTemperatureRange(min, max - min);
    }
    public void SetTemperatureRange(float min, float range)
    {
        material.SetFloat(minTemperatureID, min);
        material.SetFloat(invTemperatureRangeID, 1f / range);
    }

    public void FilterTemperatures(Vector2 minmax)
    {
        FilterTemperatures(minmax.x, minmax.y);
    }
    public void FilterTemperatures(float min, float max)
    {
        material.SetFloat(filterMinTemperatureID, min);
        material.SetFloat(filterMaxTemperatureID, max);
    }

    public void FilterPositions(Vector2 x, Vector2 y, Vector2 z)
    {
        material.SetFloat(filterMinXID, x.x);
        material.SetFloat(filterMaxXID, x.y);
        material.SetFloat(filterMinYID, y.x);
        material.SetFloat(filterMaxYID, y.y);
        material.SetFloat(filterMinZID, z.x);
        material.SetFloat(filterMaxZID, z.y);
    }

    public void Draw(CellType? otherType, Camera camera)
    {
        GL.PushMatrix();
        GL.modelview = camera.worldToCameraMatrix * modelMatrix;

        material.SetPass((int)(otherType ?? type));
        Graphics.DrawProcedural(MeshTopology.Points, count, 1);

        GL.PopMatrix();
    }

    public void Release()
    {
        ReleaseMaterial();
        ReleaseThermalComputeBuffer();
    }

    void CreateMaterial(Shader shader)
    {
        ReleaseMaterial();

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        
        CheckShaderProperty(material, "Resolution", ref resolutionID);
        CheckShaderProperty(material, "HalfSize", ref halfSizeID);
        CheckShaderProperty(material, "HalfHeight", ref halfHeightID);
        CheckShaderProperty(material, "MinTemperature", ref minTemperatureID);
        CheckShaderProperty(material, "InvTemperatureRange", ref invTemperatureRangeID);
        CheckShaderProperty(material, "FilterMinTemperature", ref filterMinTemperatureID);
        CheckShaderProperty(material, "FilterMaxTemperature", ref filterMaxTemperatureID);
        CheckShaderProperty(material, "FilterMinX", ref filterMinXID);
        CheckShaderProperty(material, "FilterMaxX", ref filterMaxXID);
        CheckShaderProperty(material, "FilterMinY", ref filterMinYID);
        CheckShaderProperty(material, "FilterMaxY", ref filterMaxYID);
        CheckShaderProperty(material, "FilterMinZ", ref filterMinZID);
        CheckShaderProperty(material, "FilterMaxZ", ref filterMaxZID);
    }

    void ReleaseMaterial()
    {
        if (material != null)
        {
            Object.Destroy(material);
            material = null;
        }
    }

    void CreateThermalComputeBuffer(int count, int strideSize)
    {
        ReleaseThermalComputeBuffer();
        buffer = new ComputeBuffer(count, strideSize, ComputeBufferType.Default);
        material.SetBuffer("thermalBuffer", buffer);
    }

    void ReleaseThermalComputeBuffer()
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }

    public static void CheckShaderProperty(Material mat, string propertyName, ref int propertyID)
    {
        if (mat.HasProperty(propertyName))
        {
            propertyID = Shader.PropertyToID(propertyName);
        }
        else
        {
            Debug.LogWarning("Shader " + mat.shader.name + " doesn't have property: " + propertyName);
        }
    }
}
