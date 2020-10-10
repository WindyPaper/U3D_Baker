using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ucCreatePlaneVisualization
{    

    static public void CreatePlaneVisualization(int grid_size, Vector3[] pos, Vector3[] normals, Vector3[] diff)
    {
        //instance rendering
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        MeshRenderer renderer;

        Shader debug_inst_shader = Shader.Find("Unlit/DebugPlaneShader");
        Material inst_mat = new Material(debug_inst_shader);
        inst_mat.enableInstancing = true;

        GameObject plane_root = new GameObject("PlaneRoot");
        for(int i = 0; i < pos.Length; ++i)
        {
            //plane size is 10M X 10M
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = ucCoordToUnity.F3(pos[i]);
            float scale_factor = 1.0f;
            float scale_value = ((1.0f / 10.0f) * (grid_size / 100.0f)) * scale_factor;
            plane.transform.localScale = new Vector3(scale_value, scale_value, scale_value);
            plane.transform.rotation = Quaternion.FromToRotation(Vector3.up, ucCoordToUnity.F3(normals[i].normalized));
            //plane.transform.LookAt(ucCoordToUnity.F3(normals[i]));
            //plane.transform.LookAt(new Vector3(1.0f, 0.0f, 0.0f));

            plane.transform.parent = plane_root.transform;

            props.SetColor("_Color", new Color(diff[i].x, diff[i].y, diff[i].z, 1.0f));

            renderer = plane.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = inst_mat;
            renderer.SetPropertyBlock(props);
        }
    }
}
