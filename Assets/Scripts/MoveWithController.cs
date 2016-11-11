// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: moves the attached game object using a game controller (e.g. xbox controller)

using UnityEngine;

public class MoveWithController : MonoBehaviour {

	public float SpeedScale = 10f;
    public float TurnScale = 200f;

    void Update()
	{
        float speed = Input.GetAxisRaw("Speed");
        UpdatePosition(speed * SpeedScale);

        if (speed > 0.001f)
            UpdateRotation(Input.GetAxis("Turn") * TurnScale);
        else if (speed < -0.001f)
            UpdateRotation(-Input.GetAxis("Turn") * TurnScale);
    }

    void UpdatePosition(float forward)
	{
        transform.position += forward * transform.forward * Time.deltaTime;
    }

    void UpdateRotation(float yaw)
	{
        transform.localRotation *= Quaternion.Euler(0, yaw * Time.deltaTime, 0);
        //yaw = WrapAngle(transform.localEulerAngles.y + yaw * Time.deltaTime);
        //transform.localEulerAngles = new Vector3(pitch, yaw, 0);
    }
	
    public static float WrapAngle(float angle)
	{
        return (angle + 360f) % 360f;
	}

}
