using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class AtmosphereLayerBlock : MonoBehaviour
{
    /* Name. Used to refer to the layer in UI and printouts. */
    public string m_name = "defaultLayerName";

    /* User-exposed controls. */
    /* Modeling. */
    [Tooltip("Density distribution type for this atmosphere layer.")]
    public AtmosphereDatatypes.DensityDistribution m_densityDistribution = AtmosphereDatatypes.DensityDistribution.Exponential;
    [Min(0), Tooltip("Density of this atmosphere layer.")]
    public float m_density = 1;
    [Tooltip("Height of this atmosphere layer in world units.")]
    public float m_height = 25000;
    [Min(0), Tooltip("Thickness of this atmosphere layer in world units.")]
    public float m_thickness = 8000;

    /* Lighting. */ 
    [ColorUsageAttribute(true,true), Tooltip("Extinction coefficients control the absorption of light by the atmosphere.")]
    public Color m_extinctionCoefficients = new Color(0.0000058f, 0.0000135f, 0.0000331f, 1.0f);
    [ColorUsageAttribute(true,true), Tooltip("Scattering coefficients control the scattering of light by the atmosphere. Should be less than extinction to remain physical.")]
    public Color m_scatteringCoefficients = new Color(0.0000058f, 0.0000135f, 0.0000331f, 1.0f);
    [Tooltip("Phase function to use for this atmosphere layer. Isotropic phase functions are useful for modeling simple non-directional scattering. The Rayleigh phase function is useful for modeling air and gases. The Mie phase function is good for modeling smoke, fog, and aerosols.")]
    public AtmosphereDatatypes.PhaseFunction m_phaseFunction = AtmosphereDatatypes.PhaseFunction.Rayleigh;
    [Range(-1, 1), Tooltip("Anisotropy of this atmosphere layer. Higher values will give more forward scattering. Lower values will give more backward scattering. A value of zero is neutral---i.e. it will produce \"isotropic\" scattering.")]
    public float m_anisotropy = 0.7f;
    [Tooltip("Tint to this atmosphere layer. Artistic override. A tint of perfect grey (127, 127, 127) is neutral. It's often preferable to first adjust the scattering and extinction coefficients, as this will alter the color in a way that is physically-based.")]
    public Color m_tint = Color.grey;
    [Min(0), Tooltip("Multiple scattering multipler for this atmosphere layer. Artistic override. 1 is a physically accurate value.")]
    public float m_multipleScatteringMultiplier = 1;
    [Tooltip("Whether to use proper physical lighting or a cheaper approximation for screenspace distributions.")]
    public bool m_physicalLighting = false;

    /* Shadows. */
    [Tooltip("Whether or not to compute screenspace volumetric shadows from geometry for this layer.")]
    public bool m_geometryShadows = false;
    [Tooltip("Whether or not to compute screenspace volumetric shadows from clouds for this layer. Disable if you aren't using any clouds.")]
    public bool m_cloudShadows = false;
    [Range(0, 1), Tooltip("The maximum occlusion value that volumetric geometry shadows can have. This is useful for allowing some light to leak in and soften the volumetric shadows.")]
    public float m_maxGeometryOcclusion = 0.9f;
    [Range(0, 1), Tooltip("The maximum occlusion value that volumetric cloud shadows can have. This is useful for allowing some light to leak in and soften the volumetric shadows.")]
    public float m_maxCloudOcclusion = 0.9f;

    void OnEnable() 
    {
        AtmosphereLayerRenderSettings.register(this);
    }

    void OnDisable() 
    {
        AtmosphereLayerRenderSettings.deregister(this);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AtmosphereLayerBlock))]
public class AtmosphereLayerBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_name"));
    
    
    SerializedProperty densityDistribution = serializedObject.FindProperty("m_densityDistribution");
    EditorGUILayout.PropertyField(densityDistribution);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_density"));
    if (densityDistribution.enumValueIndex == (int) AtmosphereDatatypes.DensityDistribution.Tent) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_thickness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_height"));
    } else if (densityDistribution.enumValueIndex == (int) AtmosphereDatatypes.DensityDistribution.ScreenspaceUniform) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_height"), new GUIContent("Radius"));
    } else if (densityDistribution.enumValueIndex == (int) AtmosphereDatatypes.DensityDistribution.ScreenspaceHeightFog) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_thickness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_height"), new GUIContent("Radius"));
    } else {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_thickness"));
    }
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_extinctionCoefficients"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scatteringCoefficients"));

    SerializedProperty phaseFunction = serializedObject.FindProperty("m_phaseFunction");
    EditorGUILayout.PropertyField(phaseFunction);
    if (phaseFunction.enumValueIndex == (int) AtmosphereDatatypes.PhaseFunction.Mie) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_anisotropy"));
    }

    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tint"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringMultiplier"));
    if (densityDistribution.enumValueIndex == (int) AtmosphereDatatypes.DensityDistribution.ScreenspaceUniform ||
        densityDistribution.enumValueIndex == (int) AtmosphereDatatypes.DensityDistribution.ScreenspaceHeightFog) {
        SerializedProperty geoShadows = serializedObject.FindProperty("m_geometryShadows");
        SerializedProperty cloudShadows = serializedObject.FindProperty("m_cloudShadows");
        EditorGUILayout.PropertyField(geoShadows);
        if (geoShadows.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxGeometryOcclusion"));
        }
        EditorGUILayout.PropertyField(cloudShadows);
        if (cloudShadows.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxCloudOcclusion"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_physicalLighting"));
    }

    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse
