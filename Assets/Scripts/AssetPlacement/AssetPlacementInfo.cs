// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: scriptable object holding data necessary for asset placement:
//          position, rotation and scale

using UnityEngine;

[CreateAssetMenu(menuName = "AssetPlacement/Info")]
public class AssetPlacementInfo : ScriptableObject
{
    [System.Serializable]
    public struct Placement
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public Placement[] placements;
}
