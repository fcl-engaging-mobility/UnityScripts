// Copyright (C) 2016 Michael Joos
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos
// Summary: allows to display graphs of game variables' values changing over time.
//          To use it, add it to an empty GameObject called "GameProfiler".
//          In another script, paste the sample code bellow.

using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class ProfileItem
{
    public string id;
    public GraphSettings settings;
    public float[] values;

    public ProfileItem(string id, int height, int length)
    {
        this.id = id;
        settings = new GraphSettings();
        settings.height = height;
        values = new float[length];
    }
}

[Serializable]
public class GraphSettings
{
    public int height;
    public float min = 0;
    public float start = 0;
    public float max = 100;
}

/*
    Usage:

    private IGameProfiler profiler;
	void OnEnable () {
		profiler = GameProfiler.Get("GameProfiler");
		var settings = profiler.Add("myID");
		settings.min = 0;
		settings.max = 100;
	}

	void LateUpdate () {
		profiler.Update ("myID", value);
	}
 */
public interface IGameProfiler
{
    GraphSettings Add(string id);
    GraphSettings Add(string id, int height);
    GraphSettings GetSettings(string id);
    void Update(string id, float value);
    void Remove(string id);
    int GetCount();
    bool Has(string id);
}

class EmptyGameProfiler : IGameProfiler
{
    private static GraphSettings dummy = new GraphSettings();
    public GraphSettings Add(string id)
    {
        return dummy;
    }
    public GraphSettings Add(string id, int height)
    {
        return dummy;
    }
    public GraphSettings GetSettings(string id)
    {
        return dummy;
    }
    public void Update(string id, float value) { }
    public void Remove(string id) { }
    public int GetCount() { return 0; }
    public bool Has(string id) { return false; }
}

class GameProfiler : MonoBehaviour, IGameProfiler
{
    public Vector2 position = new Vector2(0, 0);

    public int defaultGraphHeight = 51;
    public int historyLength = 200;

    public bool scrollUpdates = true;

    public Color backgroundColor = new Color(0.3f, 0.3f, 0.3f, .5f);
    public Color borderColor = new Color(0.6f, 0.6f, .6f);

    public Color lowColor = new Color(0, 1, 0);
    public Color hightColor = new Color(1, 0, 0);
    private HSBColor lowHSB;
    private HSBColor hightHSB;
    public float threshold = 0.3f;

    private RectOffset graphMargin;

    private int labelWidth = 150;
    private int labelMarginTop = 2;
    private int labelMarginLeft = 4;
    private int rowWidth;
    private Material material;

    public List<ProfileItem> itemList = new List<ProfileItem>();
    public List<ProfileItem> newItemList = new List<ProfileItem>();
    private Dictionary<string, ProfileItem> itemDict = new Dictionary<string, ProfileItem>();
    private int historyIndex = 0;

    // Execution Order:
    //--------------------
    // Reset (editor)
    // Awake
    // OnEnable
    // Start
    //   Update
    //   LateUpdate
    //   OnPreRender
    //   OnRenderObject
    //   OnPostRender
    //   OnRenderImage
    //   OnGUI (multiple times)
    //   yield WaitForEndOfFrame
    // OnDisable
    // OnDestroy

    //
    // Unity Methods
    //

    void Start()
    {
        initialize();
    }

    void OnEnable()
    {
        if (newItemList.Count > 0)
        {
            Debug.Log("NewList!");
            itemList = newItemList;
            newItemList = new List<ProfileItem>();
        }
        if (itemDict.Count != itemList.Count)
        {
            itemDict.Clear();
            foreach (var item in itemList)
            {
                itemDict.Add(item.id, item);
            }
        }
        Debug.Assert(itemDict.Count == itemList.Count);
    }

    void Update()
    {
        int prevIndex = historyIndex;
        historyIndex = (historyIndex + 1) % historyLength;
        foreach (var item in itemList)
            item.values[historyIndex] = item.values[prevIndex];
    }

    void OnGUI()
    {
        if (itemDict.Count > 0)
        {
            drawUI();
            drawLabels();
        }
    }

    //
    // Private Methods
    //

    private void initialize()
    {
        // QualitySettings.antiAliasing = 0;

        initMaterial();

        graphMargin = new RectOffset(2, 2, 2, 2);
        rowWidth = labelWidth + graphMargin.horizontal + historyLength * 2 - 1;
        lowHSB = new HSBColor(lowColor);
        hightHSB = new HSBColor(hightColor);
    }

    public GraphSettings Add(string id)
    {
        return Add(id, defaultGraphHeight);
    }

    public GraphSettings Add(string id, int height)
    {
        ProfileItem item;
        if (itemDict.TryGetValue(id, out item))
        {
            item.settings.height = height;
        }
        else
        {
            item = new ProfileItem(id, height, historyLength);

            // Odd way of knowing that the behaviour has been disabled and not yet re-enabled
            if (itemList.Count == itemDict.Count)
            {
                itemList.Add(item);
                itemDict.Add(id, item);
            }
            else
            {
                newItemList.Add(item);
            }
        }
        return item.settings;
    }

    public GraphSettings GetSettings(string id)
    {
        return itemDict[id].settings;
    }

    public void Update(string id, float value)
    {
        itemDict[id].values[historyIndex] = value;
    }

    public void Remove(string id)
    {
        ProfileItem item;
        if (itemDict.TryGetValue(id, out item))
        {
            itemDict.Remove(id);
            itemList.Remove(item);
        }
    }

    public int GetCount()
    {
        return itemDict.Count;
    }
    public bool Has(string id)
    {
        return itemDict.ContainsKey(id);
    }

    //
    // UI Methods
    //

    private void initMaterial()
    {
        var shader = Shader.Find("Sprites/Default");
        material = new Material(shader);
        //		material.hideFlags = HideFlags.HideAndDontSave;
        //		// Turn on alpha blending
        //		material.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        //		material.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        //		// Turn backface culling off
        //		material.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        //		// Turn off depth writes
        //		material.SetInt ("_ZWrite", 0);
    }

    private void drawLabels()
    {
        var y = position.y + labelMarginTop;
        foreach (var item in itemList)
        {
            Rect rect = new Rect(position.x + labelMarginLeft, y, labelWidth, item.settings.height);
            if (item.settings.max - item.settings.min > 10)
                GUI.Label(rect, item.id + ": " + string.Format("{0,7:0.0}", item.values[historyIndex]));
            else
                GUI.Label(rect, item.id + ": " + string.Format("{0,7:0.0000}", item.values[historyIndex]));

            y += item.settings.height + graphMargin.vertical + 1;
        }
    }

    private void drawUI()
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        // Apply the material
        material.SetPass(0);

        drawBackground();
        drawLines();

