using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

public struct ucCyclesMeshData
{
    public float[] vertex_array;
    public float[] uvs_array;
    public float[] lightmapuvs_array;
    public float[] normal_array;
    public int vertex_num;
    public int[] index_array;
    public int[] index_mat_array;
    public int[] lm_index_array;
    public int triangle_num;
    public int mtl_num;
    public Bounds bbox;
};

public struct LightmassBakerData
{    
    public string name;

    public int texels_num;
    public int size_x;
    public int size_y;
    public int sample_num;
    public float[] diffuse_map;
    public float[] world_pos_map;
    public float[] world_normal_map;
    public float[] emissive_map;
    public float[] texel_radius;    
};

public struct ucCyclesMeshMtlData
{
    public ucCyclesMeshData mesh_data;
};

public class ucExportMesh
{
    public static ucMeshLightmapData[] mrt_datas;

    public static List<MeshFilter> GetAllObjectsInScene()
    {
        List<MeshFilter> objectsInScene = new List<MeshFilter>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            MeshFilter mf = go.transform.GetComponent<MeshFilter>();
            if (mf && go.active == true)
            {
                //Debug.Log(go.name);
                objectsInScene.Add(mf);
            }
        }
        return objectsInScene;
    }    

    static void ReadFloatFromTexColor(ref float[] f_data, Color[] colors)
    {        
        for (int n = 0; n < colors.Length; ++n)
        {
            f_data[n * 4] = colors[n].r;
            f_data[n * 4 + 1] = colors[n].g;
            f_data[n * 4 + 2] = colors[n].b;
            f_data[n * 4 + 3] = colors[n].a;
        }
    }

    public static List<LightmassBakerData> GetBakerData()
    {
        List<MeshFilter> objs = GetAllObjectsInScene();

        List<LightmassBakerData> bake_data_list = new List<LightmassBakerData>();

        foreach (ucMeshLightmapData d in mrt_datas)
        {
            LightmassBakerData bake_data = new LightmassBakerData();
            string name = d.name;

            bake_data.name = name;                                 
            
            Color[] diffuse_data = ucObjectMrt.toTexture2D(d.rt_texs[0], d.size).GetPixels();
            string diffuse_name = string.Format(name + "_rt_{0}", 0);

            Color[] normal_data = ucObjectMrt.toTexture2D(d.rt_texs[1], d.size).GetPixels();
            string normal_name = string.Format(name + "_rt_{0}", 1);

            Color[] world_pos_data = ucObjectMrt.toTexture2D(d.rt_texs[2], d.size).GetPixels();
            string world_pos_name = string.Format(name + "_rt_{0}", 2);            

            bake_data.texels_num = d.size * d.size; //diffuse_t.width * diffuse_t.height;
            bake_data.size_x = d.size;
            bake_data.size_y = d.size;
            bake_data.sample_num = 16;

            //Color[] diffuse_color = diffuse_t.GetPixels(0);
            bake_data.diffuse_map = new float[bake_data.texels_num * 4];
            ReadFloatFromTexColor(ref bake_data.diffuse_map, diffuse_data);

            //Color[] normal_color = normal_t.GetPixels(0);
            bake_data.world_normal_map = new float[bake_data.texels_num * 4];
            ReadFloatFromTexColor(ref bake_data.world_normal_map, normal_data);

            //Color[] world_pos_color = world_pos_t.GetPixels(0);
            bake_data.world_pos_map = new float[bake_data.texels_num * 4];
            ReadFloatFromTexColor(ref bake_data.world_pos_map, world_pos_data);

            bake_data.emissive_map = new float[bake_data.texels_num * 4];
            //Array.Clear(bake_data.emissive_map, 0, bake_data.emissive_map.Length);
            for (int i = 0; i < bake_data.texels_num * 4; i += 4)
            {
                bake_data.emissive_map[i] = 0.0f;
                bake_data.emissive_map[i + 1] = 0.0f;
                bake_data.emissive_map[i + 2] = 0.0f;
                bake_data.emissive_map[i + 3] = 1.0f; //alpha
            }


            bake_data_list.Add(bake_data);
        }        

        return bake_data_list;
    }

    public static void ExportCurrSceneMesh(ref List<ucCyclesMeshMtlData> mesh_data_list)
    {
        List<MeshFilter> objs = GetAllObjectsInScene();

        int index_offset = 0;
        foreach (MeshFilter mf in objs)
        {
            if (mf.tag == "GameController")
            {
                continue;
            }

            ucCyclesMeshMtlData mesh_mtl_data = new ucCyclesMeshMtlData();
            mesh_mtl_data.mesh_data = new ucCyclesMeshData();
            ref ucCyclesMeshData mesh_data = ref mesh_mtl_data.mesh_data;
            //ref ucCyclesMtlData[] mtl_datas = ref mesh_mtl_data.mtl_datas;

            Transform t = mf.transform;

            Vector3 final_scale = t.localScale;
            Transform parent = t.parent;
            while (parent != null)
            {
                final_scale = Vector3.Scale(final_scale, parent.localScale);
                parent = parent.parent;
            }

            //Vector3 local_scale = t.localToWorldMatrix.lossyScale;
            //Vector3 p = t.localPosition;
            Quaternion r = t.rotation;


            int numVertices = 0;
            Mesh m = mf.sharedMesh;
            if (!m)
            {
                Debug.LogError("No mesh!");
                continue;
            }
            mesh_data.bbox = m.bounds;
            mesh_data.vertex_array = new float[m.vertices.Length * 3];
            foreach (Vector3 vv in m.vertices)
            {                
                Vector3 tv = (t.TransformPoint(vv));
                Vector3 scale_tv = tv * 100;
                Vector3 v = ucCoordToUE.F3(scale_tv);
                mesh_data.vertex_array[numVertices * 3] = v.x;
                mesh_data.vertex_array[numVertices * 3 + 1] = v.y;
                mesh_data.vertex_array[numVertices * 3 + 2] = v.z;

                numVertices++;
            }
            mesh_data.vertex_num = numVertices;

            //int numNormal = 0;
            //mesh_data.normal_array = new float[m.normals.Length * 3];
            //foreach (Vector3 nn in m.normals)
            //{
            //    //Vector3 v = r * nn;
            //    //v = Vector3.Scale(v, final_scale);
            //    //v = Vector3.Normalize(v);
            //    //Vector4 v = new Vector4(nn.x, nn.y, nn.z, 0.0f);
            //    Vector3 v = nn;// Matrix4x4.Transpose(t.transform.worldToLocalMatrix).MultiplyVector(nn);
            //    v = ucCoordToUE.F3(v);

            //    mesh_data.normal_array[numNormal * 3] = v.x;
            //    mesh_data.normal_array[numNormal * 3 + 1] = v.y;
            //    mesh_data.normal_array[numNormal * 3 + 2] = v.z;
            //    //mesh_data.normal_array[numNormal * 4 + 3] = 0.0f;

            //    numNormal++;
            //}

            int numUVs = 0;
            mesh_data.uvs_array = new float[m.uv.Length * 2];
            foreach (Vector2 v in m.uv)
            {
                mesh_data.uvs_array[numUVs * 2] = v.x;
                mesh_data.uvs_array[numUVs * 2 + 1] = v.y;

                numUVs++;
            }

            mesh_data.lightmapuvs_array = new float[m.uv.Length * 2]; //hack 防止没有lightmap崩溃
            int numLightmapuv = 0;
            if (m.uv2.Length > 0)
            {
                foreach (Vector2 v in m.uv2)
                {
                    mesh_data.lightmapuvs_array[numLightmapuv * 2] = v.x;
                    mesh_data.lightmapuvs_array[numLightmapuv * 2 + 1] = v.y;

                    numLightmapuv++;
                }
            }

            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
            mesh_data.mtl_num = mats.Length;
            //GetObjectMtls(mf, ref mtl_datas);


            mesh_data.triangle_num = 0;
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);
                mesh_data.triangle_num += triangles.Length/3;
            }

            mesh_data.index_array = new int[mesh_data.triangle_num * 3];
            mesh_data.index_mat_array = new int[mesh_data.triangle_num];
            mesh_data.lm_index_array = new int[mesh_data.triangle_num];
            int index_i = 0;            
            for (int material = 0; material < m.subMeshCount; material++)
            {
                int[] triangles = m.GetTriangles(material);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (final_scale.x > 0 && final_scale.y > 0 && final_scale.z > 0)
                    {
                        //revert wind
                        mesh_data.index_array[index_i * 3] = triangles[i + 0] + index_offset;
                        mesh_data.index_array[index_i * 3 + 1] = triangles[i + 1] + index_offset;
                        mesh_data.index_array[index_i * 3 + 2] = triangles[i + 2] + index_offset;
                    }
                    else
                    {
                        //for negative scale value, revert triangle order.
                        mesh_data.index_array[index_i * 3] = triangles[i + 0] + index_offset;
                        mesh_data.index_array[index_i * 3 + 1] = triangles[i + 2] + index_offset;
                        mesh_data.index_array[index_i * 3 + 2] = triangles[i + 1] + index_offset;
                    }
                    mesh_data.index_mat_array[index_i] = material;
                    mesh_data.lm_index_array[index_i] = mesh_data_list.Count;

                    ++index_i;
                }
            }

            index_offset += mesh_data.vertex_num;
            mesh_data_list.Add(mesh_mtl_data);
        }

        //return mesh_data_list;
    }
}