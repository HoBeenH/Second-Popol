using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class AerialPerspectiveSettingsBlock : MonoBehaviour
{
    /* User-exposed controls. */
    [Min(0), Tooltip("How aggressively aerial perspective due to Rayleigh and Isotropic (\"uniform\") layers is attenuated as a consequence of approximate volumetric shadowing. To see the effect, put the sun behind a big piece of geometry (like a mountain) and play around with this parameter. Expanse does not accurately model atmospheric volumetric shadows due to the performance cost, and instead uses this approximation to avoid visual artifacts.")]
    public float m_uniformOcclusionSpread = 0.5f;
    [Min(0), Tooltip("Provides a way of offsetting the attenuation of aerial perspective as a consequence of approximate volumetric shadowing (for Rayleigh and Isotropic (\"uniform\") layers). To see the effect, put the sun behind a big piece of geometry (like a mountain) and play around with this parameter. Expanse does not accurately model atmospheric volumetric shadows due to the performance cost, and instead uses this approximation to avoid visual artifacts.")]
    public float m_uniformOcclusionBias = 0.25f;
    [Min(0), Tooltip("How aggressively aerial perspective due to Mie (\"directional\") layers is attenuated as a consequence of approximate volumetric shadowing. To see the effect, put the sun behind a big piece of geometry (like a mountain) and play around with this parameter. Expanse does not accurately model atmospheric volumetric shadows due to the performance cost, and instead uses this approximation to avoid visual artifacts.")]
    public float m_directionalOcclusionSpread = 1;
    [Min(0), Tooltip("Provides a way of offsetting the attenuation of aerial perspective as a consequence of approximate volumetric shadowing (for Mie (\"directional\") layers). To see the effect, put the sun behind a big piece of geometry (like a mountain) and play around with this parameter. Expanse does not accurately model atmospheric volumetric shadows due to the performance cost, and instead uses this approximation to avoid visual artifacts.")]
    public float m_directionalOcclusionBias = 0.02f;
    [Min(0), Tooltip("The night scattering effect can sometimes be either too intense or not intense enough for aerial perspective. This artistic override allows for attenuation of night scattering for aerial perspective only.")]
    public float m_nightScatteringMultiplier = 1;

    void OnEnable() 
    {
        AerialPerspectiveRenderSettings.register(this);
    }

    void OnDisable() 
    {
        AerialPerspectiveRenderSettings.deregister(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(AerialPerspectiveSettingsBlock))]
public class AerialPerspectiveSettingsBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_uniformOcclusionSpread"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_uniformOcclusionBias"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_directionalOcclusionSpread"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_directionalOcclusionBias"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_nightScatteringMultiplier"));
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse