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

    public float InitialDistanceToTarget = 20f;
    public float MinDistanceToTarget = 2f;

    private Vector3 target;
    private float distanceToTarget = 20f;

    private bool isDragging = false;
    private bool isOrbiting = false;
    private bool useLocalMovement = false;

    private Transform camTransform = null;

    void OnEnable()
    {
        camTransform = useLocalMovement ? transform.GetChild(0) : transform;
        distanceToTarget = InitialDistanceToTarget;
        UpdateTarget();
    }

    void Update()
	{
        // Game Controller
        float speedMultiplier = Input.GetKey(KeyCode.LeftShift)? 2f : 1f;
        float strafe = Input.GetAxis("Strafe") * StrafeSpeed * speedMultiplier;
        float forward = Input.GetAxis("Forward") * MoveForwardSpeed * speedMultiplier;
        if (Input.GetKeyDown(KeyCode.JoystickButton9))
            isOrbiting = !isOrbiting;
        if (Input.GetKeyDown(KeyCode.JoystickButton8))
        {
            useLocalMovement = !useLocalMovement;
            camTransform = useLocalMovement ? transform.GetChild(0) : transform;
            UpdateTarget();
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            transform.localRotation = Quaternion.identity;
            UpdateTarget();
        }

        if (isOrbiting)
        {
            Orbit(Input.GetAxis("OrbitY") * HorizontalOrbitScale, Input.GetAxis("OrbitX") * VerticalOrbitScale);
        }
        else
        {
            ChangeHeading(Input.GetAxis("OrbitX") * HeadingRotationScale);
            ChangePitch(Input.GetAxis("OrbitY") * PicthRotationScale);
        }

        Strafe(strafe);
        MoveForwards(forward);

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
            float deltaX = -Input.GetAxis("Mouse X");
            float deltaY = -Input.GetAxis("Mouse Y");
            if (Input.GetMouseButton(0))
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        Strafe(deltaX * HorizontalPanScale);
                        ChangeHeight(deltaY * VerticalPanScale);
                    }
                    else
                    {
                        Orbit(-deltaY * HorizontalOrbitScale, deltaX * VerticalOrbitScale);
                    }
                }
            }
            else if (Input.GetMouseButton(1))
            {
                ChangeHeading(-deltaX * HeadingRotationScale);
                ChangePitch(deltaY * PicthRotationScale);
            }
        }
        else
        {
            float deltaZ = Input.GetAxis("Mouse ScrollWheel");
            if (deltaZ != 0)
            {
                distanceToTarget = Mathf.Max(MinDistanceToTarget, distanceToTarget - deltaZ * ZoomScale * distanceToTarget);
                camTransform.position = target - camTransform.forward * distanceToTarget;
            }
        }
    }

	void MoveForwards(float aVal)
	{
        transform.position += aVal * camTransform.forward * Time.deltaTime;
        UpdateTarget();
    }
	void Strafe(float aVal)
	{
        transform.position += aVal * camTransform.right * Time.deltaTime;
        UpdateTarget();
    }
	void ChangeHeight(float aVal)
	{
        transform.position += aVal * camTransform.up * Time.deltaTime;
        UpdateTarget();
    }
	void ChangeHeading(float aVal)
	{
		float hdg = transform.localEulerAngles.y + aVal * Time.deltaTime;
		WrapAngle(ref hdg);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, hdg, 0);
        UpdateTarget();
    }
	void ChangePitch(float aVal)
	{
		float pitch = transform.localEulerAngles.x + aVal * Time.deltaTime;
		WrapAngle(ref pitch);
        transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0);
        UpdateTarget();
    }
    public void Orbit(float pitch, float yaw)
    {
        Vector3 target = transform.position + camTransform.forward.normalized * distanceToTarget;
        Quaternion quat = transform.rotation * Quaternion.Euler(-pitch, -yaw, 0);
        transform.rotation = Quaternion.Euler(quat.eulerAngles.x, quat.eulerAngles.y, 0);
        transform.position = target - camTransform.forward.normalized * distanceToTarget;
    }
    private void UpdateTarget()
    {
        target = transform.position + camTransform.forward * distanceToTarget;
    }
    public static void WrapAngle(ref float angle)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
	}

}
