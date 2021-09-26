#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace Expanse {

/**
 * @brief: custom editor for cloud layer settings. Allows us to draw
 * a custom representation of a SerializedDataParameter representing an
 * CloudLayerSettings object. We store a canonical instance of this class
 * in /code/source/ui/scripts/ExpanseEditor.cs.
 * */
[VolumeComponentEditor(typeof(CloudLayerSettings))]
class CloudLayerEditor : VolumeComponentEditor {

  /**
   * @brief: private helper class for drawing cloud noise layer settings.
   * */
  [VolumeComponentEditor(typeof(CloudNoiseLayerSettings))]
  private class CloudNoiseLayerEditor : VolumeComponentEditor {

    private Dictionary<string, SerializedDataParameter> parameters = new Dictionary<string, SerializedDataParameter>(){
      {"procedural", null},
      {"noiseTexture2D", null},
      {"noiseTexture3D", null},
      {"noiseType", null},
      {"scale", null},
      {"octaves", null},
      {"octaveScale", null},
      {"octaveMultiplier", null},
      {"tile", null},
    };

    public void draw(SerializedDataParameter parameter, Datatypes.NoiseDimension dimension) {

      /* Collect the parameters. */
      var it = parameter.value.Copy();
      var end = it.GetEndProperty();
      bool first = true;
      var keys = new List<string>(parameters.Keys);
      foreach(string name in keys) {
        it.Next(first);
        parameters[name] = Unpack(it);
        first = false;
      }

      /* Draw the UI. */
      PropertyField(parameters["procedural"]);
      if (parameters["procedural"].value.boolValue) {
        PropertyField(parameters["noiseType"]);
        PropertyField(parameters["scale"]);
        PropertyField(parameters["octaves"]);
        PropertyField(parameters["octaveScale"]);
        PropertyField(parameters["octaveMultiplier"]);
        PropertyField(parameters["tile"]);
      } else {
        if (dimension == Datatypes.NoiseDimension.TwoDimensional) {
          PropertyField(parameters["noiseTexture2D"], new GUIContent("Noise Texture"));
          PropertyField(parameters["tile"]);
        } else {
          PropertyField(parameters["noiseTexture3D"], new GUIContent("Noise Texture"));
          PropertyField(parameters["tile"]);
        }
      }
    }
  }

  private Dictionary<string, SerializedDataParameter> parameters = new Dictionary<string, SerializedDataParameter>(){
    {"enabled", null},

    /* Geometry. */
    {"geometryType", null},
    {"geometryXExtent", null},
    {"geometryYExtent", null},
    {"geometryZExtent", null},
    {"geometryHeight", null},

    /* Noise. */
    {"noiseTextureQuality", null},
    {"coverageIntensity", null},
    {"structureIntensity", null},
    {"detailIntensity", null},
    {"baseWarpIntensity", null},
    {"detailWarpIntensity", null},
    {"heightGradientBottom", null},
    {"heightGradientTop", null},
    {"windSkew", null},
    {"rounding", null},
    {"roundingShape", null},
    {"coverageNoiseLayer", null},
    {"baseNoiseLayer", null},
    {"structureNoiseLayer", null},
    {"detailNoiseLayer", null},
    {"baseWarpNoiseLayer", null},
    {"detailWarpNoiseLayer", null},

    /* Movement. */
    {"coverageOffset", null},
    {"baseOffset", null},
    {"structureOffset", null},
    {"detailOffset", null},
    {"baseWarpOffset", null},
    {"detailWarpOffset", null},

    /* Lighting. */
    /* 2D and 3D. */
    {"density", null},
    {"attenuationDistance", null},
    {"attenuationBias", null},
    {"rampUp", null},
    {"extinctionCoefficients", null},
    {"scatteringCoefficients", null},
    {"multipleScatteringAmount", null},
    {"multipleScatteringBias", null},
    {"multipleScatteringRampDown", null},
    {"multipleScatteringRampDownShape", null},
    {"silverSpread", null},
    {"silverIntensity", null},
    {"anisotropy", null},
    {"ambient", null},
    {"ambientHeightRange", null},
    {"ambientStrengthRange", null},
    {"celShade", null},
    {"celShadeLightingBands", null},
    {"celShadeTransmittanceBands", null},
    /* 2D. */
    {"apparentThickness", null},
    /* 3D. */
    {"verticalProbabilityHeightRange", null},
    {"verticalProbabilityStrength", null},
    {"depthProbabilityHeightRange", null},
    {"depthProbabilityStrengthRange", null},
    {"depthProbabilityDensityMultiplier", null},
    {"depthProbabilityBias", null},
    {"depthProbabilityHighDetail", null},
    {"lightPollutionDimmer", null},

    /* Sampling. */
    {"castShadows", null},
    {"maxShadowIntensity", null},
    {"selfShadowing", null},
    {"highQualityShadows", null},
    {"maxSelfShadowDistance", null},
    {"coarseStepRange", null},
    {"detailStepRange", null},
    {"stepDistanceRange", null},
    {"flythroughStepRange", null},
    {"flythroughStepDistanceRange", null},
    {"mediaZeroThreshold", null},
    {"transmittanceZeroThreshold", null},
    {"maxConsecutiveZeroSamples", null},
    {"reprojectionFrames", null},
    {"useTemporalDenoising", null},
    {"denoisingHistoryFrames", null},
    {"optimizeForFlythrough", null},
    {"flythroughThreshold", null}
  };

