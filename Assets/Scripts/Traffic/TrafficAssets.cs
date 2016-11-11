// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a scriptable object that stores the asset replacement rules
//          for all traffic asset types

using System;
using System.Collections.Generic;
using UnityEngine;

public enum AssetType : byte
{
    None = 0,
    MalePedestrian = 1,
    FemalePedestrian = 2,
    Pedestrian = 3,
    Cyclist = 10,
    Car1 = 20,
    Car2 = 21,
    Motorbike = 22,
    Taxi = 23,
    HGV = 30,
    Minivan = 31,
    Bus = 32,
    Bus_SBS = 33,
}

[Serializable]
public class AssetReplacement
{
    [HideInInspector]
    public string name;
    public AssetType type;
    public GameObject[] prefabs;
}

[Serializable]
public class CustomAssetReplacement
{
    [HideInInspector]
    public string name;
    public byte type;
    public GameObject[] prefabs;
}

[CreateAssetMenu(menuName = "Traffic/Assets")]
public class TrafficAssets : ScriptableObject
{
    private static readonly System.Random random = new System.Random(13);
    public GameObject defaultPrefab;

    [SerializeField]
    private AssetReplacement[] assets = new AssetReplacement[0];

    [SerializeField]
    private CustomAssetReplacement[] customAssets = new CustomAssetReplacement[0];

    public AssetReplacement[] Assets
    {
        get { return assets; }
        set
        {
            assets = value;
            UpdateDictionary();
            CheckErrors();
        }
    }

    public CustomAssetReplacement[] CustomAssets
    {
        get { return customAssets; }
        set
        {
            customAssets = value;
            UpdateDictionary();
            CheckErrors();
        }
    }

    private Dictionary<byte, GameObject[]> assetTypeToPrefab = new Dictionary<byte, GameObject[]>();

    void OnEnable()
    {
        UpdateDictionary();
    }

    void OnValidate()
    {
        UpdateDictionary();
    }

    void UpdateDictionary()
    {
        assetTypeToPrefab.Clear();
        foreach (var asset in assets)
        {
            asset.name = asset.type.ToString();
            assetTypeToPrefab.Add((byte)asset.type, asset.prefabs);
        }
        foreach (var asset in customAssets)
        {
            asset.name = "Type " + asset.type;
            assetTypeToPrefab.Add(asset.type, asset.prefabs);
        }
    }

    void CheckErrors()
    {
        string msg = GetErrorMessage();
        if (msg != null)
        {
            Debug.LogError(name + ": " + msg);
        }
    }

    public Dictionary<byte, GameObject[]> GetDictionary()
    {
        return assetTypeToPrefab;
    }

    public string GetErrorMessage()
    {
        string msg = null;
        foreach (var asset in assets)
        {
            msg = GetErrorMessage(asset.type, asset.prefabs);
            if (msg != null)
                return msg;
        }
        foreach (var asset in customAssets)
        {
            msg = GetErrorMessage(asset.type, asset.prefabs);
            if (msg != null)
                return msg;
        }
        return msg;
    }

    private string GetErrorMessage<T>(T type, GameObject[] prefabs)
    {
        foreach (var prefab in prefabs)
        {
            if (prefab == null)
            {
                return "Asset type " + type + " has an empty prefab";
            }
            else if (prefab.GetComponent<SimulationElement>() == null)
            {
                return "Prefab " + prefab.name + " doesn't have SimulationElement component";
            }
        }
        return null;
    }

    public void Check(byte[] assetTypes, bool addIfMissing)
    {
        List<CustomAssetReplacement> newCustomAssets = new List<CustomAssetReplacement>(customAssets);
        foreach (var assetType in assetTypes)
        {
            if (assetType == (byte)AssetType.None)
                continue;

            bool found = false;
            foreach (var asset in assets)
            {
                if (assetType == (byte)asset.type)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                foreach (var v in newCustomAssets)
                {
                    if (assetType == v.type)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    string msg = "Asset type " + assetType + " was not found in traffic assets.";
                    if (addIfMissing)
                        msg += " Adding it to the custom assets.";
                    Debug.LogWarning(msg);

                    if (addIfMissing)
                    {
                        newCustomAssets.Add(new CustomAssetReplacement
                        {
                            type = assetType,
                            prefabs = new GameObject[] { defaultPrefab }
                        });
                    }
                }
            }
        }
        customAssets = newCustomAssets.ToArray();

        UpdateDictionary();
        CheckErrors();
    }

    public GameObject Instantiate(byte assetType, Transform parent)
    {
        GameObject[] gos = assetTypeToPrefab[assetType];

        int index = random.Next(0, gos.Length);
        return Instantiate(gos[index], parent) as GameObject;
    }
}
