using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode]
public class LightControl : MonoBehaviour
{
  // Common properties.
  [Tooltip("Light intensity in lux. In particular this is the illuminance on the ground when the body is at the zenith position. A typical value for the sun is 150000, but this does not always integrate well with existing material workflows that are not physically-based.")]
  public float m_lightIntensity;
  [Tooltip("Specify color via blackbody temperature.")]
  public bool m_useTemperature = true;
  [Tooltip("Light color, or if using in temperature mode, filter applied to chosen temperature (in this case an artistic override). If this is a directional light, the intensity is specified in Lux. If it's a point light, the intensity is specified in lumens.")]
  public Color m_lightColor = Color.white;
  [Range(1000, 20000), Tooltip("Celestial body's temperature, used to set color in a physically-based way, according to the blackbody spectrum.")]
  public float m_lightTemperature = 5778;
  [Tooltip("Whether or not this light will illuminate clouds.")]
  public bool m_lightClouds = true;
  [Tooltip("Whether or not this light will cast volumetric geometry cloud shadows.")]
  public bool m_volumetricGeometryShadows = true;
  [Tooltip("If this light is the main directional light, check this parameter to use the shadowmap for volumetric shadows instead of the depth buffer.")]
  public bool m_shadowmapVolumetricShadows = false;
  [Min(0), Tooltip("Maximum distance from camera over which volumetric shadows are applied. If you set this too high, you can lose detail in your close-up shadows.")]
  public float m_maxVolumetricShadowmapDistance = 5000;
  [Min(0), Tooltip("Artistic override to affect how much light gets transmitted through the atmosphere. For instance, if you feel like there's not enough light peeking through your fog at sunset, you can adjust this value above 1.")]
  public float m_transmittanceMultiplier = 1;

  // Directional-only properties.
  [Range(0, 90), Tooltip("Light's angular radius in the sky, specified in degrees. Used when calculating penumbra.")]
  public float m_angularRadius = 0.5f;
  [Min(0), Tooltip("Multiplier on the angular radius used to compute the light's penumbra. Good for smoothing out the sky shadow for smaller bodies. 1 is a \"physical\" value, though the way Expanse compute sun disc illumination is an approximation anyway.")]
  public float m_penumbra = 1;
  [Tooltip("Whether or not this light will cast volumetric cloud shadows.")]
  public bool m_volumetricCloudShadows = true;
  [Tooltip("Whether or not this light will cast cloud shadows.")]
  public bool m_castCloudShadows = true;
  
  // Point-only properties.
  [Tooltip("Whether or not to raymarch the point light. If this is disabled, an analytical integration is used. The analytical integration strategy is very good and so this should be unnecessary.")]
  public bool m_raymarch = false;
  [Tooltip("Whether or not this light will illuminate fog.")]
  public bool m_lightFog = true;
  [Min(0), Tooltip("Multiplier on the fog scattering.")]
  public float m_fogMultiplier = 1;


  // Public variables that are set from the light.
  public Vector3 m_direction;
  public float m_range;
  public UnityEngine.LightType m_lightType;
  public bool m_lightHasShadowmap;


  // This indices are set by the global light render settings function---they tell
  // the light control block where to pull transmittance values and shadowmaps from.
  private int m_atmosphereIndex = 0;
  private int m_cloudIndex = 0;
  private int m_cloudShadowIndex = 0;
  public void SetAtmosphereIndex(int index) {
    m_atmosphereIndex = index;
  }
  public void SetCloudIndex(int index) {
    m_cloudIndex = index;
  }
  public void SetCloudShadowIndex(int index) {
    m_cloudShadowIndex = index;
  }

  public UnityEngine.Light GetLight() {
    return gameObject.GetComponent(typeof(UnityEngine.Light)) as UnityEngine.Light;
  }

  public HDAdditionalLightData GetHDAdditionalLightData() {
    return gameObject.GetComponent(typeof(HDAdditionalLightData)) as HDAdditionalLightData;
  }

  public void SetRotation(Vector3 rotation) {
    gameObject.transform.localRotation = Quaternion.Euler(rotation.x,
                                rotation.y,
                                rotation.z);
  }

  void OnEnable() {
      LightingRenderSettings.register(this);
  }

  void OnDisable() {
      LightingRenderSettings.deregister(this);
  }

