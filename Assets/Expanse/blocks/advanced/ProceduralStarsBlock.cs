using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class ProceduralStarsBlock : MonoBehaviour
{
    /* User-exposed controls. */

    /* Common. */
    [Tooltip("The intensity of the stars.")]
    public float m_intensity = 50;
    [Tooltip("Tint to the stars.")]
    public Color m_tint = Color.white;
    [Tooltip("The rotation of the stars.")]
    public Vector3 m_rotation = new Vector3(0, 0, 0);

    /* Modeling. */
    [Tooltip("Quality of star texture.")]
    public Datatypes.Quality m_quality = Datatypes.Quality.Medium;
    [Tooltip("When checked, shows seed values for procedural star parameters. Tweaking random seeds can help you get the right flavor of randomness you want.")]
    public bool m_showSeeds = false;
    [Tooltip("Activates high density mode, which layers a second detail star texture on top of the primary one. Dense star fields are important for imparting a sense of realism in scenes with minimal light pollution, but can be too much for more stylized skies.")]
    public bool m_highDensityMode = false;
    [Range(0, 1), Tooltip("Density of stars.")]
    public float m_density = 0.25f;
    [Tooltip("Seed for star density variation.")]
    public Vector3 m_densitySeed = new Vector3(3.473f, 5.253f, 0.532f);
    [MinMaxSlider(0.0001f, 1), Tooltip("Range of random star sizes.")]
    public Vector2 m_sizeRange = new Vector2(0.4f, 0.6f);
    [Range(0, 1), Tooltip("Biases star sizes toward one end of the range. 0 is biased toward the minimum size. 1 is biased toward the maximum size.")]
    public float m_sizeBias = 0.5f;
    [Tooltip("Seed for star size variation.")]
    public Vector3 m_sizeSeed = new Vector3(6.3234f, 1.253f, 0.3209f);
    [MinMaxSlider(0.0001f, 100), Tooltip("Range of random star brightnesses.")]
    public Vector2 m_intensityRange = new Vector2(1, 6);
    [Range(0, 1), Tooltip("Biases star intensity toward one end of the range. 0 is biased toward the minimum intensity. 1 is biased toward the maximum intensity.")]
    public float m_intensityBias = 0.5f;
    [Tooltip("Seed for star brightness variation.")]
    public Vector3 m_intensitySeed = new Vector3(9.9532f, 7.7345f, 2.0532f);
    [MinMaxSlider(1500, 30000), Tooltip("Range of random star temperatures, in Kelvin. The accuracy of the blackbody model diminishes for temperatures above 20000K, use at your own discretion.")]
    public Vector2 m_temperatureRange = new Vector2(2000, 20000);
    [Range(0, 1), Tooltip("Biases star temperature toward one end of the range. 0 is biased toward the minimum temperature. 1 is biased toward the maximum temperature.")]
    public float m_temperatureBias = 0.5f;
    [Tooltip("Seed for star temperature variation.")]
    public Vector3 m_temperatureSeed = new Vector3(0.2352f, 1.582f, 8.823f);
    // [Min(0), Tooltip("Amount that the star density follows the nebula texture, if there is one.")]
    // public float m_nebulaFollowAmount = 0;
    // [Min(0), Tooltip("How strictly to have the star density follow the nebula density. At higher values, the star density change is very rapid across the nebula boundary. At lower values, the star density change is gradual from the center of the nebula to empty space.")]
    // public float m_nebulaFollowSpread = 2;
    
    /* Twinkle effect. */
    [Tooltip("Whether or not to use star twinkle effect.")]
    public bool m_twinkle = false;
    [Min(0), Tooltip("Brightness threshold for twinkle effect to be applied.")]
    public float m_twinkleThreshold = 0.001f;
    [MinMaxSlider(0, 50), Tooltip("Range of randomly generated twinkle frequencies. Higher values will make the stars twinkle faster. Lower values will make them twinkle slower. A value of zero will result in no twinkling at all.")]
    public Vector2 m_twinkleFrequencyRange = new Vector2(0.5f, 3);
    [Tooltip("Bias to twinkle effect. Negative values increase the time when the star is not visible.")]
    public float m_twinkleBias = 0;
    [Min(0), Tooltip("Intensity of smoother twinkle effect.")]
    public float m_twinkleSmoothAmplitude = 1;
    [Min(0), Tooltip("Intensity of more chaotic twinkle effect.")]
    public float m_twinkleChaoticAmplitude = 1;

    // Start is called before the first frame update
    void Start()
    {
        OnEnable();
    }

    void OnEnable() 
    {
        StarRenderSettings.register(this);
    }

    void OnDisable() 
    {
        StarRenderSettings.deregister(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralStarsBlock))]
public class ProceduralStarsBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensity"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tint"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_quality"));
    SerializedProperty seeds = serializedObject.FindProperty("m_showSeeds");
    EditorGUILayout.PropertyField(seeds);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_highDensityMode"));
    if (seeds.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_density"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_densitySeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeSeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensityRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensityBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensitySeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_temperatureRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_temperatureBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_temperatureSeed"));
    } else {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_density"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensityRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensityBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_temperatureRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_temperatureBias"));
    }

    // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_nebulaFollowAmount"));
    // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_nebulaFollowSpread"));

    SerializedProperty twinkle = serializedObject.FindProperty("m_twinkle");
    EditorGUILayout.PropertyField(twinkle);
    if (twinkle.boolValue) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_twinkleFrequencyRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_twinkleBias"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_twinkleSmoothAmplitude"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_twinkleChaoticAmplitude"));
    }
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse