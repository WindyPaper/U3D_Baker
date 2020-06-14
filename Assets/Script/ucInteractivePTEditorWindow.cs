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
            window_inst = ScriptableWizard.DisplayWizard<ucInteractivePTEditorWindow>("RayTracingPreview", "Yes", "Cancel");
            window_inst.autoRepaintOnSceneChange = true;
        }
        window_inst.Focus();
    }

    bool interactive_rendering = false;
    public static bool select_sceneview_active_camera = true;
    public static bool enable_denoise = true;
    public static Camera cam = null;
    //bool pressed = false;
    //ucRenderDeviceOptions render_device_op;
    //public static int sample_count = 128;
    //ucSampleCountOptions sample_count_op;
    //public static float render_progress = 0.0f;

    //DateTime start_time = System.DateTime.Now;
    //TimeSpan offset_time;

    //float save_main_camera_far_clip_value = 0.0f;

    public static Cubemap hdr_texture = null;

    ucDLLFunctionCaller dll_function_caller = null;
    ucThreadDispatcher thread_dispatcher = null;

    void OnGUI()
    {

        //Start Btn, needed to add bottom after all parameters have inited.
        if (interactive_rendering != GUILayout.Toggle(interactive_rendering, new GUIContent("Start", "Ray tracing result will be outputed to GameView"), "Button"))
        {
            interactive_rendering = !interactive_rendering;
            if (interactive_rendering)
            {                
                //Record time
                InteractiveRenderingStart();
            }
            else
            {
                //Debug.Log("Interactive Stop!");
                InteractiveRenderingEnd();
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

        dll_function_caller.LoadDLLAndInitCycles();               

        //Thread t = new Thread(dll_function_caller.InteractiveRenderStart(render_options));
        //t.Start();        
    }

    void InteractiveRenderingEnd()
    {
        if(dll_function_caller != null)
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
        //Debug.Log("OnHierarchyChange");
    }

    public void Update()
    {
        //if (thread_dispatcher != null)
        //{
        //    thread_dispatcher.Update();
        //}

        Repaint();
    }

}
