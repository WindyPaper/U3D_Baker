using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public struct ucMeshLightmapData
{
    public string name;
    public int size;
    public RenderTexture[] rt_texs;
}

public static class ucObjectMrt
{
    static Material gbuf_material;
    static ucMeshLightmapData[] mesh_lm_datas;

    // Start is called before the first frame update
    public static ucMeshLightmapData[] StartExportData()
    {
        Init();

        StartExport();

        //foreach (MeshLightmapData d in mesh_lm_datas)
        //{
        //    foreach (RenderTexture t in d.rt_texs)
        //    {
        //        byte[] bytes = toTexture2D(t, d.size).GetRawTextureData();
        //    }
        //}

        return mesh_lm_datas;
    }

    static private List<MeshFilter> GetAllObjectsInScene()
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

    static void StartExport()
    {
        List<MeshFilter> objs = GetAllObjectsInScene();

        mesh_lm_datas = new ucMeshLightmapData[objs.Count];

        int default_size = 64;
        int texture_size = 3;

        int i = 0;
        foreach (MeshFilter mf in objs)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();

            mesh_lm_datas[i].name = mr.name;
            mesh_lm_datas[i].size = default_size;
            mesh_lm_datas[i].rt_texs = new RenderTexture[texture_size];

            RenderObjectToBuffer(mr, ref mesh_lm_datas[i].rt_texs, mesh_lm_datas[i].size, mesh_lm_datas[i].size);

            ++i;
        }        
    }

    //public void SaveTexture(RenderTexture rt, string name, int size)
    //{
    //    //byte[] bytes = toTexture2D(rt, size).EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
    //    byte[] bytes = toTexture2D(rt, size).GetRawTextureData();
    //    System.IO.File.WriteAllBytes("./Assets/RTData/" + name + ".DATA", bytes);
    //}

    static public Texture2D toTexture2D(RenderTexture rTex, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    static void Init()
    {
        Shader gbuf_shader = Shader.Find("Unlit/GBuffer");
        gbuf_material = new Material(gbuf_shader);


    }

    //void OnDestroy()
    //{
    //    //Debug.Log("OnDestroy1");

    //    foreach (MeshLightmapData d in mesh_lm_datas)
    //    {
    //        foreach (RenderTexture t in d.rt_texs)
    //        {
    //            SaveTexture(t, t.name, d.size);
    //        }
    //    }
    //}

    // Update is called once per frame
    //void Update()
    //{
        
    //}

    static void RenderObjectToBuffer(MeshRenderer mesh, ref RenderTexture[] out_tex, int w, int h)
    {
        CommandBuffer cbr = new CommandBuffer();
        cbr.name = mesh.name + "_rt";
        RenderTargetIdentifier[] rt_gbuffer_id = new RenderTargetIdentifier[out_tex.Length];

        int aa_level = 1;

        for(int i = 0; i < out_tex.Length; ++i)
        {
            out_tex[i] = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);
            rt_gbuffer_id[i] = out_tex[i];
            out_tex[i].name = cbr.name + "_" + i;
        }
        RenderTexture depthBuffer = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);

        cbr.SetRenderTarget(rt_gbuffer_id, depthBuffer);
        cbr.ClearRenderTarget(true, true, Color.clear, 1);
        cbr.DrawRenderer(mesh, gbuf_material);

        Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cbr);
        Graphics.ExecuteCommandBuffer(cbr);        
        //Camera.current.Render();
    }
}
