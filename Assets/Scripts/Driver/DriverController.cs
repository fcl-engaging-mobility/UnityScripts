/**
 * Copyright(C) 2016 Singapore ETH Centre, Future Cities Laboratory
 * All rights reserved.
 * 
 * This software may be modified and distributed under the terms
 * of the MIT license.See the LICENSE file for details.
 * 
 * Author:  Filip Schramka (schramka@arch.ethz.ch)
 * Summary: This class controlls the avatar. Movement, animations, tilting
 *          is controlled.
 * 
 */

//#define USING_STEAMVR

using System;
using UnityEngine;
using System.Collections.Generic;

public class DriverController : MonoBehaviour
{
    [Header("External device controller")]
    [Space(10)]
#if USING_STEAMVR
    [Tooltip("The tracked object for steering")]
    public SteamVR_TrackedController trackedSteering;
#endif
    [Tooltip("The serial controller needed for axiom data")]
    public AxiomSerialController axiomserialController;
    [Tooltip("The bluefruit controller needed for pedal data")]
    public AbstractBluefruitController bfPedalController;
    [Tooltip("The bluefruit controller needed for pedal data")]
    public AbstractBluefruitController bfWheelController;
    [Tooltip("The bluefruit controller needed for tilting data")]
    public AbstractBluefruitController bfTiltController;

    [Header("Animator layer names")]
    [Space(10)]
    [Tooltip("The animator layer name for the pedaling animation")]
    public string pedalingLayer = "Pedaling";
    [Tooltip("The animator layer name for the right turn animation")]
    public string rightTurnLayer = "SteeringRight";
    [Tooltip("The animator layer name for the left turn animation")]
    public string leftTurnLayer = "SteeringLeft";

    [Header("3D model wheels")]
    [Space(10)]
    [Tooltip("The front wheel of the bike")]
    public Transform frontWheel;
    [Tooltip("The rear wheel of the bike")]
    public Transform rearWheel;

    [Header("Physical settings")]
    [Space(10)]
    [Tooltip("The radius of the bike wheels")]
    public float wheelRadius = 0.3075f;
    [Tooltip("Distance between both wheels' center")]
    public float wheelbase = 1.06f;

    [Header("Wheel sensor settings")]
    [Space(10)]
    [Tooltip("Defines the smoothing method")]
    public SmoothingMethod smoothingMethod;
    [Tooltip("Smoothing factor - higher values create higher delay")]
    public int wheelSmoothFactor = 20;

    [Header("Pedal sensor settings")]
    [Space(10)]
    [Tooltip("Clockwise or counter clockwise rotation")]
    public bool inversePedalRotation = true;
   
    [Header("Tilt sensor settings")]
    [Space(10)]
    [Tooltip("Multiplied by the sensor angle")]
    public float tiltFactor = 2;

    [Header("Resistance settings")]
    [Space(10)]
    [Tooltip("Defines if resistance is used or not")]
    public bool useResistance = false;
    [Tooltip("Defines the speed threshold, beneath the resistance turns to 0")]
    public float resistanceThreshold = 0.2f;

    [Header("Debug")]
    [Space(20)]
    public bool useGamePad = false;
    public bool moveForward = true;


    public enum Layers
    {
        Pedaling,
        RightTurn,
        LeftTurn
    }

    public enum SmoothingMethod
    {
        Average,
        Median,
        None
    }

    const float MAX_STEERING_ANGLE = 45f;
    const float INV_MAX_STEERING_ANGLE = 1f / 45f;

    Animator anim;
    AxiomDataPackage data;

    Dictionary<Layers, int> layerIndices;

    float sWheelExtend;
    float sWheelPerDegree;

    float oldWheelAngle;

    float[] wheelSmooth;
    int wheelSmoothPtr;
    float invWheelSmoothFactor;

    IGameProfiler profiler;

    void OnEnable()
    {
        // init the serial connections
        axiomserialController.Init();
        bfPedalController.Init();
        bfWheelController.Init();
        bfTiltController.Init();

        layerIndices = new Dictionary<Layers, int>();

        smoothingMethod = SmoothingMethod.Average;
        wheelSmooth = new float[wheelSmoothFactor];
        invWheelSmoothFactor = 1f / wheelSmoothFactor;

        //- debug
        
        profiler = GameProfiler.Get("GameProfiler");
        var settings = profiler.Add("Raw");
        settings.min = 0;
        settings.max = 60;
        var settings2 = profiler.Add("Median");
        settings2.min = 0;
        settings2.max = 60;
        var settings3 = profiler.Add("Mean");
        settings3.min = 0;
        settings3.max = 60;
        
    }

    void OnDisable()
    {
        axiomserialController.Shutdown();
        bfPedalController.Shutdown();
        bfWheelController.Shutdown();
        bfTiltController.Shutdown();
    }

    void OnValidate()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
            return;

        float[] newWheelSmooth = new float[wheelSmoothFactor];
        Array.Copy(wheelSmooth, newWheelSmooth, Mathf.Min(wheelSmoothFactor, wheelSmooth.Length));
        wheelSmooth = newWheelSmooth;
        wheelSmoothPtr %= wheelSmoothFactor;
        invWheelSmoothFactor = 1f / wheelSmoothFactor;
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        layerIndices.Add(Layers.Pedaling, anim.GetLayerIndex(pedalingLayer));
        layerIndices.Add(Layers.RightTurn, anim.GetLayerIndex(rightTurnLayer));
        layerIndices.Add(Layers.LeftTurn, anim.GetLayerIndex(leftTurnLayer));

        anim.SetLayerWeight(layerIndices[Layers.RightTurn], 0.0f);
        anim.SetLayerWeight(layerIndices[Layers.LeftTurn], 0.0f);

        sWheelExtend = 2.0f * Mathf.PI * wheelRadius;
        sWheelPerDegree = sWheelExtend / 360.0f;
    
        if(axiomserialController.isActive)
            axiomserialController.RequestResistance(0);
    }

    void Update()
    {
        // axiom data
        data = axiomserialController.getAxiomData();

        // get angles from sensors
        float backWheelAngle = GetRelativeRearWheelAngle(smoothingMethod);
        float pedalAngle = bfPedalController.GetAbsoluteAngle();

#if USING_STEAMVR
        float steeringAngle = (trackedSteering.transform.eulerAngles.y + 180f) % 360f - 180f;
#else
        float steeringAngle = 0;
#endif

        float leaningAngle = MathUtils.GetSignedAngleDiff(bfTiltController.GetAbsoluteAngle(), 360f) * tiltFactor;
            
        // Steering weight (for animation) from -1 to 1
        float steeringWeight = Mathf.Clamp(steeringAngle * INV_MAX_STEERING_ANGLE, -1, 1);
        
        // override data with gamePad data
        if (useGamePad)
        {
            steeringWeight = Input.GetAxis("Turn");
            steeringAngle = steeringWeight * MAX_STEERING_ANGLE;
            backWheelAngle = Input.GetAxis("Speed") * 10;
            leaningAngle = -Input.GetAxis("OrbitX") * 25f;
        }
     
        // update animations
        UpdateWheelAnimation(backWheelAngle);
        UpdatePedalAnimation(pedalAngle);
        UpdateSteeringAnimation(steeringWeight);

        float distanceTraveled = backWheelAngle * sWheelPerDegree;
        float wheelSpeed = (distanceTraveled / Time.deltaTime) * 3.6f;

        // Change the bicycle's Y position and pitch so that wheels always make contact with the ground
        float slope = AdjustBicycleToGround(leaningAngle);

        if (useResistance)
            AdjustAxiomResistance(wheelSpeed, slope);

        // move forward
        if (moveForward)
            Move(distanceTraveled, steeringAngle * Mathf.Deg2Rad, leaningAngle * Mathf.Deg2Rad);

        // Reset
#if USING_STEAMVR
        if (trackedSteering.gripped || Input.GetKeyDown(KeyCode.R))
#else
        if(Input.GetKeyDown(KeyCode.R))
#endif
        {
            UnityEngine.VR.InputTracking.Recenter();
#if USING_STEAMVR
            Valve.VR.OpenVR.System.ResetSeatedZeroPose();
            Valve.VR.OpenVR.Compositor.SetTrackingSpace(Valve.VR.ETrackingUniverseOrigin.TrackingUniverseSeated);
#endif
            bfPedalController.SetReference();
            bfTiltController.SetReference();
        }
    }

    void UpdateWheelAnimation(float backWheelAngle)
    {
        //+ TODO: Wheels need to rotate at individual speeds
        rearWheel.localRotation = Quaternion.Euler(0.0f, 0.0f, rearWheel.localRotation.eulerAngles.z + backWheelAngle);
        frontWheel.localRotation = Quaternion.Euler(0.0f, 0.0f, rearWheel.localRotation.eulerAngles.z + backWheelAngle);
    }

    void UpdatePedalAnimation(float pedalAngle)
    {
        anim.speed = 1f;
        float normAngle;

        if (inversePedalRotation)
            normAngle = 1f - (pedalAngle / 360f);
        else
            normAngle = (pedalAngle / 360f);

        anim.Play("Pedaling", layerIndices[Layers.Pedaling], normAngle);
        Invoke("StopAnimation", normAngle + Time.deltaTime);

    }

    void UpdateSteeringAnimation(float steeringWeight)
    {
        if (steeringWeight > 0.0f)
        {
            anim.SetLayerWeight(layerIndices[Layers.LeftTurn], 0.0f);
            anim.SetLayerWeight(layerIndices[Layers.RightTurn], steeringWeight);
        }
        else
        {
            anim.SetLayerWeight(layerIndices[Layers.RightTurn], 0.0f);
            anim.SetLayerWeight(layerIndices[Layers.LeftTurn], -steeringWeight);
        }
    }

    void StopAnimation()
    {
        anim.speed = 0f;
    }

    float GetRelativeRearWheelAngle(SmoothingMethod sMethod)
    {
        float newWheelAngle = bfWheelController.GetAbsoluteAngle();
        float ret = MathUtils.GetSignedAngleDiff(newWheelAngle, oldWheelAngle);

        //- Debug
        
        profiler.Update("Raw", ret);
        profiler.Update("Mean", GetAverage());
        profiler.Update("Median", GetMedian());
          

        oldWheelAngle = newWheelAngle;

        if (sMethod == SmoothingMethod.None)
            return ret;
        else
            return LowPass(ret, sMethod);
    }

    float LowPass(float newVal, SmoothingMethod sMethod)
    {
        wheelSmooth[wheelSmoothPtr] = newVal;

        float ret;

        if (sMethod == SmoothingMethod.Average)
            ret = GetAverage();
        else if (sMethod == SmoothingMethod.Median)
            ret = GetMedian();
        else
        {
            Debug.LogError("Wrong SmoothingMethod: Return 0");
            ret = 0.0f;
        }

        wheelSmoothPtr = ++wheelSmoothPtr % wheelSmoothFactor;

        return ret;
    }

    float GetAverage()
    {
        float ave = 0.0f;
        
        for (int i = wheelSmoothFactor - 1; i >= 0; --i)
        {
            ave += wheelSmooth[i];
        }
        ave *= invWheelSmoothFactor;

        return ave;
    }

    float GetMedian()
    {
        float[] tmp = new float[wheelSmooth.Length];
        Array.Copy(wheelSmooth, tmp, tmp.Length);
        Array.Sort(tmp);

        return tmp.Length % 2 != 0 ? tmp[tmp.Length / 2] : tmp[tmp.Length / 2] + tmp[tmp.Length / 2 + 1];
    }

    void Move(float distance, float steeringAngle, float leaningAngle)
    {

        if (Mathf.Abs(steeringAngle) > 0.0017f)   // 0.1 degree
        {
            float rearWheelTurningRadius = wheelbase / Mathf.Tan(steeringAngle);
            
            // Decrease turning radius by leaning angle
            rearWheelTurningRadius *= Mathf.Cos(leaningAngle * Mathf.Deg2Rad);

            // Create a flat right vector (no Y component)
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();
            if (right.x * transform.right.x < 0)
                right *= -1;

            // Find rotation center
            Vector3 centerToRearWheel = right * rearWheelTurningRadius;
            Vector3 rotationCenter = transform.position;
            rotationCenter += centerToRearWheel;
            Debug.DrawLine(transform.position, rotationCenter, Color.blue);

            // Create rotation
            float rotationDeg = distance / rearWheelTurningRadius * Mathf.Rad2Deg;
            Quaternion qRotation = Quaternion.AngleAxis(rotationDeg, Vector3.up);

            // Update position and rotation
            Vector3 rearWheelNewPosition = rotationCenter - qRotation * centerToRearWheel;
            Debug.DrawLine(rotationCenter, rearWheelNewPosition, Color.red);

            transform.localRotation *= qRotation;

            transform.position = rearWheelNewPosition;
        }
        else
        {
            // Angle between front wheel's direction and rear wheel's direction is almost 0
            transform.position += transform.forward * distance;
        }
    }

    float AdjustBicycleToGround(float leaningAngle)
    {
        RaycastHit hit;
        Vector3 newRearPosition = transform.position;
        Vector3 newFrontPosition = newRearPosition + transform.forward * wheelbase;
        Debug.DrawRay(transform.position, transform.forward, Color.yellow);

        if (Physics.Raycast(newRearPosition + transform.up * 2, transform.up * -1, out hit, 10f))
        {
            newRearPosition.y = hit.point.y;
        }

        if (Physics.Raycast(newFrontPosition + transform.up * 2, transform.up * -1, out hit, 10f))
        {
            newFrontPosition.y = hit.point.y;
        }

        transform.position = newRearPosition;

        float slopeAngle = Mathf.Asin(Mathf.Clamp((newFrontPosition.y - newRearPosition.y) / wheelbase, -1, 1)) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(-slopeAngle, transform.eulerAngles.y, leaningAngle);

        // return the slope between 0 and 1 
        return Mathf.Clamp((newFrontPosition.y - newRearPosition.y) / (Mathf.Cos(Mathf.Clamp(slopeAngle, -1f, 1f)) * wheelbase), -1, 1);
    }

    void AdjustAxiomResistance(float speed, float slope)
    {
        if (speed > resistanceThreshold && slope > 0)
        {
            axiomserialController.RequestResistance((int)(slope * 255f));
        }
        else
            axiomserialController.RequestResistance(0);
    }
}
