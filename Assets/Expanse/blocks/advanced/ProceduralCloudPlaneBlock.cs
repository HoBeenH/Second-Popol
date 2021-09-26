using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class ProceduralCloudPlaneBlock : BaseCloudLayerBlock
{
    /* Name. Used to refer to the layer in UI and printouts. */
    public string m_name = "defaultCloudLayerName";

    /* Internal universal representation. */
    private UniversalCloudLayer m_universal = new UniversalCloudLayer();

    /* User-exposed controls. */

    /* Modeling. */
    [Tooltip("Whether or not the cloud layer is curved with the surface of the planet.")]
    public bool m_curved = false;
    [Tooltip("X extent of this cloud layer's geometry.")]
    public Vector2 m_geometryXExtent = new Vector2(-100000, 100000);
    [Tooltip("Z extent of this cloud layer's geometry.")]
    public Vector2 m_geometryZExtent = new Vector2(-100000, 100000);
    [Tooltip("Height of this cloud layer's geometry.")]
    public float m_geometryHeight = 12000;
    [Min(0), Tooltip("Density over which density is attenuated.")]
    public float m_attenuationDistance = 25000;
    [Min(0), Tooltip("Density before density attenuation kicks in.")]
    public float m_attenuationBias = 25000;
    [MinMaxSlider(0, 200000), Tooltip("Range over which density ramps up to full. Useful as a sort of soft near clipping plane for the clouds.")]
    public Vector2 m_rampUp = new Vector2(0, 0);
    [Tooltip("Quality of procedural noises for this layer. If no procedural noises are enabled, this parameter does not change anything.")]
    public Datatypes.Quality m_noiseTextureQuality = Datatypes.Quality.Medium;
    [Range(0, 1), Tooltip("How much the coverage noise erodes the base noise.")]
    public float m_coverageIntensity = 1;
    [Range(0, 1), Tooltip("How much the structure noise erodes the base noise.")]
    public float m_structureIntensity = 0;
    [Range(0, 1), Tooltip("How much the structure noise multiplies the base noise.")]
    public float m_structureMultiply = 0;
    [Range(0, 1), Tooltip("How much the detail noise erodes the base noise.")]
    public float m_detailIntensity = 0;
    [Range(0, 1), Tooltip("How much the detail noise multiplies the base noise.")]
    public float m_detailMultiply = 0;
    [Range(0, 1), Tooltip("How much the base noise is warped by its warp texture.")]
    public float m_baseWarpIntensity = 0;
    [Range(0, 1), Tooltip("How much the detail noise is warped by its warp texture.")]
    public float m_detailWarpIntensity = 0;

    /* Noise editor. */
    /* Settings for each noise authoring layer. */
    [System.Serializable]
    public sealed class LayerSettings {
        [Tooltip("Whether to use procedural noise or a texture this noise layer.")]
        public bool procedural = true;
        [Tooltip("Noise texture for this layer.")]
        public Texture texture = null;
        [Tooltip("Noise type for this layer.")]
        public Datatypes.NoiseType noiseType = Datatypes.NoiseType.Perlin;
        [Tooltip("Scale of 0th octave.")]
        public Vector2 scale = new Vector2(16, 16);
        [Range(1, 8), Tooltip("Number of octaves.")]
        public int octaves = 4;
        [Min(0), Tooltip("How much to scale each successive octave by.")]
        public float octaveScale = 2;
        [Min(0), Tooltip("How much to multiply the intensity of each successive octave by.")]
        public float octaveMultiplier = 0.5f;
        [Min(1), Tooltip("Tile factor.")]
        public int tile = 1;
    }
    string[] kNoiseLayerNames = {"coverage", "base", "structure", "detail", "baseWarp", "detailWarp"};
    public LayerSettings[] m_noiseLayers = new LayerSettings[CloudDatatypes.kNumCloudNoiseLayers];

    /* Lighting. */
    [Min(0), Tooltip("Density of this cloud layer.")]
    public float m_density = 250;
    [Min(1), Tooltip("Apparent thickness of this 2D cloud layer. Pushing this value too high can give strange results.")]
    public float m_apparentThickness = 250;
    [ColorUsageAttribute(true,true), Tooltip("Extinction coefficients control the absorption of light by the atmosphere.")]
    public Color m_extinctionCoefficients = new Color(4e-6f, 4e-6f, 4e-6f, 1);
    [ColorUsageAttribute(true,true), Tooltip("Scattering coefficients control the scattering of light by the atmosphere. Should be less than extinction to remain physical.")]
    public Color m_scatteringCoefficients = new Color(4e-6f, 4e-6f, 4e-6f, 1);
    [Range(-1, 1), Tooltip("Anistropy of cloud scattering. Higher values will give more forward scattering. Lower values will give more backward scattering. A value of zero is neutral---i.e. it will produce \"isotropic\" scattering. Clouds are generally quite anisotropic, so a value of around 0.6 is a good physical approximation.")]
    public float m_anisotropy = 0.6f;
    [Range(0, 1), Tooltip("Intensity of cloud silver lining.")]
    public float m_silverIntensity = 0.0f;
    [Range(0, 1), Tooltip("Spread of cloud silver lining.")]
    public float m_silverSpread = 0.5f;
    [Min(0), Tooltip("Ambient lighting the clouds receive from the sky. Expanse doesn't compute self-shadowing of ambient light, so this can help to lower the ambient light contribution to a level that looks more physically correct.")]
    public float m_ambient =  1;
    [Tooltip("Whether or not the clouds cast shadows on themselves.")]
    public bool m_selfShadowing = false;
    [Range(0, 1), Tooltip("Amount of approximated multiple scattering.")]
    public float m_multipleScatteringAmount = 0.7f;
    [Range(0, 1), Tooltip("Bias to approximated multiple scattering.")]
    public float m_multipleScatteringBias = 0.25f;
    [Range(0, 1), Tooltip("How much to ramp down multiple scattering as samples approach the light. This is useful for making sure that denser clouds block enough light when the sun is behind them.")]
    public float m_multipleScatteringRampDown = 0;
    [Min(0), Tooltip("How sharply the multiple scattering ramps down as samples approach the light.")]
    public float m_multipleScatteringRampDownShape = 7;
    [Range(0, 1), Tooltip("Amount the light pollution affects the clouds. Useful for when light pollution is being used primarily as an artistic effect for the sky.")]
    public float m_lightPollutionDimmer = 1.0f;
    [Tooltip("Whether to use cel/\"toon\" shading on the clouds.")]
    public bool m_celShade = false;
    [Min(0), Tooltip("Band above which transmittance is 1, and below which transmittance is zero.")]
    public float m_celShadeColorBands = 1500;
    [Range(0, 1), Tooltip("Band above which lighting is clamped, and below which lighting is zero.")]
    public float m_celShadeTransmittanceBands = 0.5f;
    [Tooltip("Whether or not this layer casts shadows on the ground and geometry.")]
    public bool m_castShadows = false;
    [Range(0, 1), Tooltip("The maximum darkness of shadows this cloud layer casts onto geometry and fog.")]
    public float m_maxShadowIntensity = 0.95f;

    /* Movement. */
    [Tooltip("Displays sampling offset instead of velocity, for user control.")]
    public bool m_useOffset = false;
    [Tooltip("Velocity of the clouds. Automates the sampling offset parameter.")]
    public Vector2 m_velocity = new Vector2(0.0001f, -0.0001f);
    [Tooltip("Sampling offset of the clouds. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_samplingOffset = new Vector2(0, 0);

    /* Sampling And Quality. */
    [Range(1, 4), Tooltip("Number of history frames to use for reprojection. Increasing can improve performance, but at the cost of quality")]
    public int m_reprojectionFrames = 2;

#if UNITY_EDITOR
    /* Layer dropdown select---only included in UI. */
    [Tooltip("Which noise layer to display for editing.")]
    public CloudDatatypes.CloudNoiseLayer m_layerSelect = CloudDatatypes.CloudNoiseLayer.Base;
#endif // UNITY_EDITOR

    // Update is called once per frame
    void Update()
    {
        /* Modeling. */
        m_universal.renderSettings.geometryType = (int) (m_curved ? CloudDatatypes.CloudGeometryType.CurvedPlane : CloudDatatypes.CloudGeometryType.Plane);
        m_universal.renderSettings.geometryXExtent = m_geometryXExtent;
        m_universal.renderSettings.geometryZExtent = m_geometryZExtent;
        m_universal.renderSettings.geometryHeight = m_geometryHeight;
        m_universal.renderSettings.attenuationDistance = m_attenuationDistance;
        m_universal.renderSettings.attenuationBias = m_attenuationBias;
        m_universal.renderSettings.rampUp = m_rampUp;
        m_universal.noiseTextureQuality = m_noiseTextureQuality;
        m_universal.renderSettings.coverageIntensity = m_coverageIntensity;
        m_universal.renderSettings.structureIntensity = m_structureIntensity;
        m_universal.renderSettings.structureMultiply = m_structureMultiply;
        m_universal.renderSettings.detailIntensity = m_detailIntensity;
        m_universal.renderSettings.detailMultiply = m_detailMultiply;
        m_universal.renderSettings.baseWarpIntensity = m_baseWarpIntensity;
        m_universal.renderSettings.detailWarpIntensity = m_detailWarpIntensity;

        for (int i = 0; i < m_noiseLayers.Length; i++) {
            m_universal.noiseLayers[i].procedural = m_noiseLayers[i].procedural;
            m_universal.noiseLayers[i].noiseTexture = m_noiseLayers[i].texture;
            m_universal.noiseLayers[i].renderSettings.noiseType = m_noiseLayers[i].noiseType;
            m_universal.noiseLayers[i].renderSettings.scale = m_noiseLayers[i].scale;
            m_universal.noiseLayers[i].renderSettings.octaves = m_noiseLayers[i].octaves;
            m_universal.noiseLayers[i].renderSettings.octaveScale = m_noiseLayers[i].octaveScale;
            m_universal.noiseLayers[i].renderSettings.octaveMultiplier = m_noiseLayers[i].octaveMultiplier;
            m_universal.noiseLayers[i].renderSettings.tile = m_noiseLayers[i].tile;
        }

        m_universal.renderSettings.coverageTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.Coverage].tile;
        m_universal.renderSettings.baseTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.Base].tile;
        m_universal.renderSettings.structureTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.Structure].tile;
        m_universal.renderSettings.detailTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.Detail].tile;
        m_universal.renderSettings.baseWarpTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.BaseWarp].tile;
        m_universal.renderSettings.detailWarpTile = m_noiseLayers[(int) CloudDatatypes.CloudNoiseLayer.DetailWarp].tile;


        /* Lighting. */
        m_universal.renderSettings.density = m_density;
        m_universal.renderSettings.apparentThickness = m_apparentThickness;
        m_universal.renderSettings.extinctionCoefficients = ((Vector4) m_extinctionCoefficients).xyz();
        m_universal.renderSettings.scatteringCoefficients = ((Vector4) m_scatteringCoefficients).xyz();
        m_universal.renderSettings.anisotropy = m_anisotropy;
        m_universal.renderSettings.silverIntensity = m_silverIntensity;
        m_universal.renderSettings.silverSpread = m_silverSpread;
        m_universal.renderSettings.ambient = m_ambient;
        m_universal.renderSettings.selfShadowing = m_selfShadowing ? 1 : 0;
        m_universal.renderSettings.multipleScatteringAmount = m_multipleScatteringAmount;
        m_universal.renderSettings.multipleScatteringBias = m_multipleScatteringBias;
        m_universal.renderSettings.multipleScatteringRampDown = m_multipleScatteringRampDown;
        m_universal.renderSettings.multipleScatteringRampDownShape = m_multipleScatteringRampDownShape;
        m_universal.renderSettings.castShadows = m_castShadows ? 1 : 0;
        m_universal.renderSettings.maxShadowIntensity = Mathf.Sqrt(m_maxShadowIntensity);
        m_universal.renderSettings.lightPollutionDimmer = m_lightPollutionDimmer;
        m_universal.renderSettings.celShade = m_celShade ? 1 : 0;
        m_universal.renderSettings.celShadeLightingBands = m_celShadeColorBands;
        m_universal.renderSettings.celShadeTransmittanceBands = m_celShadeTransmittanceBands;

        /* Movement. */
        if (!m_useOffset) {
            m_samplingOffset += Time.deltaTime * m_velocity;
        }
        m_universal.renderSettings.baseOffset = new Vector3(m_samplingOffset.x, 0, m_samplingOffset.y);

        /* Quality. */
        m_universal.renderSettings.reprojectionFrames = m_reprojectionFrames;
    }

    public override UniversalCloudLayer ToUniversal() {
        return m_universal;
    }

    public override void FromUniversal(UniversalCloudLayer from, bool bypassOffset=false) {
        /* Modeling. */
        m_curved = from.renderSettings.geometryType == (int) CloudDatatypes.CloudGeometryType.CurvedPlane;
        m_geometryXExtent = from.renderSettings.geometryXExtent;
        m_geometryZExtent = from.renderSettings.geometryZExtent;
        m_geometryHeight = from.renderSettings.geometryHeight;
        m_attenuationDistance = from.renderSettings.attenuationDistance;
        m_attenuationBias = from.renderSettings.attenuationBias;
        m_rampUp = from.renderSettings.rampUp;
        m_coverageIntensity = from.renderSettings.coverageIntensity;
        m_structureIntensity = from.renderSettings.structureIntensity;
        m_structureMultiply = from.renderSettings.structureMultiply;
        m_detailIntensity = from.renderSettings.detailIntensity;
        m_detailMultiply = from.renderSettings.detailMultiply;
        m_baseWarpIntensity = from.renderSettings.baseWarpIntensity;
        m_detailWarpIntensity = from.renderSettings.detailWarpIntensity;

        m_noiseTextureQuality = from.noiseTextureQuality;
        for (int i = 0; i < m_noiseLayers.Length; i++) {
            m_noiseLayers[i].procedural = from.noiseLayers[i].procedural;
            m_noiseLayers[i].texture = from.noiseLayers[i].noiseTexture;
            m_noiseLayers[i].noiseType = from.noiseLayers[i].renderSettings.noiseType;
            m_noiseLayers[i].scale = from.noiseLayers[i].renderSettings.scale;
            m_noiseLayers[i].octaves = from.noiseLayers[i].renderSettings.octaves;
            m_noiseLayers[i].octaveScale = from.noiseLayers[i].renderSettings.octaveScale;
            m_noiseLayers[i].octaveMultiplier = from.noiseLayers[i].renderSettings.octaveMultiplier;
            m_noiseLayers[i].tile = from.noiseLayers[i].renderSettings.tile;
        }

        /* Lighting. */
        m_density = from.renderSettings.density;
        m_apparentThickness = from.renderSettings.apparentThickness;
        m_extinctionCoefficients = new Color(from.renderSettings.extinctionCoefficients.x, from.renderSettings.extinctionCoefficients.y, from.renderSettings.extinctionCoefficients.z);
        m_scatteringCoefficients = new Color(from.renderSettings.scatteringCoefficients.x, from.renderSettings.scatteringCoefficients.y, from.renderSettings.scatteringCoefficients.z);
        m_anisotropy = from.renderSettings.anisotropy;
        m_silverIntensity = from.renderSettings.silverIntensity;
        m_silverSpread = from.renderSettings.silverSpread;
        m_ambient = from.renderSettings.ambient;
        m_selfShadowing = from.renderSettings.selfShadowing == 1;
        m_multipleScatteringAmount = from.renderSettings.multipleScatteringAmount;
        m_multipleScatteringBias = from.renderSettings.multipleScatteringBias;
        m_multipleScatteringRampDown = from.renderSettings.multipleScatteringRampDown;
        m_multipleScatteringRampDownShape = from.renderSettings.multipleScatteringRampDownShape;
        m_castShadows = from.renderSettings.castShadows == 1;
        m_maxShadowIntensity = from.renderSettings.maxShadowIntensity * from.renderSettings.maxShadowIntensity;
        m_lightPollutionDimmer = from.renderSettings.lightPollutionDimmer;
        m_celShade = from.renderSettings.celShade == 1;
        m_celShadeColorBands = from.renderSettings.celShadeLightingBands;
        m_celShadeTransmittanceBands = from.renderSettings.celShadeTransmittanceBands;

        /* Movement. */
        if (!bypassOffset) {
            m_samplingOffset = from.renderSettings.baseOffset;
        }

        /* Quality. */
        m_reprojectionFrames = Math.Max(1, Math.Min(4, from.renderSettings.reprojectionFrames));
    }

    public override bool SetTexture(CloudDatatypes.CloudNoiseLayer noiseLayer, Texture texture, int tile) {
        if (texture == null) {
            return false;
        }
        if (texture.dimension != UnityEngine.Rendering.TextureDimension.Tex2D) {
            return false;
        }
        m_noiseLayers[(int) noiseLayer].procedural = false;
        m_noiseLayers[(int) noiseLayer].texture = texture;
        m_noiseLayers[(int) noiseLayer].tile = tile;
        return true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralCloudPlaneBlock))]
