using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class ProceduralNebulaeBlock : MonoBehaviour
{
    /* User-exposed controls. */

    [Tooltip("Quality of procedural nebulae texture.")]
    public Datatypes.Quality m_quality = Datatypes.Quality.Medium;
    [Tooltip("When checked, shows seed values for procedural nebulae parameters. Tweaking random seeds can help you get the right flavor of randomness you want.")]
    public bool m_showSeeds = false;
    [Min(0), Tooltip("The overall intensity of the nebulae.")]
    public float m_overallIntensity = 50;
    [Min(0), Tooltip("Global definition control for the whole nebula texture. This increases saturation and contrast. It's useful to use in tandem with the global intensity control.")]
    public float m_overallDefinition = 1;
    [Tooltip("Global tint control for the whole nebula texture.")]
    public Color m_overallTint = Color.white;
    [Tooltip("The rotation of the nebulae texture.")]
    public Vector3 m_rotation = new Vector3(0, 0, 0);
    [Min(0), Tooltip("Scale of noise used for determining nebula coverage. If this value is high, there will be lots of little nebulae scattered across the sky. If this value is low, there will be a few huge nebulae.")]
    public float m_coverageScale = 1;
    [Tooltip("The seed for the nebula coverage texture.")]
    public Vector3 m_coverageSeed = new Vector3(1.5325337f, 0.553355f, 9.53266f);
    [Min(0), Tooltip("Scale of noise used to randomize nebula transmittance.")]
    public float m_transmittanceScale = 5;
    [MinMaxSlider(0, 1), Tooltip("Range of transmittance values the nebula can have.")]
    public Vector2 m_transmittanceRange = new Vector2(0, 1);
    [Tooltip("The x seed for the nebula transmittance texture.")]
    public Vector3 m_transmittanceSeedX = new Vector3(0.33525f, 1.95382f, 3.1123334f);
    [Tooltip("The y seed for the nebula transmittance texture.")]
    public Vector3 m_transmittanceSeedY = new Vector3(9.53289f, 6.532432f, 3.777532f);
    [Tooltip("The z seed for the nebula transmittance texture.")]
    public Vector3 m_transmittanceSeedZ = new Vector3(7.4432f, 9.48433f, 2.0004325f);

    /* Settings for each noise authoring layer. */
    [System.Serializable]
    public sealed class LayerSettings {
        [Min(0), Tooltip("Intensity of layer.")]
        public float intensity = 0;
        [Tooltip("Color of layer.")]
        public Color color = Color.red;
        [Tooltip("Type of noise to use for this layer.")]
        public Datatypes.NoiseType noise = Datatypes.NoiseType.Perlin;
        [Min(1), Tooltip("Scale of base octave of noise. Smaller values give bigger more global features, larger values give smaller more detailed features.")]
        public float scale = 5;
        [Range(1, 8), Tooltip("Number of noise octaves. Increasing the number of octaves can dim the overall noise texture, so it is useful to adjust the intensity control in tandem with this parameter.")]
        public int octaves = 5;
        [Min(0), Tooltip("Scale multiplier applied to additional octaves of noise. As an example, if this value is 2, each octave will be twice as small as the last octave.")]
        public float octaveScale = 2;
        [Min(0), Tooltip("Intensity multiplier applied to additional octaves of noise. As an example, if this value is 0.5, each octave will be half as intense as the last octave.")]
        public float octaveMultiplier = 0.5f;
        [Range(0, 1), Tooltip("How much the coverage map effects this layer. A higher value will result in more nebula coverage. A lower value will result in less nebula coverage.")]
        public float coverage = 0.5f;
        [Min(0), Tooltip("This parameter allows the layer to bleed across the coverage boundary, and is useful for softening edges.")]
        public float spread = 1;
        [Range(-1, 1), Tooltip("Bias of zero value.")]
        public float bias = 0;
        [Min(0), Tooltip("This increases saturation and contrast, generally making the layer punchier. Increasing the definition usually requires also increasing the strength parameter to ensure that the strands can still get through the coverage map.")]
        public float definition = 1;
        [Min(0), Tooltip("This parameter is meant to be used in tandem with the coverage value. Higher strength values will allow more features to push through the coverage map. The best way to see what this parameter does is to play around with it.")]
        public float strength = 1;
        [Min(1), Tooltip("Scale of the noise used to warp this layer. A higher value gives smaller vortices and tendrils. A lower value gives bigger swirls and arcs.")]
        public float warpScale = 16;
        [Range(0, 1), Tooltip("Intensity of warping effect. Nebulae are big bodies of interstellar gas, and so they obey the laws of fluid mechanics. It's important to capture some of the resulting swirly fluid features. Warping the base noise texture can help with that.")]
        public float warpIntensity = 0.003f;
        [Tooltip("The x seed for the base texture.")]
        public Vector3 baseSeedX = new Vector3(0.19235f, 1.2359f, 3.993583f);
        [Tooltip("The y seed for the base texture.")]
        public Vector3 baseSeedY = new Vector3(0.78675f, 1.34232f, 7.85544f);
        [Tooltip("The z seed for the base texture.")]
        public Vector3 baseSeedZ = new Vector3(8.8532f, 6.643433f, 0.995325f);
        [Tooltip("The x seed for the nebula big strand warp texture.")]
        public Vector3 warpSeedX = new Vector3(1.5325f, 4.4324f, 8.3344321f);
        [Tooltip("The y seed for the nebula big strand warp texture.")]
        public Vector3 warpSeedY = new Vector3(9.5325f, 0.88756f, 5.534419f);
        [Tooltip("The z seed for the nebula big strand warp texture.")]
        public Vector3 warpSeedZ = new Vector3(8.76523f, 4.44743f, 0.42443f);
        public bool showSeeds = false;
    }

    public LayerSettings[] m_layers = new LayerSettings[NebulaDatatypes.kMaxNebulaLayers];

#if UNITY_EDITOR
    /* Layer dropdown select---only included in UI. */
    [Tooltip("Which nebula layer to display for editing.")]
    public NebulaDatatypes.NebulaLayer m_layerSelect = NebulaDatatypes.NebulaLayer.Layer0;
#endif // UNITY_EDITOR

    // Start is called before the first frame update
    void Start()
    {
        OnEnable();
    }

    void OnEnable() 
    {
        NebulaRenderSettings.register(this);
    }

    void OnDisable() 
    {
        NebulaRenderSettings.deregister(this);
    }

    // Update is called once per frame
    void Update()
    {
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(ProceduralNebulaeBlock))]
public class ProceduralNebulaeBlockEditor : Editor
{
    
