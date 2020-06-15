using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class DySkyShaderPBREditor : DySkyShaderEditor
{
    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        //if (newShader && newShader.name == "DySky/Opaque/PBR")
        {
            Texture texEnvBRDFLUT = material.GetTexture("_EnvBRDFLUT");
            if (!texEnvBRDFLUT)
            {
                string shaderLocation = AssetDatabase.GetAssetPath(newShader);
                string lutLocation = shaderLocation.Replace("/Shader/DySkyApplyPBR.shader", "/Texture/ENV_BRDF_LUT.exr");
                texEnvBRDFLUT = AssetDatabase.LoadAssetAtPath(lutLocation, typeof(Texture)) as Texture;
                material.SetTexture("_EnvBRDFLUT", texEnvBRDFLUT);
            }
        }
    }

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
