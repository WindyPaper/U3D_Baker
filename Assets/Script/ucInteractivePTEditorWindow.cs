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
    public static Cubemap hdr_texture = null;

    ucDLLFunctionCaller dll_function_caller = null;
    ucThreadDispatcher thread_dispatcher = null;

    void OnGUI()
    {

        //Start Btn, needed to add bottom after all parameters have inited.
        if (interactive_rendering != GUILayout.Toggle(interactive_rendering, new GUIContent("Start", "Start baking"), "Button"))
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

        dll_function_caller.LoadDLLAndInit();
        dll_function_caller.StartBaking();
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
    }

    public void Update()
    {
        Repaint();
    }

}