  /* Since the cloud sub-ui is a little more complicated, we have some
   * internal state we need to manage. */
  bool m_geometryFoldout = false;
  bool m_noiseFoldout = false;
  bool m_movementFoldout = false;
  bool m_lightingFoldout = false;
  bool m_samplingFoldout = false;

  /* Dropdown selection state. */
  CloudDatatypes.CloudNoiseLayer m_noiseLayerSelect = CloudDatatypes.CloudNoiseLayer.Coverage;
  /* For drawing the selected layer. */
  CloudNoiseLayerEditor m_noiseLayerEditor = new CloudNoiseLayerEditor();

  public void draw(SerializedDataParameter parameter) {

    /* Collect the parameters. */
    var it = parameter.value.Copy();
    var end = it.GetEndProperty();
    bool first = true;
    var keys = new List<string>(parameters.Keys);
    foreach(string name in keys) {
      it.Next(first);
      parameters[name] = Unpack(it);
      first = false;
    }

    /* Get the geometry type and noise dimension, since that will determine
     * some things about how we build the UI. */
    CloudDatatypes.CloudGeometryType geometryType = (CloudDatatypes.CloudGeometryType) parameters["geometryType"].value.enumValueIndex;
    Datatypes.NoiseDimension noiseDimension = CloudDatatypes.cloudGeometryTypeToNoiseDimension(geometryType);
    /* Draw the UI. */
    PropertyField(parameters["enabled"]);

    /* Geometry foldout. */
    /* End the previous foldout header group to create a nesting effect. */
    EditorGUILayout.EndFoldoutHeaderGroup();
    m_geometryFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_geometryFoldout, "Geometry", ExpanseStyles.indentedFoldoutStyle(1));
    EditorGUI.indentLevel++;
    if (m_geometryFoldout) {
      PropertyField(parameters["geometryType"]);
      /* HACK: at the time of writing this, the noise dimension corresponds
       * with the necessary geometry attributes. However, in the future,
       * these two things could get decoupled, at which point this would need
       * to change. */
      if (noiseDimension == Datatypes.NoiseDimension.TwoDimensional) {
        PropertyField(parameters["geometryXExtent"]);
        PropertyField(parameters["geometryZExtent"]);
        PropertyField(parameters["geometryHeight"]);
      } else {
        PropertyField(parameters["geometryXExtent"]);
        PropertyField(parameters["geometryYExtent"]);
        PropertyField(parameters["geometryZExtent"]);
      }
    }
    EditorGUI.indentLevel--;
    EditorGUILayout.EndFoldoutHeaderGroup();

