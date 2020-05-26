using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Reflection;

public class DySkyShaderDiffuseEditor : ShaderGUI
{
    const string DY_SKY_ALPHA_TEST_ON = "DY_SKY_ALPHA_TEST_ON";

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        bool alphaTest = EditorGUILayout.Toggle("Alpha Test", material.IsKeywordEnabled(DY_SKY_ALPHA_TEST_ON));
        if (alphaTest != material.IsKeywordEnabled(DY_SKY_ALPHA_TEST_ON))
        {
            if (alphaTest)
            {
                material.EnableKeyword(DY_SKY_ALPHA_TEST_ON);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            else
            {
                material.DisableKeyword(DY_SKY_ALPHA_TEST_ON);
                material.renderQueue = -1;
            }
        }

        if (!alphaTest)
        {
            MaterialProperty alphaCutoff = FindProperty("_Cutoff", properties);
            List<MaterialProperty> newProps = new List<MaterialProperty>();
            foreach(var prop in properties)
            {
                if (prop != alphaCutoff) newProps.Add(prop);
            }
            properties = newProps.ToArray();
        }

        base.OnGUI(materialEditor, properties);
    }
}
