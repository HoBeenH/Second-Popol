using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CelestialBodyBlock : MonoBehaviour 
{
    /* Name. Used to refer to the layer in UI and printouts. */
    public string m_name = "defaultBodyName";

    /* User-exposed controls. */
    [Tooltip("Directional light this celestial body controls.")]
    public GameObject m_directionalLight;
    // Public serializable variable that tracks whether or not this
    // celestial body uses the primary hdrp light.
    public bool m_lightHasShadowmap = false;

    /* Direction/size. */
    [Tooltip("Celestial body's direction.")]
    public Vector3 m_direction = new Vector3(90, 0, 0);
    [Range(0, 90), Tooltip("Celestial body's angular radius in the sky, specified in degrees.")]
    public float m_angularRadius = 0.5f;
    [Min(0), Tooltip("Multiplier on the angular radius used to compute the celestial body's penumbra. Good for smoothing out the sky shadow for smaller bodies. 1 is a \"physical\" value, though the way Expanse compute sun disc illumination is an approximation anyway.")]
    public float m_penumbra = 1;
    [Min(0), Tooltip("Celestial body's distance from the planet, in meters.")]
    public float m_distance = 1.5e8f;

    /* Lighting. */
    [Tooltip("Specify color via celestial body temperature.")]
    public bool m_useTemperature = false;
    [Tooltip("Light intensity of the celestial body, in lux. In particular this is the illuminance on the ground when the body is at the zenith position. A typical value for the sun is 150000, but this does not always integrate well with existing material workflows that are not physically-based.")]
    public float m_lightIntensity = 150000;
    /* Displays as "filter" in temperature mode. */
    [Tooltip("Celestial body's light color, or if using in temperature mode, filter applied to chosen temperature (in this case an artistic override).")]
    public Color m_lightColor = Color.white;
    [Range(1000, 20000), Tooltip("Celestial body's temperature, used to set color in a physically-based way, according to the blackbody spectrum.")]
    public float m_lightTemperature = 5778;
    [Tooltip("Whether or not this celestial body will illuminate clouds.")]
    public bool m_lightClouds = true;
    [Tooltip("Whether or not this celestial body will cast cloud shadows.")]
    public bool m_castCloudShadows = false;
    [Tooltip("Whether or not this celestial body casts volumetric geometry shadows.")]
    public bool m_volumetricGeometryShadows = false;
    [Tooltip("Whether or not this celestial body casts volumetric cloud shadows.")]
    public bool m_volumetricCloudShadows = false;
    [Tooltip("If this celestial body's light is the main directional light, check this parameter to use the shadowmap for volumetric shadows instead of the depth buffer.")]
    public bool m_shadowmapVolumetricShadows = false;
    [Min(0), Tooltip("Maximum distance from camera over which volumetric shadows are applied. If you set this too high, you can lose detail in your close-up shadows.")]
    public float m_maxVolumetricShadowmapDistance = 5000;
    [Min(0), Tooltip("Artistic override to affect how much light gets transmitted through the atmosphere. For instance, if you feel like there's not enough light peeking through your fog at sunset, you can adjust this value above 1.")]
    public float m_transmittanceMultiplier = 1;

    /* Albedo. */
    [Tooltip("Whether or not the celestial body is tidally locked. This means that the same side of the celestial body always faces the planet---for instance, like Earth's moon, Luna.")]
    public bool m_tidallyLocked = false;
    [Tooltip("Whether or not this celestial body receives light.")]
    public bool m_receivesLight = false;
    [Tooltip("Celestial body's albedo texture.")]
    public Cubemap m_albedoTexture = null;
    [Tooltip("Rotation of celestial body's albedo texture, specified by euler angles.")]
    public Vector3 m_albedoTextureRotation = new Vector3(0, 0, 0);
    [Tooltip("Tint to celestial body's albedo texture, or just celestial body's color if no texture is selected. Perfect grey (127, 127, 127) specifies no tint.")]
    public Color m_albedoTint = Color.grey;
    [Tooltip("Instead of using a Lambertian (diffuse) BRDF, uses a BRDF specifically tailored for modeling Earth's moon, Luna.")]
    public bool m_moonMode = false;
    [Range(0.001f, 1), Tooltip("Retrodirection of Luna BRDF. A good value for the moon is 0.6.")]
    public float m_retrodirection = 0.6f;
    [Range(-1, 1), Tooltip("Anisotropy of Luna BRDF. A good value for the moon is 0.1.")]
    public float m_anisotropy = 0.1f;

    /* Emission. */
    [Tooltip("Whether or not celestial body is emissive.")]
    public bool m_emissive = true;
    [Min(0), Tooltip("Adjustable limb-darkening effect that darkens edges of celestial body. A physically-accurate value is 1, but higher values are often needed for the effect to be visible.")]
    public float m_limbDarkening = 1;
    [Tooltip("Emission texture for celestial body. Will be multiplied by light intensity to get final displayed color.")]
    public Cubemap m_emissionTexture = null;
    [Tooltip("Rotation of celestial body's emission texture, specified by euler angles.")]
    public Vector3 m_emissionTextureRotation = new Vector3(0, 0, 0);
    [Tooltip("Tint to celestial body. If an emission texture is present, will tint the texture, but otherwise will just tint the body's light color. A value of perfect white (255, 255, 255) specifies no tint.")]
    public Color m_emissionTint = Color.white;
    [Min(0), Tooltip("Multiplier on emissive color/texture. Often, emission textures will be too blown out if their actual physical light values are used. This is an artistic override to correct that.")]
    public float m_emissionMultiplier = 1;

    void OnEnable() {
        CelestialBodyRenderSettings.register(this);
        if (m_directionalLight != null) {
            m_directionalLight.SetActive(true);
        }
    }

    void OnDisable() {
        CelestialBodyRenderSettings.deregister(this);
        if (m_directionalLight != null) {
            m_directionalLight.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_lightHasShadowmap = (m_directionalLight != null) && m_directionalLight.GetComponent<Light>().shadows != LightShadows.None;

        if (m_directionalLight != null) {
            LightControl lightControl = m_directionalLight.GetComponent<LightControl>();
            if (lightControl == null) {
                lightControl = m_directionalLight.AddComponent<LightControl>();
            }
            lightControl.SetRotation(m_direction);
            lightControl.m_angularRadius = m_angularRadius;
            lightControl.m_penumbra = m_penumbra;
            lightControl.m_useTemperature = m_useTemperature;
            lightControl.m_lightIntensity = m_lightIntensity;
            lightControl.m_lightColor = m_lightColor;
            lightControl.m_lightTemperature = m_lightTemperature;
            lightControl.m_shadowmapVolumetricShadows = m_lightHasShadowmap ? m_shadowmapVolumetricShadows : false;
            lightControl.m_maxVolumetricShadowmapDistance = m_maxVolumetricShadowmapDistance;
            lightControl.m_transmittanceMultiplier = m_transmittanceMultiplier;
            lightControl.m_lightClouds = m_lightClouds;
            lightControl.m_castCloudShadows = m_lightClouds ? m_castCloudShadows : false;
            lightControl.m_volumetricGeometryShadows = m_volumetricGeometryShadows;
            lightControl.m_volumetricCloudShadows = m_volumetricCloudShadows;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CelestialBodyBlock))]
public class CelestialBodyBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_name"));
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_directionalLight"));

    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_direction"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_distance"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_angularRadius"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_penumbra"));

    /* Light color. */
    SerializedProperty useTemp = serializedObject.FindProperty("m_useTemperature");
    EditorGUILayout.PropertyField(useTemp);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightIntensity"));
    if (useTemp.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightTemperature"));        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightColor"), new GUIContent("Light Filter"));
    } else {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightColor"));
    }
    SerializedProperty cloudLighting = serializedObject.FindProperty("m_lightClouds");
    EditorGUILayout.PropertyField(cloudLighting);
    if (cloudLighting.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_castCloudShadows"));
    }
    
    SerializedProperty volumetricGeoShadows = serializedObject.FindProperty("m_volumetricGeometryShadows");
    EditorGUILayout.PropertyField(volumetricGeoShadows);
    if (volumetricGeoShadows.boolValue) {
        SerializedProperty lightHasShadowmap = serializedObject.FindProperty("m_lightHasShadowmap");
        if (lightHasShadowmap.boolValue) {
            SerializedProperty shadowmapVolumetricShadows = serializedObject.FindProperty("m_shadowmapVolumetricShadows");
            EditorGUILayout.PropertyField(shadowmapVolumetricShadows);
            if (shadowmapVolumetricShadows.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxVolumetricShadowmapDistance"));
            }
        }
    }
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceMultiplier"));
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_volumetricCloudShadows"));

    SerializedProperty albedoTex = serializedObject.FindProperty("m_albedoTexture");
    SerializedProperty emissionTex = serializedObject.FindProperty("m_emissionTexture");
    if (albedoTex.objectReferenceValue != null || albedoTex.objectReferenceValue != null) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tidallyLocked"));
    }

    /* Albedo. */
    SerializedProperty receivesLight = serializedObject.FindProperty("m_receivesLight");
    EditorGUILayout.PropertyField(receivesLight);
    if (receivesLight.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_albedoTint"));
        EditorGUILayout.PropertyField(albedoTex);
        if (albedoTex.objectReferenceValue != null) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_albedoTextureRotation"));
        }
        SerializedProperty moonMode = serializedObject.FindProperty("m_moonMode");
        EditorGUILayout.PropertyField(moonMode);
        if (moonMode.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_retrodirection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_anisotropy"));
        }
    }

    /* Emission. */
    SerializedProperty emissive = serializedObject.FindProperty("m_emissive");
    EditorGUILayout.PropertyField(emissive);
    if (emissive.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_emissionTint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_emissionMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_limbDarkening"));
        EditorGUILayout.PropertyField(emissionTex);
        if (emissionTex.objectReferenceValue != null) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_emissionTextureRotation"));
        }
    }
    
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse