using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;

[StructLayout(LayoutKind.Sequential)]
public struct ucUnityRenderOptions
{
    public int width;
    public int height;
    public float[] camera_pos;
    public float[] euler_angle;
    public float fov;

    public int sample_count;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string hdr_texture_path;
}

public class ucInteractivePTEditorWindow : ScriptableWizard
{
    private static ucInteractivePTEditorWindow window_inst = null;

    [MenuItem("Tools/Test Lightmass")]
    static void CreateW()
    {
        if(window_inst == null)
        {
            window_inst = ScriptableWizard.DisplayWizard<ucInteractivePTEditorWindow>("Baker", "Yes", "Cancel");
            window_inst.autoRepaintOnSceneChange = true;
        }
        window_inst.Focus();
    }

    bool interactive_rendering = false;
    bool lightprobe_baking = false;
    bool export_surfel_data = false;

    ucDLLFunctionCaller dll_function_caller = null;
    ucThreadDispatcher thread_dispatcher = null;

    void OnGUI()
    {

        //Start Btn, needed to add bottom after all parameters have inited.
        if (interactive_rendering != GUILayout.Toggle(interactive_rendering, new GUIContent("LightMap", "Start baking lightmap"), "Button"))
        {
            interactive_rendering = !interactive_rendering;
            if (interactive_rendering)
            {                
                //Record time
                //InteractiveRenderingStart();
            }
            else
            {
                //Debug.Log("Interactive Stop!");
                //InteractiveRenderingEnd();
            }
        }

        if (lightprobe_baking != GUILayout.Toggle(lightprobe_baking, new GUIContent("Light Probe", "Start baking light probe"), "Button"))
        {
            lightprobe_baking = !lightprobe_baking;
            if (lightprobe_baking)
            {
                //LightprobeBakingStart();
            }
            else
            {
                Debug.Log("baking lightprobe stop!");
                //LightprobeBakingEnd();
            }
        }

        if (export_surfel_data != GUILayout.Toggle(export_surfel_data, new GUIContent("GenerateSurfel", "export mesh and generate surfel data"), "Button"))
        {
            export_surfel_data = !export_surfel_data;
            if (export_surfel_data)
            {
                GenerateSurfelStart();
            }
            else
            {
                Debug.Log("GenerateSurfel stop!");
                GenerateSurfelEnd();

            }
        }
    }

    

    void InteractiveRenderingStart()
    {
        Debug.Log("Export scene data...");
        ucExportMesh.mrt_datas = ucObjectMrt.StartExportData();

        Debug.Log("Start!!!");
        if (dll_function_caller == null)
        {
            if (thread_dispatcher == null)
            {
                thread_dispatcher = ucThreadDispatcher.Initialize();
            }

            dll_function_caller = new ucDLLFunctionCaller(thread_dispatcher);
        }

        dll_function_caller.LoadDLLAndInit();
        dll_function_caller.StartBaking();
    }

    void InteractiveRenderingEnd()
    {
        if(dll_function_caller != null)
            dll_function_caller.Release();
        dll_function_caller = null;           
    }

    void LightprobeBakingStart()
    {
        Debug.Log("Export scene data...");
        ucExportMesh.mrt_datas = ucObjectMrt.StartExportData();

        if (dll_function_caller == null)
        {
            if (thread_dispatcher == null)
            {
                thread_dispatcher = ucThreadDispatcher.Initialize();
            }

            dll_function_caller = new ucDLLFunctionCaller(thread_dispatcher);
        }

        dll_function_caller.LoadDLLAndInit();
        dll_function_caller.StartLightprobeBaking();
    }

    void LightprobeBakingEnd()
    {
        if (dll_function_caller != null)
            dll_function_caller.Release();
        dll_function_caller = null;
    }

    void GenerateSurfelStart()
    {
        Debug.Log("Export scene data...");
        //ucExportMesh.mrt_datas = ucObjectMrt.StartExportData();

        if (dll_function_caller == null)
        {
            if (thread_dispatcher == null)
            {
                thread_dispatcher = ucThreadDispatcher.Initialize();
            }

            dll_function_caller = new ucDLLFunctionCaller(thread_dispatcher);
        }

        dll_function_caller.LoadDLLAndInitSurfel();
        //dll_function_caller.StartGenerateSurfelData();
        dll_function_caller.StartDebugDirectionalData();
    }

    void GenerateSurfelEnd()
    {
        if (dll_function_caller != null)
            dll_function_caller.Release();
        dll_function_caller = null;
    }

    void OnDestroy()
    {
        if (dll_function_caller != null)
            dll_function_caller.Release();

        window_inst = null;
    }

    void OnWizardUpdate()
    {        
    }

    void OnHierarchyChange()
    {
    }

    public void Update()
    {
        Repaint();
    }

}
