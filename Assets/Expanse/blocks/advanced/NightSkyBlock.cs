using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class NightSkyBlock : MonoBehaviour
{
    /* User-exposed controls. */
    [Tooltip("The rotation of the night sky specified as euler angles.")]
    public Vector3 m_rotation = new Vector3(0.0f, 0.0f, 0.0f);
    [Min(0), Tooltip("Overall intensity of the night sky.")]
    public float m_intensity = 1;
    [Tooltip("Tint to the night sky.")]
    public Color m_tint = Color.white;
    [Min(0), Tooltip("Intensity of light scattered up from the ground used for modeling light pollution. Specified in lux.")]
    public float m_lightPollutionIntensity = 0;
    [Tooltip("Color of light coming from the ground used for modeling light pollution.")]
    public Color m_lightPollutionTint = new Color(255, 140, 66);
    [Min(0), Tooltip("Expanse computes sky scattering using the average color of the sky texture. There are so many light sources in the night sky that this is really the only computationally tractable option. However, this can sometimes result in scattering that's too intense, or not intense enough, depending on your use case. This parameter is an artistic override to help mitigate that issue.")]
    public float m_scatterIntensity = 0.05f;
    [Tooltip("An additional tint applied on top of the night sky tint, but only to the scattering. This is useful as an artistsic override for if the average color of your sky texture doesn't quite get you the scattering behavior you want. For instance, you may want the scattering to be bluer.")]
    public Color m_scatterTint = Color.white;
    [Min(0), Tooltip("Multiplier to sky cubemap ambient lighting.")]
    public float m_ambientMultiplier = 1;

    // Start is called before the first frame update
    void Start()
    {
        OnEnable();
    }
    void OnEnable() 
    {
        NightSkyRenderSettings.register(this);
    }

    void OnDisable() 
    {
        NightSkyRenderSettings.deregister(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(NightSkyBlock))]
public class NightSkyBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensity"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tint"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightPollutionIntensity"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightPollutionTint"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scatterIntensity"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scatterTint"));
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse