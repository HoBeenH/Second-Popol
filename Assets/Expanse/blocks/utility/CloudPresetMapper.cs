using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Expanse;

namespace Expanse {

public class CloudPresetMapper : MonoBehaviour
{
    public string m_sourcePreset = "";
    public string m_targetPreset = "";

    // Maps source preset onto target preset and saves
    // result at filepath.
    public void Map(string filepath) {
        if (m_sourcePreset == "") {
            Debug.LogError("No source preset specified");
        }
        if (m_targetPreset == "") {
            Debug.LogError("No target preset specified");
        }

        UniversalCloudLayer source = UniversalCloudLayer.load(m_sourcePreset);
        UniversalCloudLayer target = UniversalCloudLayer.load(m_targetPreset);

        // First, match the non-lerpable params.
        source.renderSettings.geometryType = target.renderSettings.geometryType;
        source.renderSettings.selfShadowing = target.renderSettings.selfShadowing;
        source.renderSettings.highQualityShadows = target.renderSettings.highQualityShadows;
        source.renderSettings.celShade = target.renderSettings.celShade;
        source.renderSettings.castShadows = target.renderSettings.castShadows;
        source.renderSettings.depthProbabilityDetailIndex = target.renderSettings.depthProbabilityDetailIndex;
        source.noiseTextureQuality = target.noiseTextureQuality;

        // Now, match the volume x/z extents, and try our best to
        // compute a scale factor by which to adjust the noise.
        Vector2 geometricScaleFactor = new Vector2(
            (source.renderSettings.geometryXExtent.y - source.renderSettings.geometryXExtent.x) 
            / (target.renderSettings.geometryXExtent.y - target.renderSettings.geometryXExtent.x),
            (source.renderSettings.geometryZExtent.y - source.renderSettings.geometryZExtent.x) 
            / (target.renderSettings.geometryZExtent.y - target.renderSettings.geometryZExtent.x)
        );
        source.renderSettings.geometryXExtent = target.renderSettings.geometryXExtent;
        source.renderSettings.geometryZExtent = target.renderSettings.geometryZExtent;

        // Now, for each noise layer, match the tile and adjust the scale accordingly.
        for (int i = 0; i < source.noiseLayers.Length; i++) {
            if (!source.noiseLayers[i].procedural || !target.noiseLayers[i].procedural) {
                Debug.LogError("Only entirely procedural cloud presets can be mapped onto one another---a noise layer in one of the selected presets was a texture.");
                return;
            }
            float tileFactor = (float) source.noiseLayers[i].renderSettings.tile / (float) target.noiseLayers[i].renderSettings.tile;
            source.noiseLayers[i].renderSettings.scale = new Vector2(
                Mathf.Floor(tileFactor * source.noiseLayers[i].renderSettings.scale.x / geometricScaleFactor.x), 
                Mathf.Floor(tileFactor * source.noiseLayers[i].renderSettings.scale.y / geometricScaleFactor.y)
            );
            source.noiseLayers[i].renderSettings.tile = target.noiseLayers[i].renderSettings.tile;
        }

        UniversalCloudLayer.save(source, filepath);
    }

    public void LoadSourcePreset(string filepath) {
        m_sourcePreset = filepath;
    }

    public void LoadTargetPreset(string filepath) {
        m_targetPreset = filepath;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CloudPresetMapper))]
public class CloudPresetMapperEditor : Editor
{
    override public void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Source Preset: " + serializedObject.FindProperty("m_sourcePreset").stringValue);
        EditorGUILayout.LabelField("Target Preset: " + serializedObject.FindProperty("m_targetPreset").stringValue);

        if (GUILayout.Button("Load Source Preset"))
        {
            string pathToLoad = EditorUtility.OpenFilePanel("", Application.dataPath, "json");
            if (pathToLoad.Length != 0) {
                CloudPresetMapper mapper = (CloudPresetMapper) target;
                mapper.LoadSourcePreset(pathToLoad);
            }
        }

        if (GUILayout.Button("Load Target Preset"))
        {
            string pathToLoad = EditorUtility.OpenFilePanel("", Application.dataPath, "json");
            if (pathToLoad.Length != 0) {
                CloudPresetMapper mapper = (CloudPresetMapper) target;
                mapper.LoadTargetPreset(pathToLoad);
            }
        }

        if (GUILayout.Button("Map Source Onto Target"))
        {
            string pathToSave = EditorUtility.SaveFilePanelInProject("Save Preset", "", "json", "");
            if (pathToSave.Length != 0) {
                CloudPresetMapper mapper = (CloudPresetMapper) target;
                mapper.Map(pathToSave);
            }
        }

    }
}

#endif // UNITY_EDITOR

} // namespace Expanse