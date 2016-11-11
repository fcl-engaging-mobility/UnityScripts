// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: specific implementation of a SimulationElement for 4-wheeled vehicles.

using UnityEngine;

public class Vehicle4Wheels : SimulationVehicle
{
    [System.Serializable]
    public class VariableSound
    {
        public AudioClip[] clips;
        public float minPitch = 1;
        public float maxPitch = 1;
        public float minVolume = 1;
        public float maxVolume = 1;

        public AudioClip GetClip()
        {
            return clips.Length > 0? clips[Random.Range(0, clips.Length)] : null;
        }
        public float GetPitch()
        {
            return Random.Range(minPitch, maxPitch);
        }
        public float GetVolume()
        {
            return Random.Range(minVolume, maxVolume);
        }
    }

    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform backWheels;

    [Header("Sounds")]
    public VariableSound idleSound;
    public VariableSound movingSound;
    public VariableSound hornSound;
    private AudioSource audioSource;

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
    }

    protected override void UpdateVariables()
    {
        base.UpdateVariables();
        SetWheels(frontLeftWheel != null && frontRightWheel != null && backWheels != null);
    }

    public override void InitWheels()
    {
        Vector3 rot = Vector3.zero;
        rot.x = random.Next(0, 360);
        frontLeftWheel.localEulerAngles = rot;
        rot.x = random.Next(0, 360);
        frontRightWheel.localEulerAngles = rot;
        rot.x = random.Next(0, 360);
        backWheels.localEulerAngles = rot;
    }

    protected override void UpdateWheels(float angularDistance)
    {
        frontLeftWheel.Rotate(angularDistance, 0, 0, Space.Self);
        frontRightWheel.Rotate(angularDistance, 0, 0, Space.Self);
        backWheels.Rotate(angularDistance, 0, 0, Space.Self);
    }

    protected override void OnMovementTypeChanged()
    {
        base.OnMovementTypeChanged();

        switch (movementType)
        {
            case MovementType.Idle:
                audioSource.clip = idleSound.GetClip();
                audioSource.pitch = idleSound.GetPitch();
                audioSource.volume = idleSound.GetVolume();
                audioSource.time = 0;
                audioSource.Play();
                break;
            default:
                audioSource.clip = movingSound.GetClip();
                audioSource.pitch = movingSound.GetPitch();
                audioSource.volume = movingSound.GetVolume();
                audioSource.time = 0;
                audioSource.Play();
                break;
        }
    }
    public override void SoundSignal()
    {
        if (hornSound != null && hornSound.clips.Length > 0)
        {
            audioSource.PlayOneShot(hornSound.GetClip(), hornSound.GetVolume());
        }
    }
}
