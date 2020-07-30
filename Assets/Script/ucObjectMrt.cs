using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public struct ucMeshLightmapData
{
    public string name;
    public int size;
    public RenderTexture[] rt_texs;
    public RenderTexture[] gbuf_texs;
}

public static class ucObjectMrt
{
    static Material gbuf_material;
    static Material dilate_material;
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
            mesh_lm_datas[i].gbuf_texs = new RenderTexture[texture_size];

            RenderObjectToBuffer(mr, ref mesh_lm_datas[i].rt_texs, ref mesh_lm_datas[i].gbuf_texs, mesh_lm_datas[i].size, mesh_lm_datas[i].size);

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

        Shader dilate_shader = Shader.Find("Unlit/DilateTexture");
        dilate_material = new Material(dilate_shader);
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

    static void RenderObjectToBuffer(MeshRenderer mesh, ref RenderTexture[] out_tex, ref RenderTexture[] gbuf_tex, int w, int h)
    {
        CommandBuffer cbr = new CommandBuffer();
        cbr.name = mesh.name + "_rt";
        RenderTargetIdentifier[] rt_gbuffer_id = new RenderTargetIdentifier[gbuf_tex.Length];
        //RenderTexture[] gbuf_tex = new RenderTexture[out_tex.Length];

        int aa_level = 1;

        for(int i = 0; i < gbuf_tex.Length; ++i)
        {
            gbuf_tex[i] = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);
            rt_gbuffer_id[i] = gbuf_tex[i];
            gbuf_tex[i].name = cbr.name + "_" + i;
        }
        RenderTexture depthBuffer = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);

        cbr.SetRenderTarget(rt_gbuffer_id, depthBuffer);
        cbr.ClearRenderTarget(true, true, Color.clear, 1);
        cbr.DrawRenderer(mesh, gbuf_material);

        Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cbr);
        Graphics.ExecuteCommandBuffer(cbr);
        //Camera.current.Render();

        DilateTexture(ref out_tex, ref gbuf_tex, w, h);
    }

    static void DilateTexture(ref RenderTexture[] out_tex, ref RenderTexture[] gbuf_tex, int w, int h)
    {
        CommandBuffer cbr = new CommandBuffer();
        cbr.name = "DilateTexture";
        RenderTargetIdentifier[] rt_gbuffer_id = new RenderTargetIdentifier[out_tex.Length];

        int aa_level = 1;

        for (int i = 0; i < out_tex.Length; ++i)
        {
            out_tex[i] = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);
            rt_gbuffer_id[i] = out_tex[i];
            out_tex[i].name = cbr.name + "_" + i;
        }
        RenderTexture depthBuffer = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, aa_level);

        cbr.SetRenderTarget(rt_gbuffer_id, depthBuffer);
        cbr.ClearRenderTarget(true, true, Color.clear, 1);

        Mesh mesh = new Mesh();        
        //DX
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-1, -1, 0),
            new Vector3(-1, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, -1, 0)
        };
        mesh.vertices = vertices;
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };
        mesh.uv = uv;
        mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);

        Texture2D color_tex = toTexture2D(gbuf_tex[0], w);
        color_tex.filterMode = FilterMode.Point;
        dilate_material.SetTexture("_ColorTex", color_tex);
        Texture2D normal_tex = toTexture2D(gbuf_tex[1], w);
        normal_tex.filterMode = FilterMode.Point;
        dilate_material.SetTexture("_NormalTex", normal_tex);
        Texture2D pos_tex = toTexture2D(gbuf_tex[2], w);
        pos_tex.filterMode = FilterMode.Point;
        dilate_material.SetTexture("_PosTex", pos_tex);        
        dilate_material.SetVector("_PixelOffset", new Vector4(1.0f/w, 1.0f/h, 0.0f, 0.0f));
        cbr.DrawMesh(mesh, Matrix4x4.identity, dilate_material);

        Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cbr);
        Graphics.ExecuteCommandBuffer(cbr);
    }
}
