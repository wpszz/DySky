using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class DySkyShaderParticleEditor : DySkyShaderEditor
{
    public enum BlendMode
    {
        Additive,
        AddSmooth,
        Blend
    }

    private static GUIContent renderingMode = EditorGUIUtility.TrTextContent("Rendering Mode", "Determines the blending method for drawing the object to the screen.");
    private static GUIContent[] blendNames = Array.ConvertAll(Enum.GetNames(typeof(BlendMode)), item => new GUIContent(item));

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        BlendMode mode;
        if (material.IsKeywordEnabled(DY_SKY_PARTICLE_ADD))
            mode = BlendMode.Additive;
        else if (material.IsKeywordEnabled(DY_SKY_PARTICLE_ADD_SMOOTH))
            mode = BlendMode.AddSmooth;
        else if (material.IsKeywordEnabled(DY_SKY_PARTICLE_BLEND))
            mode = BlendMode.Blend;
        else
        {
            mode = BlendMode.Additive;
            SetupMaterialWithBlendMode(material, mode);
        }

        EditorGUI.BeginChangeCheck();
        mode = (BlendMode)EditorGUILayout.Popup(renderingMode, (int)mode, blendNames);
        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
            SetupMaterialWithBlendMode(material, mode);
        }

        base.OnGUI(materialEditor, properties);
    }

    public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Additive:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.EnableKeyword(DY_SKY_PARTICLE_ADD);
                material.DisableKeyword(DY_SKY_PARTICLE_ADD_SMOOTH);
                material.DisableKeyword(DY_SKY_PARTICLE_BLEND);
                break;
            case BlendMode.AddSmooth:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                material.DisableKeyword(DY_SKY_PARTICLE_ADD);
                material.EnableKeyword(DY_SKY_PARTICLE_ADD_SMOOTH);
                material.DisableKeyword(DY_SKY_PARTICLE_BLEND);
                break;
            case BlendMode.Blend:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.DisableKeyword(DY_SKY_PARTICLE_ADD);
                material.DisableKeyword(DY_SKY_PARTICLE_ADD_SMOOTH);
                material.EnableKeyword(DY_SKY_PARTICLE_BLEND);
                break;
        }
    }
}