    bool m_layerEditorFoldout = false;

    // Internal class to help draw layers.
    [CustomPropertyDrawer(typeof(ProceduralNebulaeBlock.LayerSettings))]
    private class ProceduralNebulaeBlockLayerDrawer : PropertyDrawer
    {

        override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("intensity"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("color"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("noise"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("scale"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("octaves"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("octaveScale"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("octaveMultiplier"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("coverage"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("spread"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("bias"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("definition"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("strength"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("warpScale"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("warpIntensity"));
            SerializedProperty seeds = property.FindPropertyRelative("showSeeds");
            if (seeds.boolValue) {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("baseSeedX"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("baseSeedY"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("baseSeedZ"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("warpSeedX"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("warpSeedY"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("warpSeedZ"));
            }
        }
    }

    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty seeds = serializedObject.FindProperty("m_showSeeds");
        
        EditorGUILayout.PropertyField(seeds);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_quality"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_overallIntensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_overallDefinition"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_overallTint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
        if (seeds.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageSeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceSeedX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceSeedY"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceSeedZ"));
        } else {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceRange"));
        }
        m_layerEditorFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_layerEditorFoldout, "Layer Editor");
        if (m_layerEditorFoldout) {
            SerializedProperty layerSelect = serializedObject.FindProperty("m_layerSelect");
            int layerIndex = layerSelect.enumValueIndex;
            EditorGUILayout.PropertyField(layerSelect);
            SerializedProperty layers = serializedObject.FindProperty("m_layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(layerIndex);
            EditorGUILayout.Space(-EditorGUI.GetPropertyHeight(layerSelect));
            EditorGUILayout.PropertyField(layer);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif // UNITY_EDITOR

} // namespace Expanse