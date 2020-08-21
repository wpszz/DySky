using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class DySkyShaderVertEditor : DySkyShaderEditor
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        bool bakeMode = EditorGUILayout.Toggle("Bake mode", material.IsKeywordEnabled(DY_SKY_BAKE_MODE));
        if (bakeMode != material.IsKeywordEnabled(DY_SKY_BAKE_MODE))
        {
            if (bakeMode)
            {
                material.EnableKeyword(DY_SKY_BAKE_MODE);
            }
            else
            {
                material.DisableKeyword(DY_SKY_BAKE_MODE);
            }
        }
        base.OnGUI(materialEditor, properties);
    }
}
