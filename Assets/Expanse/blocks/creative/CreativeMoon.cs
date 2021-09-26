using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeMoon : MonoBehaviour
{

    public CelestialBodyBlock m_moonBlock;
    public bool m_useTimeOfDay = true;
    public DateTimeBlock m_dateTimeBlock;
    public Vector3 m_direction = new Vector3(30, 0, 0);
    [Range(0, 10), Tooltip("Size of the moon. 1 is physically-accurate.")]    
    public float m_size = 1;
    [Min(0), Tooltip("Brightness of the moonlight.")]
    public float m_lightBrightness = 200;
    public Color m_lightTint = new Color(1, 1, 1);
    [Min(0)]
    public Cubemap m_texture;
    public Vector3 m_rotation;
    public Color m_surfaceTint = new Color(0.5f, 0.5f, 0.5f);

    // Update is called once per frame
    void Update()
    {
        if (m_useTimeOfDay) {
            m_dateTimeBlock.m_moon = m_moonBlock;
        } else {
            if (m_dateTimeBlock != null) {
                m_dateTimeBlock.m_moon = null;
            }
            m_moonBlock.m_direction = m_direction;
        }
        m_moonBlock.m_angularRadius = 0.5f * m_size;
        m_moonBlock.m_lightIntensity = m_lightBrightness;
        m_moonBlock.m_lightColor = m_lightTint;
        m_moonBlock.m_albedoTexture = m_texture;
        m_moonBlock.m_albedoTextureRotation = m_rotation;
        m_moonBlock.m_albedoTint = m_surfaceTint;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CreativeMoon))]
public class CreativeMoonEditor : Editor
{
    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_moonBlock"));
        SerializedProperty timeOfDay = serializedObject.FindProperty("m_useTimeOfDay");
        EditorGUILayout.PropertyField(timeOfDay);
        if (timeOfDay.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_dateTimeBlock"));
        } else {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_direction"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightBrightness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightTint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_texture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_surfaceTint"));
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif // UNITY_EDITOR

}
