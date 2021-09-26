using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class QualitySettingsBlock : MonoBehaviour
{
    /* User-exposed controls. */

    [Tooltip("Whether or not to use MSAA 8x anti-aliasing. Expanse uses conditional MSAA, only multisampling on the edges of celestial bodies and the ground, so enabling this should not cause much of a performance hit.")]
    public bool m_antiAlias = true;
    [Tooltip("Whether or not to use dithering, to reduce color banding. Since expanse computes everything in floating point HDR values, this is more of a de-band operation than a true dither, and you may be better off using a dither post-process step on your camera.")]
    public bool m_dither = true;
    [Range(0.25f, 1), Tooltip("Specifies resolution to render clouds at, as a function of the camera resolution. 1 means full resolution. 0.5 means half resolution.")]
    public float m_cloudSubresolution = 1;
    [Tooltip("Uses cloud layer height, instead of per-pixel logic, to determine composition order of cloud layers. This is an optimization for rendering many cloud layers from the ground and will produce inconsistent results when the camera is between, above, or inside cloud layers.")]
    public bool m_compositeCloudsByHeight = false;
    [Min(0), Tooltip("Size of cloud shadow map film plane. This limits how far out cloud shadows can extend.")]
    public float m_cloudShadowMapFilmPlaneScale = 30000;
    [Tooltip("Quality of cloud shadowmap texture. Increasing this will reduce performance.")]
    public Datatypes.Quality m_cloudShadowMapQuality = Datatypes.Quality.Low;
    [Tooltip("Quality of atmosphere lookup textures.")]
    public Datatypes.Quality m_atmosphereTextureQuality = Datatypes.Quality.High;
    [Tooltip("Quality of screenspace fog textures.")]
    public Datatypes.Quality m_screenspaceFogQuality = Datatypes.Quality.Medium;
    [Range(1, 256), Tooltip("The number of samples used when computing transmittance lookup tables. With importance sampling turned on, a value of as low as 10 gives near-perfect results on the ground. A value as low as 4 is ok if some visible inaccuracy is tolerable. Without importantance sampling, a value of 32 or higher is recommended.")]
    public int m_transmittanceSamples = 12;
    [Range(1, 256), Tooltip("The number of samples used when computing light pollution. With importance sampling turned on, a value of as low as 10 gives near-perfect results on the ground. A value as low as 8 is ok if some visible inaccuracy is tolerable. Without importantance sampling, a value of 64 or higher is recommended.")]
    public int m_aerialPerspectiveSamples = 12;
    [Range(1, 256), Tooltip("The number of samples used when computing single scattering. With importance sampling turned on, a value of as low as 10 gives near-perfect results on the ground. A value as low as 5 is ok if some visible inaccuracy is tolerable. Without importantance sampling, a value of 32 or higher is recommended.")]
    public int m_singleScatteringSamples = 32;
    [Range(1, 256), Tooltip("The number of samples to use when computing the initial isotropic estimate of multiple scattering. Importance sampling does not apply here. To get a near-perfect result, around 15 samples is necessary. But it is a fairly subtle effect, so as low as 6 samples gives a decent result.")]
    public int m_multipleScatteringSamples = 16;
    [Range(1, 256), Tooltip("The number of samples to use when computing the actual accumulated estimate of multiple scattering from the isotropic estimate. The number of samples to use when computing the initial isotropic estimate of multiple scattering. With importance sample, 8 samples gives a near-perfect result. However, multiple scattering is a fairly subtle effect, so as low as 3 samples gives a decent result. Without importance sampling, a value of 32 or higher is necessary for near perfect results, but a value of 4 is sufficient for most needs.")]
    public int m_multipleScatteringAccumulationSamples = 12;
    [Range(1, 128), Tooltip("The number of samples to use when computing physical scattering for screenspace layers. If no screenspace layers use physical lighting, then this parameter has no effect.")]
    public int m_screenspaceScatteringSamples = 24;
    [Range(1, 128), Tooltip("The number of samples to use when computing the occlusion estimate for screenspace layers.")]
    public int m_screenspaceOcclusionSamples = 24;
    [Tooltip("Whether or not to use importance sampling for all atmosphere calculations except aerial perspective. Importance sampling is a sample distribution strategy that increases fidelity given a limited budget of samples. It is recommended to turn it on, as it doesn't decrease fidelity, but does allow for fewer samples to be taken, boosting performance. However, for outer-space perspectives, it can sometimes introduce inaccuracies, so it can be useful to increase sample counts and turn off importance sampling in those cases.")]
    public bool m_importanceSampleAtmosphere = true;
    [Tooltip("Whether or not to use importance sampling for aerial perspective. Importance sampling is a sample distribution strategy that increases fidelity given a limited budget of samples. However, it can sometimes cause artifacts or perform poorly when computing aerial perspective, so the option to turn it off for aerial perspective only is provided.")]
    public bool m_importanceSampleAerialPerspective = false;
    [Range(0.25f, 30), Tooltip("Skews precomputed aerial perspective samples to be further from the camera (if less than 1) or closer to the camera (if greater than 1). This is useful for environments with very heavy fog, where it can be more important to capture scattering close to the camera.")]
    public float m_aerialPerspectiveDepthSkew = 5;
    [Range(0.25f, 30), Tooltip("Skews screenspace fog samples to be further from the camera (if less than 1) or closer to the camera (if greater than 1). This is useful for environments with very heavy fog, where it can be more important to capture scattering close to the camera.")]
    public float m_screenspaceFogDepthSkew = 3;
    [Range(1, 8), Tooltip("Downscale factor for depth buffer used for occlusion in screenspace atmosphere layers. If this factor is lower, performance is worse but the volumetric shadows are sharper and more accurate. If it's higher, performance is better, but the shadows are lower quality.")]
    public int m_screenspaceDepthDownscale = 1;
    [Tooltip("Whether or not to use temporal denoising to denoise the screenspace atmosphere layers.")]
    public bool m_fogUseTemporalDenoising = true;
    [Range(1, 64), Tooltip("How many history frames to use for denoising the fog.")]
    public int m_fogDenoisingHistoryFrames = 4;
    [Tooltip("Whether or not to use importance sampling to sample physically-lit screenspace layers. Only has an effect if a physically-lit screenspace atmosphere layer is enabled.")]
    public bool m_screenspaceImportanceSample = true;

    void OnEnable() {
        QualityRenderSettings.register(this);
    }

    void OnDisable() {
        QualityRenderSettings.deregister(this);
    }

}


