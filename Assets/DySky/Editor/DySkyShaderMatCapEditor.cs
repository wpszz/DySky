using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class DySkyShaderMatCapEditor : DySkyShaderEditor
{
    public enum ApplyMode
    {
        Base,
        Mask,
        MaskBlend,
    }

    private static GUIContent modeTips = EditorGUIUtility.TrTextContent("Apply Mode", "Determines the apply method for drawing the object to the screen.");
    private static GUIContent[] modeNames = Array.ConvertAll(Enum.GetNames(typeof(ApplyMode)), item => new GUIContent(item));

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        ApplyMode mode;
        if (material.IsKeywordEnabled(DY_SKY_MATCAP_BASE))
            mode = ApplyMode.Base;
        else if (material.IsKeywordEnabled(DY_SKY_MATCAP_MASK))
            mode = ApplyMode.Mask;
        else if (material.IsKeywordEnabled(DY_SKY_MATCAP_MASK_BLEND))
            mode = ApplyMode.MaskBlend;
        else
        {
            mode = ApplyMode.Base;
            SetupMaterialWithApplyMode(material, mode);
        }

        EditorGUI.BeginChangeCheck();
        mode = (ApplyMode)EditorGUILayout.Popup(modeTips, (int)mode, modeNames);
        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.RegisterPropertyChangeUndo("Apply Mode");
            SetupMaterialWithApplyMode(material, mode);
        }

        if (mode == ApplyMode.Base)
        {
            MaterialProperty maskProp = FindProperty("_MaskTexture", properties);
            List<MaterialProperty> newProps = new List<MaterialProperty>();
            foreach (var prop in properties)
            {
                if (prop != maskProp) newProps.Add(prop);
            }
            properties = newProps.ToArray();
        }

        base.OnGUI(materialEditor, properties);
    }

    public static void SetupMaterialWithApplyMode(Material material, ApplyMode mode)
    {
        switch (mode)
        {
            case ApplyMode.Base:
                material.EnableKeyword(DY_SKY_MATCAP_BASE);
                material.DisableKeyword(DY_SKY_MATCAP_MASK);
                material.DisableKeyword(DY_SKY_MATCAP_MASK_BLEND);
                break;
            case ApplyMode.Mask:
                material.DisableKeyword(DY_SKY_MATCAP_BASE);
                material.EnableKeyword(DY_SKY_MATCAP_MASK);
                material.DisableKeyword(DY_SKY_MATCAP_MASK_BLEND);
                break;
            case ApplyMode.MaskBlend:
                material.DisableKeyword(DY_SKY_MATCAP_BASE);
                material.DisableKeyword(DY_SKY_MATCAP_MASK);
                material.EnableKeyword(DY_SKY_MATCAP_MASK_BLEND);
                break;
        }
    }
}
