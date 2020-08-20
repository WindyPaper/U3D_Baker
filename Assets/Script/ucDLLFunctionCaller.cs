using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEditor;
using System.Reflection;
using System.Threading;
using System.IO;

[StructLayout(LayoutKind.Sequential)]
public struct ucCyclesInitOptions
{
    public int width;
    public int height;

    public int sample_count;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string device_working_folder;
    public int render_device;

    public int work_type; //RENDER / BAKDER
    public int enable_denoise;

}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct shvector2
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public fixed float v[4]; //4

    public static shvector2 operator +(shvector2 a, shvector2 b)
    {
        shvector2 ret = new shvector2();

        ret.v[0] = a.v[0] + b.v[0];
        ret.v[1] = a.v[1] + b.v[1];
        ret.v[2] = a.v[2] + b.v[2];
        ret.v[3] = a.v[3] + b.v[3];

        return ret;
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct shvector3
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
    public fixed float v[9];

    public static shvector3 operator +(shvector3 a, shvector3 b)
    {
        shvector3 ret = new shvector3();

        ret.v[0] = a.v[0] + b.v[0];
        ret.v[1] = a.v[1] + b.v[1];
        ret.v[2] = a.v[2] + b.v[2];
        ret.v[3] = a.v[3] + b.v[3];
        ret.v[4] = a.v[4] + b.v[4];
        ret.v[5] = a.v[5] + b.v[5];
        ret.v[6] = a.v[6] + b.v[6];
        ret.v[7] = a.v[7] + b.v[7];
        ret.v[8] = a.v[8] + b.v[8];

        return ret;
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct shvectorrgb
{
    public shvector2 r;
    public shvector2 g;
    public shvector2 b;

    public static shvectorrgb operator +(shvectorrgb a, shvectorrgb b)
    {
        shvectorrgb ret = new shvectorrgb();

        ret.r = a.r + b.r;
        ret.g = a.g + b.g;
        ret.b = a.b + b.b;

        return ret;
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct shvectorrgb3
{
    public shvector3 r;
    public shvector3 g;
    public shvector3 b;

    public static shvectorrgb3 operator +(shvectorrgb3 a, shvectorrgb3 b)
    {
        shvectorrgb3 ret = new shvectorrgb3();

        ret.r = a.r + b.r;
        ret.g = a.g + b.g;
        ret.b = a.b + b.b;

        return ret;
    }
};

[StructLayout(LayoutKind.Sequential)]
unsafe struct GatheredLightSample
{
    public shvectorrgb SHVector;
    public float SHCorrection;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public fixed float IncidentLighting[3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public fixed float SkyOcclusion[3];
    public float AverageDistance;
    public float NumBackfaceHits;
};

[StructLayout(LayoutKind.Sequential, Pack = 16)] //in C++ align 16
unsafe struct SurfelData
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public fixed float pos[4];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public fixed float normal[4];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public fixed float diff_alpha[4];
};

unsafe struct OutputLightMapData
{
    public String name;
    public int width;
    public int height;
    public GatheredLightSample[] outdata;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VolumetricLightSample
{
    public shvectorrgb3 SHVector;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public fixed float IncidentLighting[3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public fixed float SkyOcclusion[3];
    public float MinDistance;
    public float BackfacingHitsFraction;
};

public class ucDLLFunctionCaller
{
    static IntPtr nativeLibraryPtr;
    ucThreadDispatcher thread_dispatcher = null;

    delegate int bake_scene(int number, int multiplyBy);
    delegate bool init_lightmass(ucCyclesInitOptions op);

    delegate void PreallocRadiositySurfaceCachePointers(int num);

    delegate void ImportAggregateMesh(
        int num_vertices, 
        int num_triangles, 
        float[] w_pos_buffer, 
        float[] texture_uv_buf, 
        float[] lm_uv_buf, 
        int[] index_buf, 
        int[] triangle_mat_buf, 
        int[] triangle_tex_index_buf);

    public delegate void LogCb([MarshalAs(UnmanagedType.LPWStr)]String str);
    delegate void SetLogHandler([MarshalAs(UnmanagedType.FunctionPtr)]LogCb pDelegate);

    delegate void ImportPunctualLights(
        int NumDirectionalLights,
        DirectionalLight[] DirectionalLights,
        int NumPointLights,
        PointLight[] PointLights,
        int NumSpotLights,
        SpotLight[] SpotLights);


    delegate void ImportSurfaceCache(
        int ID,
        int SurfaceCacheSizeX,
        int SurfaceCacheSizeY,
        float[] WorldPositionMap,
        float[] WorldNormalMap,
        float[] ReflectanceMap,
        float[] EmissiveMap);

    delegate void SetTotalTexelsForProgressReport(int texels_num);

    delegate void SetGlobalSamplingParameters(float threshold);

    delegate void RunRadiosity(int pass_num, int sample_num);

    delegate void ImportSkyLightCubemap(int theta_step_num, int phi_step_num, float[] upper_data, float[] lower_data);

    

    delegate void CalculateIndirectLightingTextureMapping(int texels_num, int size_x, int size_y, int sample_num, float[] world_pos, float[] world_normal, float[] texel_radius_map, GatheredLightSample[] out_lightmap_data);

    delegate void ImportDirectLights(
        int NumDirectionalLights,
        DirectionalLight[] DirectionalLights,
        int NumPointLights,
        PointLight[] PointLights,
        int NumSpotLights,
        SpotLight[] SpotLights);

    delegate void CalculateDirectLightingAndShadow(int texels_num, int size_x, int size_y, int sample_num, float[] world_pos, float[] world_normal, float[] texel_radius_map, GatheredLightSample[] out_lightmap_data);

    delegate void CalculateAllLightingAndShadow(int texels_num, int size_x, int size_y, int sample_num, float[] world_pos, float[] world_normal, float[] texel_radius_map, GatheredLightSample[] out_lightmap_data);

    delegate void CalculateVolumeSampleList(int num_samples, float[] world_pos,
        [In, Out] VolumetricLightSample[] out_upper_samples,
        [In, Out] VolumetricLightSample[] out_lower_samples);

    delegate void RasterizeModelToSurfel(int grid_element_size, int num_vertices, int num_triangle,
        float[] local_pos, float[] uvs, int[] triangle_indexs, int[] triangle_mat_tex_indexs, float[] bbox,
        int[] out_surfel_num, SurfelData[] out_surfel_data);

    public ucDLLFunctionCaller(ucThreadDispatcher thread_dispatcher)
    {
        this.thread_dispatcher = thread_dispatcher;
    }    

    public void LoadDLLAndInit()
    {
        LoadDLL();

        InitLightMass();      

        SendSkyData();

        SendAllMeshToCycles();

        SendRadiosityCacheData();                
    }

    public void StartBaking()
    {
        SendNeedBakedLightData();
        RunRadiosityPass();
        CalculateAllBakedLighting();
        ExportQuanLMData();
    }

    unsafe public void ExportQuanLMData()
    {
        List<OutputLightMapData> out_lm_data_list = new List<OutputLightMapData>();

        for(int i = 0; i < ucBakingData.indirected_lighting_baking_data.Count; ++i)
        {
            OutputLightMapData data = new OutputLightMapData();
            data.name = ucBakingData.indirected_lighting_baking_data[i].name;
            data.width = ucBakingData.indirected_lighting_baking_data[i].width;
            data.height = ucBakingData.indirected_lighting_baking_data[i].height;

            data.outdata = ucBakingData.indirected_lighting_baking_data[i].outdata;
            for(int j = 0; j < data.outdata.Length; ++j)
            {
                data.outdata[j].IncidentLighting[0] += ucBakingData.direct_lighting_baking_data[i].outdata[j].IncidentLighting[0];
                data.outdata[j].IncidentLighting[1] += ucBakingData.direct_lighting_baking_data[i].outdata[j].IncidentLighting[1];
                data.outdata[j].IncidentLighting[2] += ucBakingData.direct_lighting_baking_data[i].outdata[j].IncidentLighting[2];

                //Add SH
                data.outdata[j].SHVector = data.outdata[j].SHVector + ucBakingData.direct_lighting_baking_data[i].outdata[j].SHVector;
                data.outdata[j].SHCorrection += ucBakingData.direct_lighting_baking_data[i].outdata[j].SHCorrection;
                //data.outdata[j].SHVector.r.v[0] = 1.0f;
                //data.outdata[j].SHCorrection = 1.0f;
            }

            //out_lm_data_list.Add(data);
            out_lm_data_list.Add(ucBakingData.direct_lighting_baking_data[i]);
        }

        //Debug.LogFormat("Output list = {0}", out_lm_data_list.Count);
        foreach (OutputLightMapData out_data in out_lm_data_list)
        {
            QuantifierResult(out_data.outdata, out_data.width, out_data.height, out_data.name);
        }
    }

    public void SendSkyData()
    {
        float[] up_data = new float[16*4];
        float[] low_data = new float[16*4];

        for(int i = 0; i < 16*4; ++i)
        {
            up_data[i] = 0.00f;
            low_data[i] = 0.00f;
        }

        ucNative.Invoke_Void<ImportSkyLightCubemap>(nativeLibraryPtr, 4, 4, up_data, low_data);
    }

    public void InitLightMass()
    {
        LogCb lcb = new LogCb(LightmassLogCb);
        ucNative.Invoke_Void<SetLogHandler>(nativeLibraryPtr, lcb);

        ucNative.Invoke_Void<SetGlobalSamplingParameters>(nativeLibraryPtr, 1000.0f);
    }

    public void Release()
    {
        if(nativeLibraryPtr == IntPtr.Zero)
        {
            return;
        }

        //ucNative.Invoke<int, release_cycles>(nativeLibraryPtr);

        UnloadDLL();
    }    

    void LoadDLL()
    {
        if (nativeLibraryPtr != IntPtr.Zero) return;

        string dll_path = Application.dataPath + "/Plugins/";
        string dll_file_name = "GPULightmassKernel.dll";
        ucNative.LoadLibraryFlags flags = ucNative.LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS;
        string dll_full_name = dll_path + dll_file_name;
        ucNative.AddDllDirectory(dll_path);
        nativeLibraryPtr = ucNative.LoadLibraryEx(dll_full_name, IntPtr.Zero, flags);

        if (nativeLibraryPtr == IntPtr.Zero)
        {
            Debug.LogError("Failed to load native library. Path = " + dll_full_name + " Last Error Code = " + Marshal.GetLastWin32Error());

            Debug.Log(ucNative.GetErrorMessage(Marshal.GetLastWin32Error()));
        }
    }

    public void SendAllMeshToCycles()
    {
        List<ucCyclesMeshMtlData> mesh_mtl_datas = new List<ucCyclesMeshMtlData>();
        ucExportMesh.ExportCurrSceneMesh(ref mesh_mtl_datas);

        List<float> vertex_array_list = new List<float>();
        List<float> uv_array_list = new List<float>();
        List<float> lm_uv_array_list = new List<float>();
        List<int> index_array_list = new List<int>();
        List<int> index_mat_array_list = new List<int>();
        List<int> lm_index_array_list = new List<int>();
        int all_vertex_num = 0;
        int all_triangle_num = 0;

        int i = 0;
        foreach (ucCyclesMeshMtlData obj in mesh_mtl_datas)
        {
            EditorUtility.DisplayProgressBar("Sync meshes to Cycles", "Sync meshes to Cycles Progress ", i / mesh_mtl_datas.Count * 100.0f);

            all_vertex_num += obj.mesh_data.vertex_num;
            all_triangle_num += obj.mesh_data.triangle_num;

            vertex_array_list.AddRange(obj.mesh_data.vertex_array);
            uv_array_list.AddRange(obj.mesh_data.uvs_array);
            lm_uv_array_list.AddRange(obj.mesh_data.lightmapuvs_array);
            index_array_list.AddRange(obj.mesh_data.index_array);
            index_mat_array_list.AddRange(obj.mesh_data.index_mat_array);
            lm_index_array_list.AddRange(obj.mesh_data.lm_index_array);

            ++i;
        }

        ucNative.Invoke_Void<ImportAggregateMesh>(nativeLibraryPtr,
                all_vertex_num,
                all_triangle_num,
                vertex_array_list.ToArray(),
                uv_array_list.ToArray(),
                lm_uv_array_list.ToArray(),
                index_array_list.ToArray(),
                index_mat_array_list.ToArray(),
                lm_index_array_list.ToArray());

        EditorUtility.ClearProgressBar();
    }    

    unsafe public void CalculateIndirectedLighting()
    {
        List<LightmassBakerData> datas = ucExportMesh.GetBakerData();

        //ucNative.Invoke_Void<PreallocRadiositySurfaceCachePointers>(nativeLibraryPtr, datas.Count);

        List<OutputLightMapData> out_lm_data_list = new List<OutputLightMapData>();

        //int i = 0;
        //int texel_num = 0;
        foreach (LightmassBakerData obj in datas)
        {
            int texel_num = obj.size_x * obj.size_y;


            float[] texel_radius = new float[texel_num];
            for(int i = 0; i < texel_num; ++i)
            {
                texel_radius[i] = 0.1f;
            }

            GatheredLightSample[] tmp_out_lm_data = new GatheredLightSample[texel_num];            

            object[] param = new object[]
            {
                texel_num,
                obj.size_x,
                obj.size_y,
                32,
                obj.world_pos_map,
                obj.world_normal_map,
                texel_radius,
                tmp_out_lm_data
            };
            ucNative.Invoke_Void<CalculateIndirectLightingTextureMapping>(nativeLibraryPtr, param);

            OutputLightMapData lm_data = new OutputLightMapData();
            lm_data.name = obj.name;
            lm_data.width = obj.size_x;
            lm_data.height = obj.size_y;
            lm_data.outdata = tmp_out_lm_data;

            out_lm_data_list.Add(lm_data);
        }

        //ucNative.Invoke_Void<SetTotalTexelsForProgressReport>(nativeLibraryPtr, texel_num);

        ucBakingData.indirected_lighting_baking_data = out_lm_data_list;

        //Debug.LogFormat("Output list = {0}", out_lm_data_list.Count);
        //foreach(OutputLightMapData out_data in out_lm_data_list)
        //{
        //    QuantifierResult(out_data.outdata, out_data.width, out_data.height, out_data.name);
        //}        

    }

    unsafe public void CalculateDirectedLighting()
    {
        List<LightmassBakerData> datas = ucExportMesh.GetBakerData();

        //ucNative.Invoke_Void<PreallocRadiositySurfaceCachePointers>(nativeLibraryPtr, datas.Count);

        List<OutputLightMapData> out_lm_data_list = new List<OutputLightMapData>();

        //int i = 0;
        //int texel_num = 0;
        foreach (LightmassBakerData obj in datas)
        {
            int texel_num = obj.size_x * obj.size_y;


            float[] texel_radius = new float[texel_num];
            for (int i = 0; i < texel_num; ++i)
            {
                texel_radius[i] = 0.1f;
            }

            GatheredLightSample[] tmp_out_lm_data = new GatheredLightSample[texel_num];

            object[] param = new object[]
            {
                texel_num,
                obj.size_x,
                obj.size_y,
                32,
                obj.world_pos_map,
                obj.world_normal_map,
                texel_radius,
                tmp_out_lm_data
            };
            ucNative.Invoke_Void<CalculateDirectLightingAndShadow>(nativeLibraryPtr, param);

            OutputLightMapData lm_data = new OutputLightMapData();
            lm_data.name = obj.name;
            lm_data.width = obj.size_x;
            lm_data.height = obj.size_y;
            lm_data.outdata = tmp_out_lm_data;

            out_lm_data_list.Add(lm_data);
        }

        //ucNative.Invoke_Void<SetTotalTexelsForProgressReport>(nativeLibraryPtr, texel_num);

        Debug.LogFormat("Output list = {0}", out_lm_data_list.Count);

        ucBakingData.direct_lighting_baking_data = out_lm_data_list;

        //foreach (OutputLightMapData out_data in out_lm_data_list)
        //{
        //    QuantifierResult(out_data.outdata, out_data.width, out_data.height, out_data.name);
        //}

    }

    unsafe public void CalculateAllBakedLighting()
    {
        List<LightmassBakerData> datas = ucExportMesh.GetBakerData();

        //ucNative.Invoke_Void<PreallocRadiositySurfaceCachePointers>(nativeLibraryPtr, datas.Count);

        List<OutputLightMapData> out_lm_data_list = new List<OutputLightMapData>();

        //int i = 0;
        //int texel_num = 0;
        foreach (LightmassBakerData obj in datas)
        {
            int texel_num = obj.size_x * obj.size_y;


            float[] texel_radius = new float[texel_num];
            for (int i = 0; i < texel_num; ++i)
            {
                texel_radius[i] = 0.1f;
            }

            GatheredLightSample[] tmp_out_lm_data = new GatheredLightSample[texel_num];

            object[] param = new object[]
            {
                texel_num,
                obj.size_x,
                obj.size_y,
                32,
                obj.world_pos_map,
                obj.world_normal_map,
                texel_radius,
                tmp_out_lm_data
            };
            ucNative.Invoke_Void<CalculateAllLightingAndShadow>(nativeLibraryPtr, param);

            OutputLightMapData lm_data = new OutputLightMapData();
            lm_data.name = obj.name;
            lm_data.width = obj.size_x;
            lm_data.height = obj.size_y;
            lm_data.outdata = tmp_out_lm_data;

            out_lm_data_list.Add(lm_data);
        }

        //ucNative.Invoke_Void<SetTotalTexelsForProgressReport>(nativeLibraryPtr, texel_num);

        Debug.LogFormat("Output list = {0}", out_lm_data_list.Count);

        //ucBakingData.direct_lighting_baking_data = out_lm_data_list;

        foreach (OutputLightMapData out_data in out_lm_data_list)
        {
            QuantifierResult(out_data.outdata, out_data.width, out_data.height, out_data.name);
        }

    }

    unsafe void QuantifierResult(GatheredLightSample[] ret, int width, int height, String name)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

        for(int i = 0; i < height; ++i)
        {
            for(int j = 0; j < width; ++j)
            {
                ref GatheredLightSample InGatheredLightSample = ref ret[i * width + j];
                // SHCorrection is SHVector sampled with the normal
                float DirCorrection = 1.0f / Math.Max(0.0001f, InGatheredLightSample.SHCorrection);
                float[] DirLuma = new float[4];
                for (int sh_i = 0; sh_i < 4; sh_i++)
                {
                    DirLuma[sh_i] = 0.30f * InGatheredLightSample.SHVector.r.v[sh_i];
                    DirLuma[sh_i] += 0.59f * InGatheredLightSample.SHVector.g.v[sh_i];
                    DirLuma[sh_i] += 0.11f * InGatheredLightSample.SHVector.b.v[sh_i];                    

                    // Lighting is already in IncidentLighting. Force directional SH as applied to a flat normal map to be 1 to get purely directional data.
                    DirLuma[sh_i] *= DirCorrection / (float)Math.PI;
                }
                float ColorScale = DirLuma[0];

                tex.SetPixel(j, i, new Color(
                    InGatheredLightSample.IncidentLighting[0] * ColorScale,
                    InGatheredLightSample.IncidentLighting[1] * ColorScale,
                    InGatheredLightSample.IncidentLighting[2] * ColorScale));

                //tex.SetPixel(j, i, new Color(
                //    InGatheredLightSample.IncidentLighting[0],
                //    InGatheredLightSample.IncidentLighting[1],
                //    InGatheredLightSample.IncidentLighting[2]));

                //tex.SetPixel(j, i, new Color(
                //    ColorScale,
                //    ColorScale,                    
                //    ColorScale));
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("./Assets/LightMapData/" + name + ".png", bytes);
    }

    public void SendRadiosityCacheData()
    {
        List<LightmassBakerData> datas = ucExportMesh.GetBakerData();

        ucNative.Invoke_Void<PreallocRadiositySurfaceCachePointers>(nativeLibraryPtr, datas.Count);

        int i = 0;
        int texel_num = 0;
        foreach(LightmassBakerData obj in datas)
        {
            ucNative.Invoke_Void<ImportSurfaceCache>(nativeLibraryPtr,
                i,
                obj.size_x,
                obj.size_y,
                obj.world_pos_map,
                obj.world_normal_map,
                obj.diffuse_map,
                obj.emissive_map);

            texel_num += obj.size_x * obj.size_y;

            ++i;
        }

        ucNative.Invoke_Void<SetTotalTexelsForProgressReport>(nativeLibraryPtr, texel_num);
    }

    public void SendIndirectedLightData()
    {
        ExportPunctualLight export_lights = ucExportLights.ExportIndirectedLights();

        ucNative.Invoke_Void<ImportPunctualLights>(nativeLibraryPtr, export_lights.DirLightNum, export_lights.DirLights,
            export_lights.PointLightNum, export_lights.PointLights,
            export_lights.SpotLightNum, export_lights.SpotLights);
    }

    public void SendAllBakeLightData()
    {
        ExportPunctualLight export_lights = ucExportLights.ExportStaticBakedLights();

        ucNative.Invoke_Void<ImportDirectLights>(nativeLibraryPtr, export_lights.DirLightNum, export_lights.DirLights,
            export_lights.PointLightNum, export_lights.PointLights,
            export_lights.SpotLightNum, export_lights.SpotLights);
    }

    public void SendNeedBakedLightData()
    {
        ExportPunctualLight export_lights = ucExportLights.ExportNeedBakedLights();

        ucNative.Invoke_Void<ImportPunctualLights>(nativeLibraryPtr, export_lights.DirLightNum, export_lights.DirLights,
            export_lights.PointLightNum, export_lights.PointLights,
            export_lights.SpotLightNum, export_lights.SpotLights);
    }

    public void RunRadiosityPass()
    {
        ucNative.Invoke_Void<RunRadiosity>(nativeLibraryPtr, 3, 8);
    } 
    
    public void LightmassLogCb(String str)
    {
        Debug.Log(str);
    }

    //lightprobe
    unsafe public void StartLightprobeBaking()
    {
        //Debug.Log("Baking light probe!");

        //SendNeedBakedLightData();
        //RunRadiosityPass();

        //List<Vector3> probes_pos = ucExportLightProbe.ExportProbePosition();
        //VolumetricLightSample[] upper_samples = new VolumetricLightSample[probes_pos.Count];
        //VolumetricLightSample[] lower_samples = new VolumetricLightSample[probes_pos.Count];
        //int sample_nums = probes_pos.Count;

        //Vector3[] pos_array = probes_pos.ToArray();
        //float[] pos_float_array = new float[pos_array.Length * 3];

        //for(int i = 0; i < pos_array.Length; ++i)
        //{
        //    pos_float_array[i * 3] = pos_array[i].x;
        //    pos_float_array[i * 3 + 1] = pos_array[i].y;
        //    pos_float_array[i * 3 + 2] = pos_array[i].z;
        //}

        //object[] param = new object[]
        //{
        //    sample_nums,
        //    pos_float_array,
        //    upper_samples,
        //    lower_samples
        //};
        //ucNative.Invoke_Void<CalculateVolumeSampleList>(nativeLibraryPtr, param);

        //ucExportLightProbe.SetLightProbeData(upper_samples, lower_samples);

        //test
        List<ucCyclesMeshMtlData> mesh_mtl_datas = new List<ucCyclesMeshMtlData>();
        ucExportMesh.ExportCurrSceneMesh(ref mesh_mtl_datas);
        float[] min_max = new float[6];
        min_max[0] = mesh_mtl_datas[0].mesh_data.bbox.min.x * 100.0f;
        min_max[1] = mesh_mtl_datas[0].mesh_data.bbox.min.y * 100.0f;
        min_max[2] = mesh_mtl_datas[0].mesh_data.bbox.min.z * 100.0f;

        min_max[3] = mesh_mtl_datas[0].mesh_data.bbox.max.x * 100.0f;
        min_max[4] = mesh_mtl_datas[0].mesh_data.bbox.max.y * 100.0f;
        min_max[5] = mesh_mtl_datas[0].mesh_data.bbox.max.z * 100.0f;

        int[] out_surfel_num = new int[1];
        Debug.Log("Surfel size = " + sizeof(SurfelData));
        SurfelData[] out_surfel_data = new SurfelData[200];

        object[] param = new object[]
        {
            10, //cm
            mesh_mtl_datas[0].mesh_data.vertex_num,
            mesh_mtl_datas[0].mesh_data.triangle_num,
            mesh_mtl_datas[0].mesh_data.vertex_array,
            mesh_mtl_datas[0].mesh_data.uvs_array,
            mesh_mtl_datas[0].mesh_data.index_array,
            mesh_mtl_datas[0].mesh_data.index_array,
            min_max,
            out_surfel_num,
            out_surfel_data
        };

        using (BinaryWriter writer = new BinaryWriter(File.Open("./Model.bin", FileMode.Create)))
        {
            unsafe
            {
                writer.Write(mesh_mtl_datas[0].mesh_data.vertex_num);

                fixed (float* f = mesh_mtl_datas[0].mesh_data.vertex_array)
                {                    
                    Byte* b_data = (Byte*)f;
                    //writer.Write((bype[])(byte*)f, 0, mesh_mtl_datas[0].mesh_data.vertex_num * sizeof(float) * 3);
                }                
                
            }
        }

        ucNative.Invoke_Void<RasterizeModelToSurfel>(nativeLibraryPtr, param);

        SurfelData[] test_out = out_surfel_data;

        Debug.Log("surfel number = " + out_surfel_num[0]);
        foreach (SurfelData i in test_out)
        {
            //if (i.pos[0] != 0.0f || i.pos[1] != 0.0f || i.pos[2] != 0.0f)
            {
                Debug.LogFormat("pos x = {0}, y = {1}, z = {2}, pixel index = {3}", i.pos[0], i.pos[1], i.pos[2], i.normal[0]);
            }
        }
    }

    public static void UnloadDLL()
    {
        Debug.Log("UnloadDLL");
        if (nativeLibraryPtr == IntPtr.Zero) return;

        if (ucNative.FreeLibrary(nativeLibraryPtr) == false)
        {
            Debug.LogError("Cycles DLL unloads fail!!");
        }
        else
        {
            nativeLibraryPtr = IntPtr.Zero;
        }
    }    
}
