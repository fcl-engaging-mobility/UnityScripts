// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to test/debug SimulationElement's movement.

using UnityEngine;

public class SpeedProfiler : MonoBehaviour
{
    public float maxSpeed = 15f;
    public bool isSimulationElement = false;

    private IGameProfiler profiler;
    private Vector3 pos = Vector3.zero;
    private SimulationElement simElement = null;
    private float prevSpeed = 0;

    void Start()
    {
        pos = transform.position;
    }

    void OnEnable()
    {
        profiler = GameProfiler.Get("GameProfiler");
        if (profiler.GetCount() < 19)
        {
            pos = transform.position;

            string id = name + " speed";
            var settings = profiler.Add(id);
            settings.min = 0;
            settings.max = maxSpeed;
            settings.height = 201;

            if (isSimulationElement)
            {
                id = name + " target speed";
                settings = profiler.Add(id);
                settings.min = 0;
                settings.max = maxSpeed;
                settings.height = 101;

                id = name + " acceleration";
                settings = profiler.Add(id);
                settings.min = -0.2f;
                settings.max = 0.2f;
                settings.height = 101;

                simElement = GetComponent<SimulationElement>();
            }
        }
        else
        {
            profiler = null;
            enabled = false;
        }
    }

    void OnDestroy()
    {
        if (profiler != null)
        {
            profiler.Remove(name);
        }
    }

    void LateUpdate()
    {
        float speed = (transform.position - pos).magnitude / Time.deltaTime;
        profiler.Update(name + " speed", speed);
        if (isSimulationElement)
        {
            profiler.Update(name + " target speed", simElement.TargetSpeed);
            profiler.Update(name + " acceleration", simElement.TargetSpeed - prevSpeed);
            prevSpeed = simElement.TargetSpeed;
        }

        pos = transform.position;
    }

}
