using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ucCreatePlaneVisualization
{    

    static public void CreatePlaneVisualization(int grid_size, Vector3[] pos, Vector3[] normals)
    {
        GameObject plane_root = new GameObject("PlaneRoot");
        for(int i = 0; i < pos.Length; ++i)
        {
            //plane size is 10M X 10M
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = ucCoordToUnity.F3(pos[i]);
            float scale_value = (1.0f / 10.0f) * (grid_size / 100.0f);
            plane.transform.localScale = new Vector3(scale_value, scale_value, scale_value);
            plane.transform.rotation = Quaternion.FromToRotation(Vector3.up, normals[i].normalized);
            //plane.transform.LookAt(ucCoordToUnity.F3(normals[i]));
            //plane.transform.LookAt(new Vector3(1.0f, 0.0f, 0.0f));

            plane.transform.parent = plane_root.transform;
        }
    }
}
