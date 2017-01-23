// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a container for all the necessary data for thermal rendering

public class ThermalRenderingData<C>
    where C : struct
{
    public C[] cells;
    public uint startIndex;
    public uint endIndex;
    public CellType cellType = CellType.Billboard;
    public float minTemperature;
    public float maxTemperature;
    public uint resolution;
}
