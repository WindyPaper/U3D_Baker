using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ucExportLightProbe
{
    static List<MeshFilter> light_probe_mesh_list = new List<MeshFilter>();

    public static List<Vector3> ExportProbePosition()
    {
        light_probe_mesh_list.Clear();

        List<MeshFilter> objs = ucExportMesh.GetAllObjectsInScene();

        List<Vector3> out_pos = new List<Vector3>();

        //int index_offset = 0;
        foreach (MeshFilter mf in objs)
        {
            if (mf.tag == "GameController")
            {
                light_probe_mesh_list.Add(mf);                
                out_pos.Add(ucCoordToUE.F3(mf.transform.position) * 100.0f);
            }
        }

        return out_pos;
    }

    //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.421.901&rep=rep1&type=pdf
    //Lambertian Reflectance and Linear Subspaces
    unsafe public static float[] dering(float[] sh)
    {
        float[] dering_sh = new float[sh.Length];

        float[] kn = new float[] {
            Mathf.Sqrt(Mathf.PI)/2.0f,
            Mathf.Sqrt(Mathf.PI/3.0f),
            Mathf.Sqrt(Mathf.PI * 5.0f)/8.0f,            
        };

        float[] alpha_l = new float[] {
            Mathf.Sqrt(Mathf.PI * 4.0f),
            Mathf.Sqrt((Mathf.PI * 4.0f)/3.0f),
            Mathf.Sqrt((Mathf.PI * 4.0f) / 5.0f)
        };

        dering_sh[0] = sh[0] * kn[0] * alpha_l[0];

        dering_sh[1] = sh[1] * kn[1] * alpha_l[1];
        dering_sh[2] = sh[2] * kn[1] * alpha_l[1];
        dering_sh[3] = sh[3] * kn[1] * alpha_l[1];

        dering_sh[4] = sh[4] * kn[2] * alpha_l[2];
        dering_sh[5] = sh[5] * kn[2] * alpha_l[2];
        dering_sh[6] = sh[6] * kn[2] * alpha_l[2];
        dering_sh[7] = sh[7] * kn[2] * alpha_l[2];
        dering_sh[8] = sh[8] * kn[2] * alpha_l[2];

        return dering_sh;
    }

    unsafe public static float[] FixedFloatArrayToSafeArray(float* fix_array, int num)
    {
        float[] ret = new float[num];

        for(int i = 0; i < num; ++i)
        {
            ret[i] = fix_array[i];
        }

        //ret = dering(ret);

        return ret;
    }


    unsafe public static void SetLightProbeData(VolumetricLightSample[] upper_data, VolumetricLightSample[] lower_data)
    {
        Shader sh_probe_shader = Shader.Find("Unlit/LightProbeShader");

        for(int i = 0; i < light_probe_mesh_list.Count; ++i)
        {            
            MeshFilter mf = light_probe_mesh_list[i];
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();

            Color incident_color = new Color(upper_data[i].IncidentLighting[0] + lower_data[i].IncidentLighting[0],
                upper_data[i].IncidentLighting[1] + lower_data[i].IncidentLighting[1],
                upper_data[i].IncidentLighting[2] + lower_data[i].IncidentLighting[2]);

            //Color incident_color = new Color(1.0f, 1.0f, 1.0f);

            Material m = new Material(sh_probe_shader);
            m.SetColor("incident_light", incident_color);

            Vector3 sky_occlusion = new Vector3(upper_data[i].SkyOcclusion[0], upper_data[i].SkyOcclusion[1], upper_data[i].SkyOcclusion[2]);            
            m.SetVector("occlusion", new Vector4(sky_occlusion.x, sky_occlusion.y, sky_occlusion.z));

            const int sh_num = 9;
            shvectorrgb3 sh3_data = upper_data[i].SHVector + lower_data[i].SHVector;
            float[] sh_r = FixedFloatArrayToSafeArray(sh3_data.r.v, sh_num);
            m.SetFloatArray("_sh3_r", sh_r);
            float[] sh_g = FixedFloatArrayToSafeArray(sh3_data.g.v, sh_num);
            m.SetFloatArray("_sh3_g", sh_g);
            float[] sh_b = FixedFloatArrayToSafeArray(sh3_data.b.v, sh_num);
            m.SetFloatArray("_sh3_b", sh_b);


            mr.material = m;

            Debug.Log("set light probe " + mf.name);
        }
    }
}