public class ProceduralCloudPlaneBlockEditor : Editor
{
    bool m_modelingFoldout = false;
    bool m_noiseEditorFoldout = false;
    bool m_lightingFoldout = false;
    bool m_movementFoldout = false;
    bool m_qualityFoldout = false;

    // Internal class to help draw layers.
    [CustomPropertyDrawer(typeof(ProceduralCloudPlaneBlock.LayerSettings))]
    private class ProceduralCloudPlaneBlockLayerDrawer : PropertyDrawer
    {

        override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty procedural = property.FindPropertyRelative("procedural");
            EditorGUILayout.PropertyField(procedural);
            if (procedural.boolValue) {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("noiseType"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("scale"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("octaves"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("octaveScale"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("octaveMultiplier"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("tile"));
            } else {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("texture"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("tile"));
            }
        }
    }

    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_name"));
        
        if (GUILayout.Button("Load Preset"))
        {
            string pathToLoad = EditorUtility.OpenFilePanel("", Application.dataPath, "json");
            if (pathToLoad.Length != 0) {
                BaseCloudLayerBlock cloudLayer = (BaseCloudLayerBlock) target;
                cloudLayer.LoadUniversal(pathToLoad);
            }
        }

        if (GUILayout.Button("Save Preset"))
        {
            string pathToSaveTo = EditorUtility.SaveFilePanelInProject("Save Preset", "", "json", "");
            if (pathToSaveTo.Length != 0) {
                BaseCloudLayerBlock cloudLayer = (BaseCloudLayerBlock) target;
                cloudLayer.SaveUniversal(pathToSaveTo);
            }
        }

        m_modelingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_modelingFoldout, "Modeling");
        if (m_modelingFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_curved"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_geometryXExtent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_geometryZExtent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_geometryHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_attenuationDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_attenuationBias"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rampUp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureMultiply"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailMultiply"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpIntensity"));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        m_noiseEditorFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_noiseEditorFoldout, "Noise Editor");
        if (m_noiseEditorFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_noiseTextureQuality"));
            SerializedProperty layerSelect = serializedObject.FindProperty("m_layerSelect");
            int layerIndex = layerSelect.enumValueIndex;
            EditorGUILayout.PropertyField(layerSelect);
            SerializedProperty layers = serializedObject.FindProperty("m_noiseLayers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(layerIndex);
            EditorGUILayout.Space(-EditorGUI.GetPropertyHeight(layerSelect));
            EditorGUILayout.PropertyField(layer);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        m_lightingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_lightingFoldout, "Lighting");
        if (m_lightingFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_density"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_apparentThickness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_extinctionCoefficients"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scatteringCoefficients"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_anisotropy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_silverIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_silverSpread"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ambient"));
            SerializedProperty selfShadow = serializedObject.FindProperty("m_selfShadowing");
            EditorGUILayout.PropertyField(selfShadow);
            if (selfShadow.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringAmount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringBias"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringRampDown"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_multipleScatteringRampDownShape"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_lightPollutionDimmer"));
            SerializedProperty celShade = serializedObject.FindProperty("m_celShade");
            EditorGUILayout.PropertyField(celShade);
            if (celShade.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_celShadeColorBands"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_celShadeTransmittanceBands"));
            }
            SerializedProperty castShadows = serializedObject.FindProperty("m_castShadows");
            EditorGUILayout.PropertyField(castShadows);
            if (castShadows.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxShadowIntensity"));
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        m_movementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_movementFoldout, "Movement");
        if (m_movementFoldout) {
            SerializedProperty offset = serializedObject.FindProperty("m_useOffset");
            EditorGUILayout.PropertyField(offset);
            if (offset.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_samplingOffset"));
            } else {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_velocity"));
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        m_qualityFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_qualityFoldout, "Quality");
        if (m_qualityFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_reprojectionFrames"));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif // UNITY_EDITOR

} // namespace Expanse
