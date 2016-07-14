// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: specific implementation of a SimulationElement for pedestrians.

using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Pedestrian : SimulationElement
{
    private readonly static int forwardId = Animator.StringToHash("Forward");

    private Animator animator;
    private AudioSource audioSource;

    void OnEnable()
	{
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void UpdateMovement()
	{
        // Update the animator parameters
        float forwardAmount = targetSpeed * 0.2f;
        animator.SetFloat(forwardId, forwardAmount, 0.15f, Time.deltaTime);
        //animator.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);

        // Mute sound at low speeds
        audioSource.volume = Mathf.Clamp01(targetSpeed * 1.5f - 0.4f);
    }

}
