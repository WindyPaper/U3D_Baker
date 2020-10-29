using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ucCreate3DTexture
{
    unsafe static Color _GetSHData(GIVolumeSHData data)
    {
        return new Color(data.sh_vector.r.v[0], 
            data.sh_vector.r.v[1],
            data.sh_vector.r.v[2],
            data.sh_vector.r.v[3]);
    }

    public static void CreateSH3DTexture(int lenx, int leny, int lenz, GIVolumeSHData[] shdata, string volume_name)
    {
        // Create a 3-dimensional array to store color data
        int tex_pixel_size = lenx * leny * lenz;
        Color[] colors_r = new Color[tex_pixel_size];
        Color[] colors_g = new Color[tex_pixel_size];
        Color[] colors_b = new Color[tex_pixel_size];

        // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
        //float inverseResolution = 1.0f / (size - 1.0f);
        for (int z = 0; z < lenz; z++)
        {
            int zOffset = z * lenx * leny;
            for (int y = 0; y < leny; y++)
            {
                int yOffset = y * lenx;
                for (int x = 0; x < lenx; x++)
                {
                    int idx = x + yOffset + zOffset;
                    colors_r[idx] = _GetSHData(shdata[idx]);
                }
            }
        }

        Create(lenx, leny, lenz, colors_r, volume_name + "_r");
        Create(lenx, leny, lenz, colors_g, volume_name + "_g");
        Create(lenx, leny, lenz, colors_b, volume_name + "_b");
    }

    static void Create(int lenx, int leny, int lenz, Color[] shd, string tex_name)
    {
        // Configure the texture
        //int size = 32;
        TextureFormat format = TextureFormat.RGBAHalf;
        TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        FilterMode fm = FilterMode.Bilinear;

        // Create the texture and apply the configuration
        Texture3D texture = new Texture3D(lenx, leny, lenz, format, false);
        texture.wrapMode = wrapMode;
        texture.filterMode = fm;        

        // colors = shd;

        // Copy the color values to the texture
        texture.SetPixels(shd);

        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply();

        // Save the texture to your Unity Project
        AssetDatabase.CreateAsset(texture, "Assets/3DSHTexture/" + tex_name + ".asset");
    }
}