    /* Noise foldout. */
    m_noiseFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_noiseFoldout, "Modeling", ExpanseStyles.indentedFoldoutStyle(1));
    EditorGUI.indentLevel++;
    if (m_noiseFoldout) {
      PropertyField(parameters["noiseTextureQuality"]);
      PropertyField(parameters["coverageIntensity"], new GUIContent("Coverage"));
      PropertyField(parameters["structureIntensity"], new GUIContent("Structure"));
      PropertyField(parameters["detailIntensity"], new GUIContent("Detail"));
      PropertyField(parameters["baseWarpIntensity"], new GUIContent("Base Warp"));
      PropertyField(parameters["detailWarpIntensity"], new GUIContent("Detail Warp"));
      if (noiseDimension == Datatypes.NoiseDimension.ThreeDimensional) {
        PropertyField(parameters["heightGradientBottom"]);
        PropertyField(parameters["heightGradientTop"]);
        PropertyField(parameters["windSkew"]);
        PropertyField(parameters["rounding"]);
        PropertyField(parameters["roundingShape"]);
      }

      /* TODO: fix me! Alignment is still a little off. */
      EditorGUILayout.Space();
      EditorGUI.indentLevel++;
      GUIStyle indentedDropdown = ExpanseStyles.indentedDropdownStyle(0.77f);
      m_noiseLayerSelect = (CloudDatatypes.CloudNoiseLayer) EditorGUILayout.EnumPopup("Noise Layer", m_noiseLayerSelect, indentedDropdown);
      EditorGUI.indentLevel--;

      /* The variable name is just the enum name converted to camelCase. */
      string layerName = Enum.GetName(typeof(CloudDatatypes.CloudNoiseLayer), m_noiseLayerSelect);
      layerName = Char.ToLowerInvariant(layerName[0]) + layerName.Substring(1);
      Datatypes.NoiseDimension noiseLayerDimension = (m_noiseLayerSelect == CloudDatatypes.CloudNoiseLayer.Coverage) ? Datatypes.NoiseDimension.TwoDimensional : noiseDimension;

      m_noiseLayerEditor.draw(parameters[layerName + "NoiseLayer"], noiseLayerDimension);
    }
    EditorGUI.indentLevel--;
    EditorGUILayout.EndFoldoutHeaderGroup();

    /* Movement foldout. */
    m_movementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_movementFoldout, "Movement", ExpanseStyles.indentedFoldoutStyle(1));
    EditorGUI.indentLevel++;
    if (m_movementFoldout) {
      PropertyField(parameters["coverageOffset"]);
      PropertyField(parameters["baseOffset"]);
      PropertyField(parameters["structureOffset"]);
      PropertyField(parameters["detailOffset"]);
      PropertyField(parameters["baseWarpOffset"]);
      PropertyField(parameters["detailWarpOffset"]);
    }
    EditorGUI.indentLevel--;
    EditorGUILayout.EndFoldoutHeaderGroup();

    /* Lighting foldout. */
    m_lightingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_lightingFoldout, "Lighting", ExpanseStyles.indentedFoldoutStyle(1));
    EditorGUI.indentLevel++;
    if (m_lightingFoldout) {
      if (noiseDimension == Datatypes.NoiseDimension.TwoDimensional) {
        PropertyField(parameters["extinctionCoefficients"]);
        PropertyField(parameters["scatteringCoefficients"]);
        PropertyField(parameters["density"]);
        PropertyField(parameters["apparentThickness"]);
        PropertyField(parameters["anisotropy"]);
        PropertyField(parameters["silverIntensity"]);
        PropertyField(parameters["silverSpread"]);
        PropertyField(parameters["attenuationDistance"]);
        PropertyField(parameters["attenuationBias"]);
        PropertyField(parameters["rampUp"]);
        PropertyField(parameters["ambient"]);
        PropertyField(parameters["selfShadowing"]);
        if (parameters["selfShadowing"].value.boolValue) {
          PropertyField(parameters["multipleScatteringAmount"]);
          PropertyField(parameters["multipleScatteringBias"]);
          PropertyField(parameters["multipleScatteringRampDown"]);
          PropertyField(parameters["multipleScatteringRampDownShape"]);
        }
        PropertyField(parameters["celShade"]);
        if (parameters["celShade"].value.boolValue) {
          PropertyField(parameters["celShadeLightingBands"]);
          PropertyField(parameters["celShadeTransmittanceBands"]);
        }
        PropertyField(parameters["castShadows"]);
        if (parameters["castShadows"].value.boolValue) {
          PropertyField(parameters["maxShadowIntensity"]);
        }
      } else {
        PropertyField(parameters["extinctionCoefficients"]);
        PropertyField(parameters["scatteringCoefficients"]);
        PropertyField(parameters["density"]);
        PropertyField(parameters["anisotropy"]);
        PropertyField(parameters["silverIntensity"]);
        PropertyField(parameters["silverSpread"]);
        PropertyField(parameters["ambientHeightRange"]);
        PropertyField(parameters["ambientStrengthRange"]);
        PropertyField(parameters["attenuationDistance"]);
        PropertyField(parameters["attenuationBias"]);
        PropertyField(parameters["rampUp"]);
        PropertyField(parameters["verticalProbabilityHeightRange"]);
        PropertyField(parameters["verticalProbabilityStrength"]);
        PropertyField(parameters["depthProbabilityHeightRange"]);
        PropertyField(parameters["depthProbabilityStrengthRange"]);
        PropertyField(parameters["depthProbabilityDensityMultiplier"]);
        PropertyField(parameters["depthProbabilityBias"]);
        PropertyField(parameters["depthProbabilityHighDetail"]);
        PropertyField(parameters["selfShadowing"]);
        if (parameters["selfShadowing"].value.boolValue) {
          PropertyField(parameters["highQualityShadows"]);
          PropertyField(parameters["maxSelfShadowDistance"]);
          PropertyField(parameters["multipleScatteringAmount"]);
          PropertyField(parameters["multipleScatteringBias"]);
          PropertyField(parameters["multipleScatteringRampDown"]);
          PropertyField(parameters["multipleScatteringRampDownShape"]);
        }
        PropertyField(parameters["lightPollutionDimmer"]);
        PropertyField(parameters["celShade"]);
        if (parameters["celShade"].value.boolValue) {
          PropertyField(parameters["celShadeLightingBands"]);
          PropertyField(parameters["celShadeTransmittanceBands"]);
        }
        PropertyField(parameters["castShadows"]);
        if (parameters["castShadows"].value.boolValue) {
          PropertyField(parameters["maxShadowIntensity"]);
        }
      }
    }
    EditorGUI.indentLevel--;
    EditorGUILayout.EndFoldoutHeaderGroup();

    /* Only display if this layer is volumetric. */
    m_samplingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_samplingFoldout, "Sampling And Quality", ExpanseStyles.indentedFoldoutStyle(1));
    EditorGUI.indentLevel++;
    if (m_samplingFoldout) {
      if (noiseDimension == Datatypes.NoiseDimension.ThreeDimensional) {
        PropertyField(parameters["coarseStepRange"]);
        PropertyField(parameters["detailStepRange"]);
        PropertyField(parameters["stepDistanceRange"]);
        PropertyField(parameters["flythroughStepRange"]);
        PropertyField(parameters["flythroughStepDistanceRange"]);
        PropertyField(parameters["mediaZeroThreshold"]);
        PropertyField(parameters["transmittanceZeroThreshold"]);
        PropertyField(parameters["maxConsecutiveZeroSamples"]);
        PropertyField(parameters["useTemporalDenoising"]);
        if (parameters["useTemporalDenoising"].value.boolValue) {
          PropertyField(parameters["denoisingHistoryFrames"]);
        }
      }
      PropertyField(parameters["reprojectionFrames"]);
    }
    EditorGUI.indentLevel--;
    EditorGUILayout.EndFoldoutHeaderGroup();

  }
}

} // namespace Expanse

#endif // #if UNITY_EDITOR
