// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: helper class to visualize a cuboid volume

using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VolumePlaceholder : MonoBehaviour {

    public bool hideOnPlay = true;
    public bool doubleSided = true;
    private bool _doubleSided = true;

    public Vector3 size = new Vector3(1,1,1);
    public Vector3 offset = new Vector3(0, 0, 0);
    private Mesh mesh = null;

    public Vector3 Min
    {
        get
        {
            Vector3 halfSize = size * 0.5f;
            return transform.position + transform.rotation * (Vector3.Scale(offset, halfSize) - halfSize);
        }
    }
    public Vector3 Max
    {
        get
        {
            Vector3 halfSize = size * 0.5f;
            return transform.position + transform.rotation * (Vector3.Scale(offset, halfSize) + halfSize);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && hideOnPlay)
        {
            GetComponent<MeshFilter>().mesh = null;
        }
        else
        {
            if (mesh == null || doubleSided != _doubleSided)
            {
                _doubleSided = doubleSided;
                CreateCubeMesh();
            }
            else
            {
                UpdateCubeMesh();
            }
        }
    }

    void CreateCubeMesh()
    {
        var faceList = doubleSided ?
            new int[] {
                 0, 1, 2, 0, 2, 3,   4, 5, 6, 4, 6, 7,   8, 9,10, 8,10,11,
                12,13,14,12,14,15,  16,17,18,16,18,19,  20,21,22,20,22,23,
                 0, 2, 1, 0, 3, 2,   4, 6, 5, 4, 7, 6,   8,10, 9, 8,11,10,
                12,14,13,12,15,14,  16,18,17,16,19,18,  20,22,21,20,23,22
            } :
            new int[] {
                 0, 1, 2, 0, 2, 3,   4, 5, 6, 4, 6, 7,   8, 9,10, 8,10,11,
                12,13,14,12,14,15,  16,17,18,16,18,19,  20,21,22,20,22,23
            };

        mesh = new Mesh();
        mesh.name = "VolumePlaceholderMesh";
        UpdateCubeMesh();
        mesh.triangles = faceList;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().receiveShadows = false;
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void UpdateCubeMesh()
    {
        Vector3 halfSize = size * 0.5f;
        Vector3 off = Vector3.Scale(offset, halfSize);

        mesh.vertices = new Vector3[] {
            new Vector3(-halfSize.x,-halfSize.y,-halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x,-halfSize.y,-halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3( halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3( halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x,-halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y, halfSize.z) + off,
            new Vector3(-halfSize.x, halfSize.y,-halfSize.z) + off,
            new Vector3(-halfSize.x,-halfSize.y,-halfSize.z) + off
        };

        mesh.bounds = new Bounds(Vector3.zero, size);
    }

}
