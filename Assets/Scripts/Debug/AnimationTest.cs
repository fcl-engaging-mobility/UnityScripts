// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: sets attached SimulationElement to a constant speed for debugging purposes

using UnityEngine;

[RequireComponent(typeof(SimulationElement))]
public class AnimationTest : MonoBehaviour
{
    public float speed = 0;

    private SimulationElement simElement;
    private bool started = false;

    void Start()
    {
        simElement = GetComponent<SimulationElement>();
        started = true;
    }

    void OnValidate()
    {
        if (started)
        {
            simElement.SetSpeed(speed);
        }
    }

    void Update()
    {
        simElement.MoveToTarget();
    }

}
