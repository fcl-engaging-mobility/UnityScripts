// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: allows to setup different daylight configurations
//          and quickly switch between them

using UnityEngine;

public class LightManager : MonoBehaviour {

    [System.Serializable]
    public class LightConfig
    {
        public string name;
        public Light light;
        public Material skybox;
    }

    public int activeConfig = 0;
    public LightConfig[] configs;

    void OnValidate()
    {
        if (configs == null || configs.Length == 0)
        {
            activeConfig = 0;
            return;
        }
        if (activeConfig >= configs.Length)
        {
            activeConfig = configs.Length - 1;
            return;
        }

        foreach (var config in configs)
        {
            if (config.light != null)
            {
                config.light.enabled = false;
            }
        }

        RenderSettings.skybox = configs[activeConfig].skybox;
        if (configs[activeConfig].light != null)
        {
            configs[activeConfig].light.enabled = true;
        }
    }
}
