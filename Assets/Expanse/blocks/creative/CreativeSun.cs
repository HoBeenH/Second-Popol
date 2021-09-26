using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeSun : MonoBehaviour
{

    public CelestialBodyBlock m_sunBlock;
    public bool m_useTimeOfDay = true;
    public DateTimeBlock m_dateTimeBlock;
    public Vector3 m_direction = new Vector3(30, 0, 0);
    [Range(0, 10), Tooltip("Size of the sun. 1 is physically-accurate.")]    
    public float m_size = 1;
    [Min(0), Tooltip("Brightness of the sunlight. 150000 is physically-accurate.")]
    public float m_lightBrightness = 150000;
    public Color m_lightTint = new Color(1, 1, 1);
    [Min(0)]
    public float m_discBrightness = 1;
    public Color m_discTint = new Color(1, 1, 1);

    // Update is called once per frame
    void Update()
    {
        if (m_useTimeOfDay) {
            m_dateTimeBlock.m_sun = m_sunBlock;
        } else {
            if (m_dateTimeBlock != null) {
                m_dateTimeBlock.m_sun = null;
            }
            m_sunBlock.m_direction = m_direction;
        }
        m_sunBlock.m_lightIntensity = m_lightBrightness;
        m_sunBlock.m_angularRadius = 0.5f * m_size;
        m_sunBlock.m_lightColor = m_lightTint;
        m_sunBlock.m_emissionTint = m_discTint;
        m_sunBlock.m_emissionMultiplier = m_discBrightness;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CreativeSun))]
public class CreativeSunEditor : Editor
{
    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sunBlock"));
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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_discBrightness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_discTint"));
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif // UNITY_EDITOR

} // namespace Expanse
