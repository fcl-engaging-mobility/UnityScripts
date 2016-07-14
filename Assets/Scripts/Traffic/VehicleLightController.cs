// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper interface & class to control vehicle lights.

public interface IVehicleLightController
{
    void UpdateLights();
}

public class NullVehicleLightController : IVehicleLightController
{
    public void UpdateLights() { }
}
