// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: removes the pitch and roll from the game-object's rotation
//          to keep it aligned with the ground

using UnityEngine;

public class FlatCamera : MonoBehaviour
{
    Vector3 initial;

    void OnEnable()
    {
        initial = transform.localPosition;
    }
    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.position = transform.parent.position + Quaternion.Euler(transform.parent.eulerAngles.x, transform.parent.eulerAngles.y, 0f) * initial;
    }
}
