// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a TrafficLight is a group of Lights (e.g. red, amber, green)
//          and each Light represents a Renderer that can toggle beteween the
//          ON and OFF materials.

using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour, Pulsator
{
    [System.Serializable]
    public class LightActivation
    {
        public int id;
        public bool pulsate;
    }

    [System.Serializable]
    public class Light
    {
        public int id;
        public string name;
        public Renderer renderer;
        public Light light;
        public Material lightOn;
        public Material lightOff;
        [HideInInspector] public bool isOn;
    }

    [SerializeField]
    private Light[] lights = new Light[] {
        new Light { id = 1, name = "Red" },
        new Light { id = 2, name = "Yellow" },
        new Light { id = 3, name = "Green" },
    };

    private List<Light> pulsatingLights = new List<Light>();

    void Awake()
    {
        // Safety checks
        foreach (var light in lights)
        {
            if (light.renderer == null)
            {
                Debug.LogError(name + " doesn't have a renderer for light " + light.id + ": " + light.name, this);
                enabled = false;
            }
            if (light.lightOn == null)
            {
                Debug.LogWarning(name + " doesn't have a ON material for light " + light.id + ": " + light.name, this);
            }
            if (light.lightOff == null)
            {
                Debug.LogWarning(name + " doesn't have a OFF material for light " + light.id + ": " + light.name, this);
            }
        }
    }

    void Start()
    {
        // Turn all lights off
        foreach (var light in lights)
        {
            EnableLight(light, false);
        }
    }

    public void ChangeLights(LightActivation[] activeLights)
    {
        TimeController.UnregisterPulsator(this);
        pulsatingLights.Clear();

        foreach (var light in lights)
        {
            bool isOn = false;
            foreach (LightActivation activeLight in activeLights)
            {
                if (activeLight.id == light.id)
                {
                    if (activeLight.pulsate)
                    {
                        pulsatingLights.Add(light);
                    }
                    isOn = true;
                    break;
                }
            }

            if (light.isOn != isOn)
            {
                EnableLight(light, isOn);
            }
        }

        if (pulsatingLights.Count > 0)
        {
            TimeController.RegisterPulsator(this);
        }
    }

    private void EnableLight(Light light, bool enable)
    {
        light.renderer.material = enable ? light.lightOn : light.lightOff;
        light.isOn = enable;
    }

    public void Pulse()
    {
        foreach (var light in pulsatingLights)
        {
            EnableLight(light, !light.isOn);
        }
    }
}
