using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class DySkyShaderWaterEditor : DySkyShaderEditor
{
    MaterialProperty[] emptyProps = new MaterialProperty[0];

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        Texture texWave= material.GetTexture("_WaveTex");
        if (!texWave)
        {
            string shaderLocation = AssetDatabase.GetAssetPath(newShader);
            string assetLocation = shaderLocation.Replace("/Shader/DySkyApplyWater.shader", "/Texture/Wave.psd");
            texWave = AssetDatabase.LoadAssetAtPath(assetLocation, typeof(Texture)) as Texture;
            material.SetTexture("_WaveTex", texWave);
        }

        Texture texFoam = material.GetTexture("_EdgeFoamTex");
        if (!texFoam)
        {
            string shaderLocation = AssetDatabase.GetAssetPath(newShader);
            string assetLocation = shaderLocation.Replace("/Shader/DySkyApplyWater.shader", "/Texture/FoamGrad.bmp");
            texWave = AssetDatabase.LoadAssetAtPath(assetLocation, typeof(Texture)) as Texture;
            material.SetTexture("_EdgeFoamTex", texWave);
        }

        material.EnableKeyword(DY_SKY_FOAM_EDGE_ENABLE);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        MaterialProperty reflectColor = FindProperty("_ReflectColor", properties);
        MaterialProperty foamProp = FindProperty("_EdgeFoamTex", properties);
        MaterialProperty foamScaleProp = FindProperty("_EdgeFoamScale", properties);
        foreach (var prop in properties) {
            if (prop == reflectColor)
            {
                bool reflectSky = EditorGUILayout.Toggle("Reflect DySky", material.IsKeywordEnabled(DY_SKY_REFLECT_SKY));
                if (reflectSky != material.IsKeywordEnabled(DY_SKY_REFLECT_SKY))
                {
                    if (reflectSky)
                    {
                        material.EnableKeyword(DY_SKY_REFLECT_SKY);
                    }
                    else
                    {
                        material.DisableKeyword(DY_SKY_REFLECT_SKY);
                    }
                }
                if (reflectSky) continue;
            }
            else if (prop == foamProp || prop == foamScaleProp)
            {
                bool foam = material.IsKeywordEnabled(DY_SKY_FOAM_EDGE_ENABLE);
                if (prop == foamProp)
                {
                    foam = EditorGUILayout.Toggle("Foam", material.IsKeywordEnabled(DY_SKY_FOAM_EDGE_ENABLE));
                    if (foam != material.IsKeywordEnabled(DY_SKY_FOAM_EDGE_ENABLE))
                    {
                        if (foam)
                        {
                            material.EnableKeyword(DY_SKY_FOAM_EDGE_ENABLE);
                        }
                        else
                        {
                            material.DisableKeyword(DY_SKY_FOAM_EDGE_ENABLE);
                        }
                    }
                }
                if (!foam) continue;
            }

            if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) != 0) continue;

            if (prop.type == MaterialProperty.PropType.Texture)
                materialEditor.TextureProperty(prop, prop.displayName);
            else if (prop.type == MaterialProperty.PropType.Color)
                materialEditor.ColorProperty(prop, prop.displayName);
            else
                materialEditor.ShaderProperty(prop, prop.displayName);
        }

        base.OnGUI(materialEditor, emptyProps);
    }
}
