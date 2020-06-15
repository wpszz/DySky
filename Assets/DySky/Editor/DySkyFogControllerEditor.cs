using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Reflection;

[CustomEditor(typeof(DySkyFogController))]
public class DySkyFogControllerEditor : Editor
{
    DySkyFogController fogController;

    private void OnEnable()
    {
        fogController = this.target as DySkyFogController;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);
        if (!fogController.matFogBake)
        {
            EditorGUILayout.HelpBox("Missing Fog Bake Material", MessageType.Warning);
        }
        else 
        {
            if (Application.isPlaying && GUILayout.Button("Bake and Use Fog Cube Map"))
            {
                fogController.BakeFogCubeMap();
            }

            if (GUILayout.Button("Bake and Save Fog Cube Map"))
            {
                DySkyFogCubemapWizard rcw = ScriptableWizard.DisplayWizard<DySkyFogCubemapWizard>("Bake Fog Cubemap", "Bake and Save to");
                rcw.fogController = fogController;
            }

            if (GUILayout.Button("Replace All DySky Shaders"))
            {
                ReplaceAllDySkyShaders();
            }
        }
    }

    private void ReplaceAllDySkyShaders()
    {
        Shader t4m = Shader.Find("DySky/Opaque/T4M3");
        Shader diffuse = Shader.Find("DySky/Opaque/Diffuse");
        Shader matcap = Shader.Find("DySky/Opaque/MatCap");
        Shader particle = Shader.Find("DySky/Particles/Standard");
        Shader water = Shader.Find("DySky/Water/Standard");

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat && mat.shader && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)))
                    {
                        string name = mat.shader.name;
                        if (name.Contains("T4M") && mat.shader != t4m)
                        {
                            mat.shader = t4m;
                        }
                        else if (name.Contains("Diffuse") && mat.shader != diffuse)
                        {
                            mat.shader = diffuse;
                            if (name.Contains("Cutout"))
                                mat.EnableKeyword("DY_SKY_ALPHA_TEST_ON");
                        }
                        else if (name.Contains("MatCap") && mat.shader != matcap)
                        {
                            mat.shader = matcap;
                            if (name.Contains("MaskMono"))
                            {
                                DySkyShaderMatCapEditor.SetupMaterialWithApplyMode(mat, DySkyShaderMatCapEditor.ApplyMode.MaskBlend);
                            }
                            else if (name.Contains("Mask"))
                            {
                                DySkyShaderMatCapEditor.SetupMaterialWithApplyMode(mat, DySkyShaderMatCapEditor.ApplyMode.Mask);
                            }
                            else
                            {
                                DySkyShaderMatCapEditor.SetupMaterialWithApplyMode(mat, DySkyShaderMatCapEditor.ApplyMode.Base);
                            }
                        }
                        else if (name.Contains("Particle") && mat.shader != particle)
                        {
                            mat.shader = particle;
                            if (name.Contains("Blended"))
                            {
                                DySkyShaderParticleEditor.SetupMaterialWithBlendMode(mat, DySkyShaderParticleEditor.BlendMode.Blend);
                            }
                            else if (name.Contains("AddSmooth"))
                            {
                                DySkyShaderParticleEditor.SetupMaterialWithBlendMode(mat, DySkyShaderParticleEditor.BlendMode.AddSmooth);
                            }
                            else /*if (name.Contains("Additive"))*/
                            {
                                DySkyShaderParticleEditor.SetupMaterialWithBlendMode(mat, DySkyShaderParticleEditor.BlendMode.Additive);
                            }
                        }
                        else if (name.Contains("Water") && mat.shader != water)
                        {
                            mat.shader = water;
                            r.gameObject.AddComponent<DySkyWaterController>().SetSharedMaterial(mat);
                        }
                    }
                }
            }
        }
    }
}
