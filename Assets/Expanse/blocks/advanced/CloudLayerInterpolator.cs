using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using Expanse;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CloudLayerInterpolator : MonoBehaviour
{ //
    [Min(0.1f), Tooltip("Amount of time it takes to transition to the target preset, in seconds.")]
    public float m_transitionTime = 10;
    [Tooltip("If enabled, ignores noise offsets in presets when interpolating. Generally, you'll want this to be the case, that way your noises can keep scrolling as the interpolation occurs.")]
    public bool m_bypassOffset = true;

    // State.
    public float m_interpolationAmount = 0;
    public string m_currentPresetName = "";
    public string m_targetPresetName = "";
    private UniversalCloudLayer m_currentPreset = null;
    private UniversalCloudLayer m_targetPreset = null;
    private int m_presetsLoaded = 0;
    private int m_prevPresetsLoaded = 0;
    private bool m_interpolating = false;

    // Interpolation blocks.
    public BaseCloudLayerBlock m_cloudLayer;
    public TextureInterpolationBlock m_coverageInterpolated;
    public ProceduralNoiseBlock m_coverageCurrent;
    public ProceduralNoiseBlock m_coverageTarget;
    public TextureInterpolationBlock m_baseInterpolated;
    public ProceduralNoiseBlock m_baseCurrent;
    public ProceduralNoiseBlock m_baseTarget;
    public TextureInterpolationBlock m_structureInterpolated;
    public ProceduralNoiseBlock m_structureCurrent;
    public ProceduralNoiseBlock m_structureTarget;
    public TextureInterpolationBlock m_detailInterpolated;
    public ProceduralNoiseBlock m_detailCurrent;
    public ProceduralNoiseBlock m_detailTarget;
    public TextureInterpolationBlock m_baseWarpInterpolated;
    public ProceduralNoiseBlock m_baseWarpCurrent;
    public ProceduralNoiseBlock m_baseWarpTarget;
    public TextureInterpolationBlock m_detailWarpInterpolated;
    public ProceduralNoiseBlock m_detailWarpCurrent;
    public ProceduralNoiseBlock m_detailWarpTarget;
    // Private array of the above blocks so that it's easier to iterate over them.
    private TextureInterpolationBlock[] m_interpolatedTextures = new TextureInterpolationBlock[CloudDatatypes.kNumCloudNoiseLayers];
    private ProceduralNoiseBlock[] m_currentTextures = new ProceduralNoiseBlock[CloudDatatypes.kNumCloudNoiseLayers];
    private ProceduralNoiseBlock[] m_targetTextures = new ProceduralNoiseBlock[CloudDatatypes.kNumCloudNoiseLayers];

    // Can be called programmatically, or triggered from the load preset button.
    public void LoadPreset(string filepath) {
        if (m_currentPreset == null) {
            m_currentPreset = UniversalCloudLayer.load(filepath);
            m_currentPresetName = Path.GetFileName(filepath);
            m_targetPreset = null;
        } else {
            if (m_targetPreset != null) {
                m_currentPreset = m_targetPreset;
                m_currentPresetName = m_targetPresetName;
            }
            m_targetPreset = UniversalCloudLayer.load(filepath);
            m_targetPresetName = Path.GetFileName(filepath);
        }
        m_prevPresetsLoaded = m_presetsLoaded;
        m_presetsLoaded++;
        m_interpolationAmount = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_currentPreset = null;
        m_targetPreset = null;
        m_presetsLoaded = 0;
        m_prevPresetsLoaded = 0;
        m_interpolating = false;
    }

    void Update()
    {
        bool firstFrameAfterPresetChange = (m_prevPresetsLoaded == m_presetsLoaded - 1);

        // Reset amount back to zero when we reach the target.
        if (m_interpolationAmount == 1.0f) {
            m_currentPreset = m_targetPreset;
            m_currentPresetName = m_targetPresetName;
            m_interpolationAmount = 0;
            m_targetPreset = null;
        }

        // Resync null => none names
        m_currentPresetName = (m_currentPreset == null) ? "None" : m_currentPresetName;
        m_targetPresetName = (m_targetPreset == null) ? "None" : m_targetPresetName;

        // If we have a non-trivial target, increment interpolation amount.
        if (m_targetPreset != null) {
            m_interpolationAmount = Mathf.Clamp(m_interpolationAmount + Time.deltaTime / m_transitionTime, 0, 1);
        }

        if (m_currentPreset == null) {
            return;
        }

        // If we have no target preset, just use the current preset.
        UniversalCloudLayer interpolated = m_currentPreset;
        if (m_targetPreset != null) {
            m_interpolating = true;
            interpolated = UniversalCloudLayer.lerp(m_currentPreset, m_targetPreset, m_interpolationAmount);
        } else {
            m_interpolating = false;
        }

        // Set the cloud layer to use the interpolated settings.
        if (m_cloudLayer == null) {
            Debug.LogWarning("Expanse CloudLayerInterpolator: No cloud layer set up");
            return;
        }
        m_cloudLayer.FromUniversal(interpolated, m_bypassOffset);

        // Populate component array to make the following more succinct.
        populateTextureArrays();

        // Lerp each layer that's available to us.
        for (int i = 0; i < CloudDatatypes.kNumCloudNoiseLayers; i++) {
            if (m_interpolatedTextures[i] == null || m_currentTextures[i] == null || m_targetTextures[i] == null) {
                continue;
            }
            if (m_targetPreset == null) {
                // Disable interpolation, but if this is the first frame after a preset change, 
                // interpolate once, just to avoid artifacts.
                if (firstFrameAfterPresetChange) {
                    m_interpolatedTextures[i].EnableRendering();
                } else {
                    m_interpolatedTextures[i].DisableRendering();
                }
                // Just use current texture
                m_interpolatedTextures[i].m_blend = 0;
                m_currentTextures[i].m_dimension = (i == (int) CloudDatatypes.CloudNoiseLayer.Coverage) ? Datatypes.NoiseDimension.TwoDimensional : CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) m_currentPreset.renderSettings.geometryType);
                m_currentTextures[i].m_noiseType = m_currentPreset.noiseLayers[i].renderSettings.noiseType;
                m_currentTextures[i].m_scale = Vector2Int.FloorToInt(m_currentPreset.noiseLayers[i].renderSettings.scale); 
                m_currentTextures[i].m_octaves = m_currentPreset.noiseLayers[i].renderSettings.octaves; 
                m_currentTextures[i].m_octaveScale = m_currentPreset.noiseLayers[i].renderSettings.octaveScale; 
                m_currentTextures[i].m_octaveMultiplier = m_currentPreset.noiseLayers[i].renderSettings.octaveMultiplier;
                m_cloudLayer.SetTexture((CloudDatatypes.CloudNoiseLayer) i, m_currentTextures[i].GetTexture(), m_currentPreset.noiseLayers[i].renderSettings.tile);
            } else {
                // Enable interpolation
                m_interpolatedTextures[i].EnableRendering();
                m_interpolatedTextures[i].m_blend = m_interpolationAmount;
                // Make sure each texture is in the right slot
                m_interpolatedTextures[i].m_textureA = m_currentTextures[i];
                m_interpolatedTextures[i].m_textureB = m_targetTextures[i];
                // Force tile to be lower value, and compute the relative tile factor. AKA---do our best to
                // accomodate varying tile factors, knowing that it probably won't work.
                float relativeTileA = Mathf.Max(1, Mathf.Max(1, (float) m_currentPreset.noiseLayers[i].renderSettings.tile) / Mathf.Max(1, (float) m_targetPreset.noiseLayers[i].renderSettings.tile));
                float relativeTileB = Mathf.Max(1, Mathf.Max(1, (float) m_targetPreset.noiseLayers[i].renderSettings.tile) / Mathf.Max(1, (float) m_currentPreset.noiseLayers[i].renderSettings.tile));
                m_interpolatedTextures[i].m_tileA = relativeTileA;
                m_interpolatedTextures[i].m_tileB = relativeTileB;
                // Current
                m_currentTextures[i].m_dimension = (i == (int) CloudDatatypes.CloudNoiseLayer.Coverage) ? Datatypes.NoiseDimension.TwoDimensional : CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) m_currentPreset.renderSettings.geometryType);
                m_currentTextures[i].m_noiseType = m_currentPreset.noiseLayers[i].renderSettings.noiseType;
                m_currentTextures[i].m_scale = Vector2Int.FloorToInt(m_currentPreset.noiseLayers[i].renderSettings.scale); 
                m_currentTextures[i].m_octaves = m_currentPreset.noiseLayers[i].renderSettings.octaves; 
                m_currentTextures[i].m_octaveScale = m_currentPreset.noiseLayers[i].renderSettings.octaveScale; 
                m_currentTextures[i].m_octaveMultiplier = m_currentPreset.noiseLayers[i].renderSettings.octaveMultiplier; 
                // Target
                m_targetTextures[i].m_dimension = (i == (int) CloudDatatypes.CloudNoiseLayer.Coverage) ? Datatypes.NoiseDimension.TwoDimensional : CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) m_currentPreset.renderSettings.geometryType);
                m_targetTextures[i].m_noiseType = m_targetPreset.noiseLayers[i].renderSettings.noiseType;
                m_targetTextures[i].m_scale = Vector2Int.FloorToInt(m_targetPreset.noiseLayers[i].renderSettings.scale); 
                m_targetTextures[i].m_octaves = m_targetPreset.noiseLayers[i].renderSettings.octaves; 
                m_targetTextures[i].m_octaveScale = m_targetPreset.noiseLayers[i].renderSettings.octaveScale; 
                m_targetTextures[i].m_octaveMultiplier = m_targetPreset.noiseLayers[i].renderSettings.octaveMultiplier;

                m_cloudLayer.SetTexture((CloudDatatypes.CloudNoiseLayer) i, 
                    m_interpolatedTextures[i].GetTexture(), 
                    Mathf.Min(m_currentPreset.noiseLayers[i].renderSettings.tile, m_targetPreset.noiseLayers[i].renderSettings.tile));
            }
        }
    }

    public bool IsInterpolating() {
        return m_interpolating;
    }

    private void populateTextureArrays() {
        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.Coverage] = m_coverageInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.Coverage] = m_coverageCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.Coverage] = m_coverageTarget; 

        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.Base] = m_baseInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.Base] = m_baseCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.Base] = m_baseTarget; 

        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.Structure] = m_structureInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.Structure] = m_structureCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.Structure] = m_structureTarget; 

        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.Detail] = m_detailInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.Detail] = m_detailCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.Detail] = m_detailTarget; 

        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.BaseWarp] = m_baseWarpInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.BaseWarp] = m_baseWarpCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.BaseWarp] = m_baseWarpTarget; 

        m_interpolatedTextures[(int) CloudDatatypes.CloudNoiseLayer.DetailWarp] = m_detailWarpInterpolated; 
        m_currentTextures[(int) CloudDatatypes.CloudNoiseLayer.DetailWarp] = m_detailWarpCurrent; 
        m_targetTextures[(int) CloudDatatypes.CloudNoiseLayer.DetailWarp] = m_detailWarpTarget; 
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CloudLayerInterpolator))]
public class CloudLayerInterpolatorEditor : Editor
{
    bool m_componentFoldout = false;
    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transitionTime"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bypassOffset"));

        EditorGUILayout.Slider("Progress", serializedObject.FindProperty("m_interpolationAmount").floatValue, 0, 1);

        EditorGUILayout.LabelField("Current Preset: " + serializedObject.FindProperty("m_currentPresetName").stringValue);
        EditorGUILayout.LabelField("Target Preset: " + serializedObject.FindProperty("m_targetPresetName").stringValue);

        if (GUILayout.Button("Load Preset"))
        {
            string pathToLoad = EditorUtility.OpenFilePanel("", Application.dataPath, "json");
            if (pathToLoad.Length != 0) {
                CloudLayerInterpolator interpolator = (CloudLayerInterpolator) target;
                interpolator.LoadPreset(pathToLoad);
            }
        }
        
        m_componentFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_componentFoldout, "Components");
        if (m_componentFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_cloudLayer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpInterpolated"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpCurrent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpTarget"));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif // UNITY_EDITOR

} // namespace Expanse