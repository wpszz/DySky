using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Reflection;

[CustomEditor(typeof(DySkyController))]
public class DySkyControllerEditor : Editor
{
    DySkyController controller;

    private void OnEnable()
    {
        controller = this.target as DySkyController;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);
        if (!controller.matBake)
        {
            EditorGUILayout.HelpBox("Missing Bake Material", MessageType.Warning);
        }
        else 
        {
            if (GUILayout.Button("Bake and Save DySky Cube Map"))
            {
                DySkyCubemapWizard rcw = ScriptableWizard.DisplayWizard<DySkyCubemapWizard>("Bake DySky Cubemap", "Bake and Save to");
                rcw.controller = controller;
            }
        }
    }
}