        GL.PopMatrix();
    }

    private void drawBackground()
    {
        // Draw quads
        GL.Begin(GL.QUADS);
        GL.Color(backgroundColor);

        var pos = position;
        pos.y = Screen.height - 1 - pos.y;
        foreach (var item in itemList)
        {
            int height = item.settings.height + graphMargin.vertical;
            GL.Vertex3(pos.x, pos.y, 0);
            GL.Vertex3(pos.x, pos.y - height, 0);
            GL.Vertex3(pos.x + rowWidth, pos.y - height, 0);
            GL.Vertex3(pos.x + rowWidth, pos.y, 0);
            pos.y -= height + 1;
        }
        GL.End();
    }

    private void drawLines()
    {
        GL.Begin(GL.LINES);

        Vector2 pos = position;
        pos.y = Screen.height - 1 - pos.y;
        foreach (var item in itemList)
        {
            drawBorderLine(ref pos, rowWidth);
            drawGraphLine(item, ref pos);
            pos.y -= item.settings.height + graphMargin.vertical + 1;
        }
        // Draw last border line
        drawBorderLine(ref pos, rowWidth);
        GL.End();
    }

    private void drawBorderLine(ref Vector2 pos, int width)
    {
        GL.Color(borderColor);
        GL.Vertex3(pos.x, pos.y, 0);
        GL.Vertex3(pos.x + width, pos.y, 0);
    }

    private void drawGraphLine(ProfileItem item, ref Vector2 pos)
    {
        var settings = item.settings;
        float factor = (settings.height - 1) / (settings.max - settings.min);
        int baseline = (int)((settings.start - settings.min) * factor);
        Vector2 graphPoint = new Vector2(0, pos.y - graphMargin.top - settings.height + baseline);
        int index = historyIndex + 1;
        if (scrollUpdates)
        {
            graphPoint.x = rowWidth - graphMargin.right;
            int height;
            if (index < historyLength)
            {
                height = (int)((item.values[index] - settings.start) * factor);
                drawGraphLineFrom(item, ref graphPoint, factor, index, historyLength, -1, ref height);
                graphPoint.x += 2;
                drawGraphLineFrom(item, ref graphPoint, factor, -1, index, -1, ref height);
            }
            else
            {
                drawGraphLineFrom(item, ref graphPoint, factor, 0, index, -1);
            }
        }
        else
        {
            graphPoint.x = pos.x + labelWidth + graphMargin.left;
            drawGraphLineFrom(item, ref graphPoint, factor, 0, index, 1);
            if (index < historyLength)
            {
                drawGraphLineFrom(item, ref graphPoint, factor, index, historyLength, 1);
            }
        }
    }

    private void drawGraphLineFrom(ProfileItem item, ref Vector2 pt, float factor, int start, int end, int offset)
    {
        int height = (int)((item.values[start] - item.settings.start) * factor);
        drawGraphLineFrom(item, ref pt, factor, start, end, offset, ref height);
    }

    private void drawGraphLineFrom(ProfileItem item, ref Vector2 pt, float factor, int start, int end, int offset, ref int height)
    {
        int prevValueHeight = height;
        float topThreshold = (item.settings.max - item.settings.start) * threshold;
        float botThreshold = (item.settings.min - item.settings.start) * threshold;
        var topRange = item.settings.max - topThreshold;
        var botRange = item.settings.min - botThreshold;
        for (int i = start + 1; i < end; i++)
        {
            // Enable these lines for more acurate graphs
            //GL.Vertex3 (pt.x, pt.y + prevValueHeight, 0);
            //GL.Vertex3 (pt.x+offset, pt.y + prevValueHeight, 0);

            float value = item.values[i] - item.settings.start;
            int valueHeight = (int)(value * factor);
            setColor(value, topThreshold, botThreshold, topRange, botRange);
            GL.Vertex3(pt.x, pt.y + prevValueHeight, 0);

            pt.x += 2 * offset;
            GL.Vertex3(pt.x, pt.y + valueHeight, 0);

            prevValueHeight = valueHeight;
        }
        GL.Vertex3(pt.x, pt.y + prevValueHeight, 0);
        pt.x += offset;
        GL.Vertex3(pt.x, pt.y + prevValueHeight, 0);
        pt.x += offset;
        height = prevValueHeight;
    }

    private void setColor(float value, float topThreshold, float botThreshold, float topRange, float botRange)
    {
        var color = lowColor;
        if (value > topThreshold)
        {
            color = HSBColor.Lerp(lowHSB, hightHSB, (value - topThreshold) / topRange).ToColor();
            //color = Color.Lerp(lowColor, hightColor, (value-topThreshold)/topRange);
        }
        else if (value < botThreshold)
        {
            color = HSBColor.Lerp(lowHSB, hightHSB, (value - botThreshold) / botRange).ToColor();
            //color = Color.Lerp(lowColor, hightColor, (value-botThreshold)/botRange);
        }
        GL.Color(color);
    }

    //
    // Static helper Methods
    //

    private static IGameProfiler emptyProfiler = new EmptyGameProfiler();

    public static IGameProfiler Get(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            //+ Debug.LogWarning("Couldn't find GameObject '" + name + "'");
        }
        else
        {
            GameProfiler profiler = obj.GetComponent<GameProfiler>();
            if (profiler == null)
            {
                Debug.LogWarning("GameObject '" + name + "' doesn't have a GameProfiler component");
            }
            else
            {
                return profiler;
            }
        }
        return emptyProfiler;
    }

}