#if UNITY_EDITOR
[CustomEditor(typeof(QualitySettingsBlock))]
public class QualitySettingsBlockEditor : Editor
{

bool m_generalFoldout = false;
bool m_cloudFoldout = false;
bool m_atmosphereFoldout = false;
bool m_fogFoldout = false;

override public void OnInspectorGUI()
{
    serializedObject.Update();
    

    m_generalFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_generalFoldout, "General");
    if (m_generalFoldout) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_antiAlias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_dither"));
    }
    EditorGUILayout.EndFoldoutHeaderGroup();

    m_cloudFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_cloudFoldout, "Clouds");
    if (m_cloudFoldout) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cloudSubresolution"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_compositeCloudsByHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cloudShadowMapFilmPlaneScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cloudShadowMapQuality"));
    }
    EditorGUILayout.EndFoldoutHeaderGroup();

    m_atmosphereFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_atmosphereFoldout, "Atmosphere");
    if (m_atmosphereFoldout) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_atmosphereTextureQuality"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_aerialPerspectiveSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_singleScatteringSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringAccumulationSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_importanceSampleAtmosphere"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_importanceSampleAerialPerspective"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_aerialPerspectiveDepthSkew"));
    }
    EditorGUILayout.EndFoldoutHeaderGroup();

    m_fogFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_fogFoldout, "Fog");
    if (m_fogFoldout) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceFogQuality"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceScatteringSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceImportanceSample"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceOcclusionSamples"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceFogDepthSkew"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_screenspaceDepthDownscale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fogUseTemporalDenoising"));
        if (serializedObject.FindProperty("m_fogUseTemporalDenoising").boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fogDenoisingHistoryFrames"));
        }
    }
    EditorGUILayout.EndFoldoutHeaderGroup();
    
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse