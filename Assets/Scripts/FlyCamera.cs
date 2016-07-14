// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: when attached to a Camera it allows to control it with keyboard and
//          mouse similarly to Unity's editor camera.

using UnityEngine;
using UnityEngine.EventSystems;

public class FlyCamera : MonoBehaviour {

	public float HeadingRotationScale = 200.0f;
	public float PicthRotationScale = 200.0f;

	public float StrafeSpeed = 1.5f;
	public float MoveForwardSpeed = 1.5f;

	public float HorizontalPanScale = 3.0f;
	public float VerticalPanScale = 3.0f;

    public float HorizontalOrbitScale = 3.0f;
    public float VerticalOrbitScale = 3.0f;

    public float ZoomScale = 3f;

    private Vector3 target;
    private float distanceToTarget = 20f;

    private bool isDragging = false;

    void OnEnable()
    {
        target = transform.position + transform.forward * distanceToTarget;
    }

    void Update()
	{
		// Keyboard Input
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
			Strafe(-StrafeSpeed);
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
			Strafe(StrafeSpeed);
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
			MoveForwards(MoveForwardSpeed);
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
			MoveForwards(-MoveForwardSpeed);

        bool isPointerInUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            isDragging = !isPointerInUI;
        }
        if (Input.GetMouseButtonUp(0) && Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        // Mouse Input
        if (isDragging && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
        {
            float deltaX = Input.GetAxis("Mouse X");
            float deltaY = Input.GetAxis("Mouse Y");
            if (Input.GetMouseButton(0))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        Strafe(-deltaX * HorizontalPanScale);
                        ChangeHeight(-deltaY * VerticalPanScale);
                    }
                    else
                    {
                        Orbit(-deltaY * HorizontalOrbitScale, deltaX * VerticalOrbitScale);
                    }
                }
            }
            else if (Input.GetMouseButton(1))
            {
                ChangeHeading(deltaX * HeadingRotationScale);
                ChangePitch(-deltaY * PicthRotationScale);
            }
        }
        else
        {
            float deltaZ = Input.GetAxis("Mouse ScrollWheel");
            if (deltaZ != 0)
            {
                distanceToTarget = Mathf.Max(3f, distanceToTarget - deltaZ * ZoomScale * distanceToTarget);
                transform.position = target - transform.forward * distanceToTarget;
            }
        }
    }

	void MoveForwards(float aVal)
	{
		transform.position += aVal * transform.forward * Time.deltaTime;
        target = transform.position + transform.forward * distanceToTarget;
    }
	void Strafe(float aVal)
	{
		transform.position += aVal * transform.right * Time.deltaTime;
        target = transform.position + transform.forward * distanceToTarget;
    }
	void ChangeHeight(float aVal)
	{
		transform.position += aVal * transform.up * Time.deltaTime;
        target = transform.position + transform.forward * distanceToTarget;
    }
	void ChangeHeading(float aVal)
	{
		float hdg = transform.localEulerAngles.y + aVal * Time.deltaTime;
		WrapAngle(ref hdg);
		transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, hdg, 0);
        target = transform.position + transform.forward * distanceToTarget;
    }
	void ChangePitch(float aVal)
	{
		float pitch = transform.localEulerAngles.x + aVal * Time.deltaTime;
		WrapAngle(ref pitch);
		transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0);
        target = transform.position + transform.forward * distanceToTarget;
    }
    public void Orbit(float xVal, float yVal)
    {
        Vector3 target = transform.position + transform.forward.normalized * distanceToTarget;
        Quaternion quat = transform.rotation * Quaternion.Euler(xVal, yVal, 0);
        transform.rotation = Quaternion.Euler(quat.eulerAngles.x, quat.eulerAngles.y, 0);
        transform.position = target - transform.forward.normalized * distanceToTarget;
    }
	public static void WrapAngle(ref float angle)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
	}

}
