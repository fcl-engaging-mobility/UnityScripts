// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: loads and stores asset placement data, and cann be setup with
//          different placement modes that allow for randomized asset placement

using UnityEngine;

public class AssetPlacement : MonoBehaviour
{
    public enum Mode
    {
        None,
        Original,
        Fixed,
        Offset,
        RangedRandom,
        OffsetRangedRandom,
    }

    public AssetPlacementInfo placementInfo;

    public GameObject prefab;

    public Mode positionMode = Mode.Original;
    public Mode rotationMode = Mode.Original;
    public Mode scaleMode = Mode.Original;

    [HideInInspector]
    public Vector3 positionOffset;
    [HideInInspector]
    public Vector3 positionMin;
    [HideInInspector]
    public Vector3 positionMax;

    [HideInInspector]
    public Vector3 rotationOffset;
    [HideInInspector]
    public Vector3 rotationMin;
    [HideInInspector]
    public Vector3 rotationMax;

    [HideInInspector]
    public Vector3 scaleOffset;
    [HideInInspector]
    public Vector3 scaleMin;
    [HideInInspector]
    public Vector3 scaleMax;

    public void Load()
    {
        if (placementInfo == null || placementInfo.placements == null)
        {
            Debug.LogError("No asset placement info was found");
            return;
        }
        if (prefab == null)
        {
            Debug.LogError("Prefab hasn't been assigned");
            return;
        }

        Clear();

        Vector3 offset;
        Quaternion rotOffset = Quaternion.Euler(rotationOffset);
        int count = placementInfo.placements.Length;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(prefab, transform) as GameObject;
            go.name = prefab.name + "_" + i;
            switch (scaleMode)
            {
                case Mode.Original:
                    go.transform.localScale = placementInfo.placements[i].scale;
                    break;
                case Mode.Fixed:
                    go.transform.localScale = scaleOffset;
                    break;
                case Mode.Offset:
                    go.transform.localScale = Vector3.Scale(placementInfo.placements[i].scale, scaleOffset);
                    break;
                case Mode.RangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(scaleMin.x, scaleMax.x, Random.value),
                        Mathf.Lerp(scaleMin.y, scaleMax.y, Random.value),
                        Mathf.Lerp(scaleMin.z, scaleMax.z, Random.value));
                    go.transform.localScale = offset;
                    break;
                case Mode.OffsetRangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(scaleMin.x, scaleMax.x, Random.value),
                        Mathf.Lerp(scaleMin.y, scaleMax.y, Random.value),
                        Mathf.Lerp(scaleMin.z, scaleMax.z, Random.value));
                    go.transform.localScale = Vector3.Scale(placementInfo.placements[i].scale, offset);
                    break;
            }
            switch (rotationMode)
            {
                case Mode.Original:
                    go.transform.localRotation = placementInfo.placements[i].rotation;
                    break;
                case Mode.Fixed:
                    go.transform.localRotation = rotOffset;
                    break;
                case Mode.Offset:
                    go.transform.localRotation = placementInfo.placements[i].rotation * rotOffset;
                    break;
                case Mode.RangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(rotationMin.x, rotationMax.x, Random.value),
                        Mathf.Lerp(rotationMin.y, rotationMax.y, Random.value),
                        Mathf.Lerp(rotationMin.z, rotationMax.z, Random.value));
                    go.transform.localRotation = Quaternion.Euler(offset);
                    break;
                case Mode.OffsetRangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(rotationMin.x, rotationMax.x, Random.value),
                        Mathf.Lerp(rotationMin.y, rotationMax.y, Random.value),
                        Mathf.Lerp(rotationMin.z, rotationMax.z, Random.value));
                    go.transform.localRotation = placementInfo.placements[i].rotation * Quaternion.Euler(offset);
                    break;
            }
            switch (positionMode)
            {
                case Mode.Original:
                    go.transform.localPosition = placementInfo.placements[i].position;
                    break;
                case Mode.Fixed:
                    go.transform.localPosition = positionOffset;
                    break;
                case Mode.Offset:
                    go.transform.localPosition = placementInfo.placements[i].position + positionOffset;
                    break;
                case Mode.RangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(positionMin.x, positionMax.x, Random.value),
                        Mathf.Lerp(positionMin.y, positionMax.y, Random.value),
                        Mathf.Lerp(positionMin.z, positionMax.z, Random.value));
                    go.transform.localPosition = offset;
                    break;
                case Mode.OffsetRangedRandom:
                    offset = new Vector3(
                        Mathf.Lerp(positionMin.x, positionMax.x, Random.value),
                        Mathf.Lerp(positionMin.y, positionMax.y, Random.value),
                        Mathf.Lerp(positionMin.z, positionMax.z, Random.value));
                    go.transform.localPosition = placementInfo.placements[i].position + offset;
                    break;
            }
        }
    }

    public void Save()
    {
        int count = transform.childCount;
        if (placementInfo.placements == null || count != placementInfo.placements.Length)
        {
            placementInfo.placements = new AssetPlacementInfo.Placement[count];
        }
        for (int i = 0; i < count; i++)
        {
            Transform t = transform.GetChild(i);
            placementInfo.placements[i].position = t.localPosition;
            placementInfo.placements[i].rotation = t.localRotation;
            placementInfo.placements[i].scale = t.localScale;
        }
    }

    public void Clear()
    {
        int childs = transform.childCount;
        for (int i = childs - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

}
