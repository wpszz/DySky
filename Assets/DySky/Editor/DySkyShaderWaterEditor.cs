﻿using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class DySkyShaderWaterEditor : DySkyShaderEditor
{
    MaterialProperty[] emptyProps = new MaterialProperty[0];

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        MaterialProperty foamProp = FindProperty("_EdgeFoamTex", properties);
        MaterialProperty foamFreqProp = FindProperty("_EdgeFoamFreq", properties);
        foreach (var prop in properties) {
            if (prop == foamProp || prop == foamFreqProp)
            {
                bool foam = material.IsKeywordEnabled(DY_SKY_FOAM_EDGE_ENABLE);
                if (prop == foamProp)
                {
                    foam = EditorGUILayout.Toggle("Edge Foam", material.IsKeywordEnabled(DY_SKY_FOAM_EDGE_ENABLE));
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
