using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Reflection;

[CustomEditor(typeof(DySkyProfile))]
public class DySkyProfileEditor : Editor
{
    DySkyProfile profile;
    DySkyProfile standard;

    List<SerializedProperty> props;
    List<object> values;
    List<object> values_standard;
    List<float> spaces;

    bool standardHide;

    private void OnEnable()
    {
        profile = this.target as DySkyProfile;

        string scriptLocation = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        string standardLocation = scriptLocation.Replace("/Editor/DySkyProfileEditor.cs", "/Profile/_Standard.asset");
        standard = AssetDatabase.LoadAssetAtPath(standardLocation, profile.GetType()) as DySkyProfile;

        props = new List<SerializedProperty>();
        values = new List<object>();
        values_standard = new List<object>();
        spaces = new List<float>();
        foreach (var field in profile.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance))
        {
            props.Add(serializedObject.FindProperty(field.Name));
            values.Add(field.GetValue(profile));
            values_standard.Add(field.GetValue(standard));
            var attrs = field.GetCustomAttributes(typeof(SpaceAttribute), false);
            spaces.Add(attrs.Length > 0 ? (attrs[0] as SpaceAttribute).height : 0);
        }

        standardHide = true;
    }

    public override void OnInspectorGUI()
    {
        if (standard == profile)
        {
            GUI.color = Color.red;
            if (standardHide)
            {
                if (GUILayout.Button("Show")) standardHide = false;
            }
            else
            {
                if (GUILayout.Button("Hide")) standardHide = true;
            }
            GUI.color = Color.white;
            if (!standardHide) base.OnInspectorGUI();
            return;
        }

        Undo.RecordObject(profile, "Undo Day Profile");

        bool reset = false;
        for (int i = 0; i < props.Count; i++)
        {
            SerializedProperty prop = props[i];
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName.Replace("Curve ", "").Replace("Grad ", "")));
            if (GUILayout.Button("R", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(15 + spaces[i])))
            {
                object value = values[i];
                object value_standard = values_standard[i];
                Type type = value_standard.GetType();
                if (type == typeof(float))
                    prop.floatValue = (float)value_standard;
                else if (type == typeof(AnimationCurve))
                    (value as AnimationCurve).keys = ((AnimationCurve)value_standard).keys;
                else if (type == typeof(Gradient))
                    (value as Gradient).SetKeys(((Gradient)value_standard).colorKeys, ((Gradient)value_standard).alphaKeys);
                else
                    Debug.LogError("Don't support type: " + type);
                reset = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();
        EditorUtility.SetDirty(target);

        if (reset)
        {
            Selection.activeObject = null;
            EditorApplication.delayCall += () =>
            {
                Selection.activeObject = profile;
            };
        }
    }
}
