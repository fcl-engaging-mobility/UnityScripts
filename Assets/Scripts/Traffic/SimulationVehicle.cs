// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: base class for all vehicle elements (cyclists, motorbikes, cars, etc.)

using UnityEngine;

public abstract class SimulationVehicle : SimulationElement, IVehicleLightController, IVehicleWheelController
{
    public enum MovementType
    {
        Idle,
        Accelerating,
        Moving,
        Decelerating
    }

    [Header("Lights")]
    public MeshRenderer brakeLights;
    public int materialID = 0;
    public Material brakeLightOn;
    private Material brakeLightOff;
    //public MeshRenderer leftTurnLights;
    //public Material leftTurnLightOn;
    //private Material leftTurnLightOff;
    //public MeshRenderer rightTurnLights;
    //public Material rightTurnLightOn;
    //private Material rightTurnLightOff;

    [Header("Wheels")]
    public float wheelRadius = 0.3f;
    private float distanceToDegrees = 1;

    [Header("Movement")]
    public float maxIdleSpeed = 0.1f;
    public float accelerationThreshold = 0.01f;
    public float decelerationThreshold = -0.01f;
    protected MovementType movementType = MovementType.Idle;


    private static NullVehicleLightController nullVehicleLightController = new NullVehicleLightController();
    private IVehicleLightController lightController = nullVehicleLightController;

    private static NullVehicleWheelController nullVehicleWheelController = new NullVehicleWheelController();
    IVehicleWheelController wheelController = nullVehicleWheelController;

    //private TurningState turningState = TurningState.NotTurning;
    //private enum TurningState
    //{
    //    NotTurning,
    //    TurningLeft,
    //    TurningRight
    //}

    void Start()
    {
        UpdateVariables();
        wheelController.InitWheels();
    }

    void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            UpdateVariables();
        }
    }

    protected virtual void UpdateVariables()
    {
        distanceToDegrees = Mathf.Rad2Deg / wheelRadius;

        if (brakeLights != null)
        {
            brakeLightOff = brakeLights.materials[materialID];
            lightController = this;
        }
        else
        {
            lightController = nullVehicleLightController;
        }
    }

    public override void SetSpeed(float speed)
    {
        base.SetSpeed(speed);

        float speedDiff = 0;
        var prevMovementType = movementType;
        if (targetSpeed < maxIdleSpeed)
        {
            movementType = MovementType.Idle;
        }
        else
        {
            speedDiff = targetSpeed - previousSpeed;
            if (speedDiff > accelerationThreshold)
                movementType = MovementType.Accelerating;
            else if (speedDiff < decelerationThreshold)
                movementType = MovementType.Decelerating;
            else
                movementType = MovementType.Moving;
        }

        if (prevMovementType != movementType)
        {
            OnMovementTypeChanged();
        }
    }

    protected virtual void OnMovementTypeChanged()
    {
        lightController.UpdateLights();
    }

    protected override void UpdateMovement()
    {
        wheelController.UpdateWheels();
    }

    public abstract void InitWheels();

    public void SetWheels(bool hasWheels)
    {
        if (hasWheels)
            wheelController = this;
        else
            wheelController = nullVehicleWheelController;
    }

    public void UpdateWheels()
    {
        float angularDistance = (targetPosition - transform.localPosition).magnitude * distanceToDegrees;
        UpdateWheels(angularDistance);
    }

    protected abstract void UpdateWheels(float angularDistance);

    public void UpdateLights()
    {
        var mats = brakeLights.materials;
        mats[materialID] = movementType == MovementType.Decelerating? brakeLightOn : brakeLightOff;
        brakeLights.materials = mats;
    }

}
