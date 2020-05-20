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
        }
    }
}