  void Update() {
    /* Get the light. */
    UnityEngine.Light light = gameObject.GetComponent(typeof(UnityEngine.Light)) as UnityEngine.Light;
    HDAdditionalLightData additionalLightData = gameObject.GetComponent(typeof(HDAdditionalLightData)) as HDAdditionalLightData;

    /* Get the quality settings. */
    QualitySettingsBlock quality = QualityRenderSettings.Get();

    /* Set our cached light variables from the light. */
    m_lightType = light.type;
    m_lightHasShadowmap = light.shadows != LightShadows.None;

    /* Set the direction based on the game object transform.
      * If we're a point light, the direction is actually the position. */
    if (light.type == UnityEngine.LightType.Directional) {
      m_direction = gameObject.transform.localRotation.eulerAngles;
    } else if (light.type == UnityEngine.LightType.Point || light.type == UnityEngine.LightType.Spot) {
      m_direction = gameObject.transform.position;
    }

    /* Set the range from the light object. */
    if (light.type == UnityEngine.LightType.Point || light.type == UnityEngine.LightType.Spot) {
      m_range = light.range;
    }

    /* Spot lights can't light the clouds. */
    if (light.type == UnityEngine.LightType.Spot) {
      m_lightClouds = false;
    }

    /* Set the light color. */
    light.intensity = m_lightIntensity;
    if (m_useTemperature) {
      light.color = m_lightColor * CelestialBodyUtils.blackbodyTempToColor(m_lightTemperature);
    } else {
      light.color = m_lightColor;
    }

    if (light.type == UnityEngine.LightType.Directional) {
      /* Set light's angular radius. */
      additionalLightData.angularDiameter = m_angularRadius * 2;
      /* Modulate it by the light transmittance. */
      Vector3 transmittance = m_transmittanceMultiplier * ((Vector4) AtmosphereRenderer.GetBodyTransmittance(m_atmosphereIndex)).xyz();
      transmittance = Vector3.ClampMagnitude(transmittance, Mathf.Sqrt(3));
      light.color *= new Vector4(transmittance.x, transmittance.y, transmittance.z, 1);
      /* Set the cookie for cloud shadows. */
      RTHandle cloudShadowMap = CloudCompositor.getShadowMap(m_cloudShadowIndex);
      if (cloudShadowMap != null && cloudShadowMap.rt != null && cloudShadowMap.rt.IsCreated() && m_castCloudShadows) {
        float cookieSize = quality.m_cloudShadowMapFilmPlaneScale;
        additionalLightData.SetCookie(cloudShadowMap, new Vector2(cookieSize, cookieSize));
      } else {
        light.cookie = null;
      }
    }



#if UNITY_EDITOR
    light.SetLightDirty();
#endif

  }

}

#if UNITY_EDITOR
[CustomEditor(typeof(LightControl)), CanEditMultipleObjects]
public class LightControlEditor : Editor
{
  override public void OnInspectorGUI()
  {
    serializedObject.Update();

    UnityEngine.LightType lightType = (UnityEngine.LightType) serializedObject.FindProperty("m_lightType").enumValueIndex;

    if (lightType == UnityEngine.LightType.Directional) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightIntensity"), new GUIContent("Intensity (Lux)"));
    } else if (lightType == UnityEngine.LightType.Point || lightType == UnityEngine.LightType.Spot) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightIntensity"), new GUIContent("Intensity (Lumen)"));
    }

    SerializedProperty useTemp = serializedObject.FindProperty("m_useTemperature");
    EditorGUILayout.PropertyField(useTemp);
    if (useTemp.boolValue) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightColor"), new GUIContent("Light Filter"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightTemperature"));
    } else {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightColor"));
    }

    if (lightType == UnityEngine.LightType.Directional) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_angularRadius"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_penumbra"));
    }

    if (lightType == UnityEngine.LightType.Point || lightType == UnityEngine.LightType.Spot) {
      SerializedProperty lightFog = serializedObject.FindProperty("m_lightFog");
      EditorGUILayout.PropertyField(lightFog);
      if (lightFog.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fogMultiplier"));
      }
    }

    if (lightType == UnityEngine.LightType.Point || lightType == UnityEngine.LightType.Directional) {
      SerializedProperty lightClouds = serializedObject.FindProperty("m_lightClouds");
      EditorGUILayout.PropertyField(lightClouds);
      if (lightClouds.boolValue && lightType == UnityEngine.LightType.Directional) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_castCloudShadows"));
      }
    }

    SerializedProperty volumetricGeoShadows = serializedObject.FindProperty("m_volumetricGeometryShadows");
    EditorGUILayout.PropertyField(volumetricGeoShadows);
    if (volumetricGeoShadows.boolValue) {
      SerializedProperty lightHasShadowmap = serializedObject.FindProperty("m_lightHasShadowmap");
      if (lightHasShadowmap.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_shadowmapVolumetricShadows"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxVolumetricShadowmapDistance"));
      }
    }

    if (lightType == UnityEngine.LightType.Directional) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_volumetricCloudShadows"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceMultiplier"));
    }

    if (lightType == UnityEngine.LightType.Point || lightType == UnityEngine.LightType.Spot) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("m_raymarch"));
    }

    serializedObject.ApplyModifiedProperties();
  }
}

#endif // UNITY_EDITOR

}
