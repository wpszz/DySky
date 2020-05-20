using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Reflection;

public class DySkyFogCubemapWizard : ScriptableWizard
{
    [HideInInspector]
    public DySkyFogController fogController;

    public int size = 256;

    void OnWizardUpdate()
    {
        if (!fogController) errorString = "Missing DySkyFogController";
        else errorString = "";
        isValid = fogController;
    }

    void OnWizardCreate()
    {
        Cubemap cubemap = new Cubemap(size, TextureFormat.ARGB32, false);
        if (!fogController.BakeFogCubeMap(cubemap)) return;
        string path = EditorUtility.SaveFilePanel("Save to", "", "cubemap", "png");
        if (!string.IsNullOrEmpty(path))
        {
            Texture2D tex2d = new Texture2D(size * 6, size, TextureFormat.RGB24, false);
            for (CubemapFace cf = CubemapFace.PositiveX; cf <= CubemapFace.NegativeZ; cf++)
            {
                int idx = (int)cf;
                //if (cf == CubemapFace.PositiveY) idx = (int)CubemapFace.NegativeY;
                //else if (cf == CubemapFace.NegativeY) idx = (int)CubemapFace.PositiveY;
                Color[] pixels = cubemap.GetPixels(cf);
                Color[] flipPixels = new Color[pixels.Length];
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        flipPixels[x + y * size] = pixels[x + (size - y - 1) * size];
                tex2d.SetPixels(size * idx, 0, size, size, flipPixels);
            }
            byte[] bytes = tex2d.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            GameObject.DestroyImmediate(tex2d);
        }
        GameObject.DestroyImmediate(cubemap);
    }

    void OnInspectorUpdate()
    {
        this.OnWizardUpdate();
    }
}
