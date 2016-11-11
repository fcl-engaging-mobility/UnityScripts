// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: class that controls time. Oher components should get the time from here.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Pulsator
{
    void Pulse();
}

public class TimeController : MonoBehaviour
{
    public float time = 0;
    public float timeScale = 1;
    public bool pulsate = false;

    private static WaitForSeconds wait = new WaitForSeconds(0.5f);
    private static List<Pulsator> pulsators = new List<Pulsator>();

    void Start()
    {
        if (pulsate)
        {
            StartCoroutine(Pulsate());
        }
    }

    void Update()
    {
        time += Time.deltaTime * timeScale;
    }

    public static void RegisterPulsator(Pulsator pulsator)
    {
        pulsators.Add(pulsator);
    }

    public static void UnregisterPulsator(Pulsator pulsator)
    {
        pulsators.Remove(pulsator);
    }

    private IEnumerator Pulsate()
    {
        while (true)
        {
            yield return wait;
            for (int i = 0; i < pulsators.Count; i++)
            {
                pulsators[i].Pulse();
            }
        }
    }
}
