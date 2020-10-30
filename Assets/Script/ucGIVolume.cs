using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct VolumeData
{
    public List<Vector3> pos;
    public int lenx;
    public int leny;
    public int lenz;
}

public class ucGIVolume
{
    static List<Vector3> pos_list = null;

    static public VolumeData ExportGIVolumePos()
    {
        GameObject volume = GameObject.Find("GIVolume");

        MeshFilter mf = volume.GetComponent<MeshFilter>();
        Renderer mr = mf.GetComponent<Renderer>();

        Bounds bound = mr.bounds;
        //Vector3 max = bound.max;
        Vector3 min = bound.min;

        float unit = 1; // 1m --- 1 probe
        Vector3 size = bound.size;
        int lenx = (int)Mathf.Floor(Mathf.Max(1, Mathf.Floor((size.x + 0.5f) / unit)));
        int leny = (int)Mathf.Floor(Mathf.Max(1, Mathf.Floor((size.y + 0.5f) / unit)));
        int lenz = (int)Mathf.Floor(Mathf.Max(1, Mathf.Floor((size.z + 0.5f) / unit)));

        pos_list = new List<Vector3>();

        for (int k = 0; k < lenz; ++k)
        {
            for(int j = 0; j < leny; ++j)
            {
                for (int i = 0; i < lenx; ++i) 
                {
                    float x = i * unit;
                    float y = j * unit;
                    float z = k * unit;
                    pos_list.Add(min + new Vector3(x, y, z));
                }
            }
        }

        //CreateProbeVisualization(pos_list.ToArray());

        VolumeData ret = new VolumeData();
        ret.pos = pos_list;
        ret.lenx = lenx;
        ret.leny = leny;
        ret.lenz = lenz;

        return ret;
    }

    unsafe static public void CreateProbeVisualization(GIVolumeSHData[] shdata)
    {
        //instance rendering
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        MeshRenderer renderer;

        Shader probe_inst_shader = Shader.Find("Unlit/LightProbeInstShader");
        Material inst_mat = new Material(probe_inst_shader);
        inst_mat.enableInstancing = true;

        GameObject probe_root = new GameObject("GIProbe");
        for (int i = 0; i < shdata.Length; ++i)
        {
            //plane size is 10M X 10M
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //float[] pos_safe_array = ucExportLightProbe.FixedFloatArrayToSafeArray(shdata[i].pos, 3);
            sphere.transform.position = ucCoordToUnity.F3(new Vector3(shdata[i].pos[0]/100.0f, shdata[i].pos[1] / 100.0f, shdata[i].pos[2] / 100.0f));
            //Debug.LogFormat("pos = ({0}, {1}, {2})", sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z);
            float scale_factor = 0.3f;
            sphere.transform.localScale = new Vector3(scale_factor, scale_factor, scale_factor);
            //plane.transform.rotation = Quaternion.FromToRotation(Vector3.up, ucCoordToUnity.F3(normals[i].normalized));
            //plane.transform.LookAt(ucCoordToUnity.F3(normals[i]));
            //plane.transform.LookAt(new Vector3(1.0f, 0.0f, 0.0f));

            sphere.transform.parent = probe_root.transform;

            //props.SetColor("_Color", new Color(diff[i].x, diff[i].y, diff[i].z, 1.0f));
            const int sh_num = 4;
            shvectorrgb sh2_data = shdata[i].sh_vector;
            float[] sh_r = ucExportLightProbe.FixedFloatArrayToSafeArray(sh2_data.r.v, sh_num);
            //Debug.LogFormat("sh r = {0}, {1}, {2}, {3}", sh_r[0], sh_r[1], sh_r[2], sh_r[3]);
            props.SetVector("_sh2_r", new Vector4(sh_r[0], sh_r[1], sh_r[2], sh_r[3]));
            float[] sh_g = ucExportLightProbe.FixedFloatArrayToSafeArray(sh2_data.g.v, sh_num);
            props.SetVector("_sh2_g", new Vector4(sh_g[0], sh_g[1], sh_g[2], sh_g[3]));
            float[] sh_b = ucExportLightProbe.FixedFloatArrayToSafeArray(sh2_data.b.v, sh_num);
            props.SetVector("_sh2_b", new Vector4(sh_b[0], sh_b[1], sh_b[2], sh_b[3]));

            //Vector3 test_color = new Vector3(sh_r[1], sh_r[2], sh_r[3]);
            //test_color = test_color.normalized;
            //Vector4 test_color4 = new Vector4(Mathf.Abs(test_color.x), Mathf.Abs(test_color.y), Mathf.Abs(test_color.z), 1.0f);
            //float r = Random.Range(0.0f, 1.0f);
            //float g = Random.Range(0.0f, 1.0f);
            //float b = Random.Range(0.0f, 1.0f);
            //props.SetVector("_color", new Vector4(r, g, b, 1.0f));

            renderer = sphere.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = inst_mat;
            renderer.SetPropertyBlock(props);
        }
    }
}
