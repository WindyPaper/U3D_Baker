using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

//name, intensity, radius, angle, l.areaSize.x, l.areaSize.y, color_f, dir, pos, l.type);
public struct ucLightData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string name;

    public float intensity;
    public float radius;
    public float angle;
    public float sizex, sizey;
    public float[] color;
    public float[] dir;
    public float[] pos;
    public int type;
}

[StructLayout(LayoutKind.Sequential)]
public struct DirectionalLight
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Color;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Direction;
};

[StructLayout(LayoutKind.Sequential)]
public struct PointLight
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Color;

    public float Radius;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] WorldPosition;
};

[StructLayout(LayoutKind.Sequential)]
public struct SpotLight
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Color;

    public float Radius;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] WorldPosition;

    public float CosOuterConeAngle;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Direction;

    public float CosInnerConeAngle;
};

[StructLayout(LayoutKind.Sequential)]
public struct ExportPunctualLight
{
    public int DirLightNum;
    public DirectionalLight[] DirLights;
    public int PointLightNum;
    public PointLight[] PointLights;
    public int SpotLightNum;
    public SpotLight[] SpotLights;
};

public class ucExportLights
{

    static ExportPunctualLight GetLightData(ref Light[] lights)
    {
        List<DirectionalLight> dir_light_list = new List<DirectionalLight>();
        List<PointLight> point_light_list = new List<PointLight>();
        List<SpotLight> spot_light_list = new List<SpotLight>();
        int dir_light_num = 0;
        int point_light_num = 0;
        int spot_light_num = 0;

        //int index = 0;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Point)
            {
                point_light_num++;

                PointLight point_l = new PointLight();
                point_l.Color = new float[3];
                point_l.Color[0] = l.color.r * l.intensity * 10000.0f;
                point_l.Color[1] = l.color.g * l.intensity * 10000.0f;
                point_l.Color[2] = l.color.b * l.intensity * 10000.0f;

                point_l.Radius = l.range * 100;

                point_l.WorldPosition = new float[3];
                Vector3 pos = ucCoordToUE.F3(l.transform.position * 100);
                point_l.WorldPosition[0] = pos.x;
                point_l.WorldPosition[1] = pos.y;
                point_l.WorldPosition[2] = pos.z;

                point_light_list.Add(point_l);
            }
            else if (l.type == LightType.Spot)
            {
                spot_light_num++;

                SpotLight spot_l = new SpotLight();
                spot_l.Color = new float[3];
                spot_l.Color[0] = l.color.r * l.intensity * 10000.0f;
                spot_l.Color[1] = l.color.g * l.intensity * 10000.0f;
                spot_l.Color[2] = l.color.b * l.intensity * 10000.0f;

                spot_l.WorldPosition = new float[3];
                Vector3 pos = ucCoordToUE.F3(l.transform.position * 100);
                spot_l.WorldPosition[0] = pos.x;
                spot_l.WorldPosition[1] = pos.y;
                spot_l.WorldPosition[2] = pos.z;

                spot_l.Radius = l.range * 100;
                spot_l.CosOuterConeAngle = Mathf.Cos(Mathf.Deg2Rad * (l.spotAngle / 2.0f));
                spot_l.CosInnerConeAngle = Mathf.Cos(Mathf.Deg2Rad * (l.innerSpotAngle / 2.0f));

                spot_l.Direction = new float[3];
                Vector3 dir = Vector3.Normalize(ucCoordToUE.F3(l.transform.forward));
                spot_l.Direction[0] = dir.x;
                spot_l.Direction[1] = dir.y;
                spot_l.Direction[2] = dir.z;

                spot_light_list.Add(spot_l);

            }
            else if (l.type == LightType.Area)
            {
                //light_value_scale = l.areaSize.x * l.areaSize.y;
            }
            else if (l.type == LightType.Directional)
            {
                dir_light_num++;

                DirectionalLight dir_l = new DirectionalLight();

                dir_l.Color = new float[3];
                dir_l.Color[0] = l.color.r * l.intensity;
                dir_l.Color[1] = l.color.g * l.intensity;
                dir_l.Color[2] = l.color.b * l.intensity;

                dir_l.Direction = new float[3];
                Vector3 dir = ucCoordToUE.F3(l.transform.forward);
                dir_l.Direction[0] = dir.x;
                dir_l.Direction[1] = dir.y;
                dir_l.Direction[2] = dir.z;

                dir_light_list.Add(dir_l);
            }
        }

        ExportPunctualLight ret_light_data;

        ret_light_data.DirLightNum = dir_light_num;
        ret_light_data.PointLightNum = point_light_num;
        ret_light_data.SpotLightNum = spot_light_num;
        ret_light_data.DirLights = dir_light_list.ToArray();
        ret_light_data.SpotLights = spot_light_list.ToArray();
        ret_light_data.PointLights = point_light_list.ToArray();

        return ret_light_data;
    }

    static public ExportPunctualLight ExportIndirectedLights()
    {
        Light[] lights = GameObject.FindObjectsOfType(typeof(Light)) as Light[];
        List<Light> not_realtime_lights = new List<Light>();

        foreach(Light l in lights)
        {
            if(l.lightmapBakeType != LightmapBakeType.Realtime)
            {
                not_realtime_lights.Add(l);
            }
        }
        Light[] not_realtime_lights_array = not_realtime_lights.ToArray();
        return GetLightData(ref not_realtime_lights_array);
    }

    static public ExportPunctualLight ExportStaticBakedLights()
    {
        Light[] lights = GameObject.FindObjectsOfType(typeof(Light)) as Light[];
        List<Light> static_baked_lights = new List<Light>();

        foreach (Light l in lights)
        {
            if (l.lightmapBakeType == LightmapBakeType.Baked)
            {
                static_baked_lights.Add(l);
            }
        }
        Light[] static_baked_lights_array = static_baked_lights.ToArray();
        return GetLightData(ref static_baked_lights_array);
    }
}