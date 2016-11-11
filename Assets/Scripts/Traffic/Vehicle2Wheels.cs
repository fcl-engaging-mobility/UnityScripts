// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: specific implementation of a SimulationElement for 2-wheeled vehicles.

using UnityEngine;

public class Vehicle2Wheels : SimulationVehicle
{
    public Transform frontWheel;
    public Transform backWheel;

    [Header("Sounds")]
    public AudioClip movingSound;
    public AudioClip breakingSound;
    public AudioClip bellSound;
    private AudioSource audioSource;

    private static readonly float LoopLength = 0.4f;
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    //private float leaning = 0;

    private Animator animator;

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void UpdateVariables()
    {
        base.UpdateVariables();
        SetWheels(frontWheel != null && backWheel != null);
    }

    public override void InitWheels()
    {
        Vector3 rot = Vector3.zero;
        rot.x = random.Next(0, 360);
        frontWheel.localEulerAngles = rot;
        rot.x = random.Next(0, 360);
        backWheel.localEulerAngles = rot;
    }

    //protected override void UpdateMovement()
    //{
    //    base.UpdateMovement();

    //    float angle = (targetRotation * Quaternion.Inverse(transform.localRotation)).eulerAngles.y;
    //    angle = (angle + 180) % 360 - 180;
    //    if (Mathf.Abs(angle) > 5)
    //    {
    //        float radius = (targetPosition - transform.localPosition).magnitude * 0.5f / Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
    //        angle = Mathf.Atan2(targetSpeed * targetSpeed, radius * 9.8f) * Mathf.Rad2Deg;
    //        //angle *= targetSpeed;
    //        angle = Mathf.Clamp(angle * Globals.TwoWheelRotationScale, -45, 45);
    //    }
    //    leaning = Mathf.Lerp(leaning, angle, 0.05f);

    //    Vector3 euler = targetRotation.eulerAngles;
    //    euler.z = leaning;
    //    targetRotation.eulerAngles = euler;
    //}

    protected override void UpdateMovement()
    {
        base.UpdateMovement();

        if (animator)
        {
            // Update the animator parameters
            float forwardAmount = targetSpeed * 0.2f;
            animator.SetFloat(SpeedId, forwardAmount);
            //animator.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);
        }
    }

    public override void SetSpeed(float speed)
    {
        base.SetSpeed(speed);

        if (audioSource && audioSource.clip == breakingSound)
        {
            UpdateBrakeSound();
        }
    }

    protected override void OnMovementTypeChanged()
    {
        base.OnMovementTypeChanged();

        if (audioSource)
        {
            switch (movementType)
            {
                case MovementType.Accelerating:
                    audioSource.clip = movingSound;
                    audioSource.time = 0;
                    audioSource.Play();
                    break;
                case MovementType.Decelerating:
                    audioSource.clip = breakingSound;
                    UpdateBrakeSound();
                    audioSource.Play();
                    break;
                case MovementType.Idle:
                    audioSource.Stop();
                    break;
            }
        }
    }

    protected override void UpdateWheels(float angularDistance)
    {
        frontWheel.Rotate(angularDistance, 0, 0, Space.Self);
        backWheel.Rotate(angularDistance, 0, 0, Space.Self);
    }

    private void UpdateBrakeSound()
    {
        float startLoop = Mathf.Max(audioSource.clip.length * (1f - targetSpeed * 0.09f), 0);
        float endLoop = startLoop + LoopLength;
        if (audioSource.time < startLoop || audioSource.time > endLoop)
        {
            audioSource.time = startLoop;
        }
    }

    public override void SoundSignal()
    {
        if (bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }
    }
}
