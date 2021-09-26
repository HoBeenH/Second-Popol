using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class ProceduralCloudVolumeBlock : BaseCloudLayerBlock
{
    /* Name. Used to refer to the layer in UI and printouts. */
    public string m_name = "defaultCloudLayerName";

    /* Internal universal representation. */
    private UniversalCloudLayer m_universal = new UniversalCloudLayer();

    /* User-exposed controls. */

    /* Modeling. */
    [Tooltip("Whether or not the cloud layer is curved with the surface of the planet.")]
    public bool m_curved = false;
    [Tooltip("Origin of this layer's cloud geometry---the center of the cloud volume.")]
    public Vector3 m_origin = new Vector3(0, 6000, 0);
    [Min(0), Tooltip("X extent---\"width\"---of this cloud layer's geometry.")]
    public float m_XExtent = 200000;
    [Min(0), Tooltip("Y extent---\"thickness\"---of this cloud layer's geometry.")]
    public float m_YExtent = 6000;
    [Min(0), Tooltip("Z extent---\"depth\"---of this cloud layer's geometry.")]
    public float m_ZExtent = 200000;
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
    [Min(0), Tooltip("How much to round off the tops of the clouds.")]
    public float m_rounding = 3;
    [Min(0), Tooltip("The curve of the rounding.")]
    public float m_roundingShape = 1;
    [MinMaxSlider(0, 1), Tooltip("Bottom of the height gradient.")]
    public Vector2 m_heightGradientBottom = new Vector2(0.05f, 0.1f);
    [MinMaxSlider(0, 1), Tooltip("Top of the height gradient.")]
    public Vector2 m_heightGradientTop = new Vector2(0.9f, 1.0f);
    [Tooltip("Skew over height of the clouds due to wind.")]
    public Vector2 m_windSkew = new Vector2(0, 0);

    /* Noise editor. */
    /* Settings for each noise authoring layer. */
    [System.Serializable]
    public sealed class LayerSettings {
        [Tooltip("Whether to use procedural noise or a texture this noise layer.")]
        public bool procedural = true;
        [Tooltip("Noise texture for this layer.")]
        public Texture texture = null;
        [Tooltip("Texture generator for this layer---can be used as an alternative to the noise texture.")]
        public BaseTextureGeneratorBlock textureGenerator = null;
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
    [MinMaxSlider(0, 1), Tooltip("Height range of ambient lighting the clouds receive from the sky. The bottom of clouds generally receives less ambient light than the top. Tweaking this can help reveal self-shadowed details.")]
    public Vector2 m_ambientHeightRange = new Vector2(0, 1);
    [MinMaxSlider(0, 5), Tooltip("Strength range of ambient lighting the clouds receive from the sky, applied over the specified height range.")]
    public Vector2 m_ambientStrengthRange = new Vector2(0.5f, 1);
    [MinMaxSlider(0, 1), Tooltip("Unit height range to apply vertical in-scattering probability to.")]
    public Vector2 m_verticalProbabilityHeightRange = new Vector2(0.05f, 0.2f);
    [Min(0), Tooltip("Strength of vertical in-scattering probablity.")]
    public float m_verticalProbabilityStrength = 0.8f;
    [MinMaxSlider(0, 1), Tooltip("Unit height range to apply vertical in-scattering probability to.")]
    public Vector2 m_depthProbabilityHeightRange = new Vector2(0.05f, 0.75f);
    [MinMaxSlider(0, 5), Tooltip("Unit height range to apply vertical in-scattering probability to.")]
    public Vector2 m_depthProbabilityStrengthRange = new Vector2(0.05f, 2);
    [Min(0), Tooltip("Pre-multiplier on density for depth in-scattering probability. Can be useful for bringing the density into a range where the effect is noticeable.")]
    public float m_depthProbabilityDensityMultiplier = 3;
    [Range(0, 1), Tooltip("Pre-multiplier on density for depth in-scattering probability. Can be useful for bringing the density into a range where the effect is noticeable.")]
    public float m_depthProbabilityBias = 0.1f;
    [Tooltip("Whether or not to use a higher detail version of depth probability. Off by default to accommodate previous presets.")]
    public bool m_depthProbabilityHighDetail = false;

    [Tooltip("Whether or not the clouds cast shadows on themselves.")]
    public bool m_selfShadowing = false;

    [Range(1, 10000), Tooltip("Max distance that clouds can cast shadows onto themselves/each other.")]
    public float m_maxSelfShadowDistance = 1000;
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
    [Tooltip("Velocity of the cloud coverage map. Automates the sampling offset parameter.")]
    public Vector2 m_coverageVelocity = new Vector2(0.001f, -0.001f);
    [Tooltip("Velocity of the cloud base noise. Automates the sampling offset parameter.")]
    public Vector2 m_baseVelocity = new Vector2(0, 0);
    [Tooltip("Velocity of the cloud structure noise. Automates the sampling offset parameter.")]
    public Vector2 m_structureVelocity = new Vector2(0, 0);
    [Tooltip("Velocity of the cloud detail noise. Automates the sampling offset parameter.")]
    public Vector2 m_detailVelocity = new Vector2(0, 0);
    [Tooltip("Velocity of the cloud base warp noise. Automates the sampling offset parameter.")]
    public Vector2 m_baseWarpVelocity = new Vector2(0, 0);
    [Tooltip("Velocity of the cloud detail warp noise. Automates the sampling offset parameter.")]
    public Vector2 m_detailWarpVelocity = new Vector2(0, 0);
    [Tooltip("Sampling offsets for coverage map. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_coverageOffset = new Vector2(0, 0);
    [Tooltip("Sampling offsets for base noise. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_baseOffset = new Vector2(0, 0);
    [Tooltip("Sampling offsets for coverage map. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_structureOffset = new Vector2(0, 0);
    [Tooltip("Sampling offsets for coverage map. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_detailOffset = new Vector2(0, 0);
    [Tooltip("Sampling offsets for coverage map. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_baseWarpOffset = new Vector2(0, 0);
    [Tooltip("Sampling offsets for coverage map. Can be animated as an alternative to the velocity parameter.")]
    public Vector2 m_detailWarpOffset = new Vector2(0, 0);

    /* Sampling And Quality. */
    [Range(1, 4), Tooltip("Number of history frames to use for reprojection. Increasing can improve performance, but at the cost of quality")]
    public int m_reprojectionFrames = 2;
    [MinMaxSlider(1, 256), Tooltip("Step number range for coarse ray marching.")]
    public Vector2 m_coarseStepRange = new Vector2(16, 32);
    [MinMaxSlider(1, 256), Tooltip("Step number range for detail ray marching.")]
    public Vector2 m_detailStepRange = new Vector2(64, 96);
    [MinMaxSlider(50, 200000), Tooltip("Distance range over which to apply the step number ranges.")]
    public Vector2 m_stepDistanceRange = new Vector2(125, 20000);
    [MinMaxSlider(1, 4096), Tooltip("Step range for flythrough ray marching, specified as (min, max) in world units. Reducing the first value can be helpful for reducing artifacts during flythrough, but can also be more expensive.")]
    public Vector2 m_flythroughStepRange = new Vector2(8, 512);
    [MinMaxSlider(0, 200000), Tooltip("Distance range over which to apply the flythrough step range.")]
    public Vector2 m_flythroughStepDistanceRange = new Vector2(0, 50000);
    [Range(0, 1), Tooltip("Threshold below which normalized cloud density is considered to be zero.")]
    public float m_mediaZeroThreshold = 0.0001f;
    [Range(0, 1), Tooltip("Threshold below which cloud transmittance is considered to be zero.")]
    public float m_transmittanceZeroThreshold = 0.0001f;
    [Min(0), Tooltip("Max number of consecutive zero samples before detail ray marching switches back to coarse ray marching.")]
    public int m_maxConsecutiveZeroSamples = 8;
    [Tooltip("Whether or not to use TAA-based denoising to allow fewer samples to be taken per frame.")]
    public bool m_useTemporalDenoising = false;
    [Range(1, 64), Tooltip("How many history frames to use for TAA-based denoising. Fewer frames will result in less blurring but more noise. More frames will reduce noise but introduce blurring.")]
    public int m_denoisingHistoryFrames = 8;
    [Tooltip("Whether or not to apply certain sampling techniques that will reduce reprojection artifacts for cloud flythrough.")]
    public bool m_optimizeForFlythrough = false;
    [Range(0, 1), Tooltip("Transmittance threshold for which to \"disable\" reprojection, to help with flythrough artifacts.")]
    public float m_flythroughThreshold = 0.95f;

#if UNITY_EDITOR
    /* Layer dropdown select---only included in UI. */
    [Tooltip("Which noise layer to display for editing.")]
    public CloudDatatypes.CloudNoiseLayer m_layerSelect = CloudDatatypes.CloudNoiseLayer.Base;
#endif // UNITY_EDITOR

    // Update is called once per frame
    void Update()
    {
        m_universal.renderSettings.geometryType = (int) (m_curved ? CloudDatatypes.CloudGeometryType.CurvedBoxVolume : CloudDatatypes.CloudGeometryType.BoxVolume);
        m_universal.renderSettings.geometryXExtent = new Vector2(m_origin.x - m_XExtent * 0.5f, m_origin.x + m_XExtent * 0.5f);
        m_universal.renderSettings.geometryYExtent = new Vector2(m_origin.y - m_YExtent * 0.5f, m_origin.y + m_YExtent * 0.5f);
        m_universal.renderSettings.geometryZExtent = new Vector2(m_origin.z - m_ZExtent * 0.5f, m_origin.z + m_ZExtent * 0.5f);
        m_universal.renderSettings.attenuationDistance = m_attenuationDistance;
        m_universal.renderSettings.attenuationBias = m_attenuationBias;
        m_universal.renderSettings.rampUp = m_rampUp;
        m_universal.renderSettings.coverageIntensity = m_coverageIntensity;
        m_universal.renderSettings.structureIntensity = m_structureIntensity;
        m_universal.renderSettings.structureMultiply = m_structureMultiply;
        m_universal.renderSettings.detailIntensity = m_detailIntensity;
        m_universal.renderSettings.detailMultiply = m_detailMultiply;
        m_universal.renderSettings.baseWarpIntensity = m_baseWarpIntensity;
        m_universal.renderSettings.detailWarpIntensity = m_detailWarpIntensity;
        m_universal.renderSettings.rounding = m_rounding;
        m_universal.renderSettings.roundingShape = m_roundingShape;
        m_universal.renderSettings.heightGradientBottom = m_heightGradientBottom;
        m_universal.renderSettings.heightGradientTop = m_heightGradientTop;
        m_universal.renderSettings.windSkew = m_windSkew;

        m_universal.noiseTextureQuality = m_noiseTextureQuality;
        for (int i = 0; i < m_noiseLayers.Length; i++) {
            m_universal.noiseLayers[i].procedural = m_noiseLayers[i].procedural;
            if (m_noiseLayers[i].textureGenerator != null && m_noiseLayers[i].textureGenerator.GetTexture() != null) {
                m_noiseLayers[i].texture = m_noiseLayers[i].textureGenerator.GetTexture();
            }
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

        m_universal.renderSettings.density = m_density;
        m_universal.renderSettings.extinctionCoefficients = ((Vector4) m_extinctionCoefficients).xyz();
        m_universal.renderSettings.scatteringCoefficients = ((Vector4) m_scatteringCoefficients).xyz();
        m_universal.renderSettings.anisotropy = m_anisotropy;
        m_universal.renderSettings.silverIntensity = m_silverIntensity;
        m_universal.renderSettings.silverSpread = m_silverSpread;
        m_universal.renderSettings.ambientHeightRange = m_ambientHeightRange;
        m_universal.renderSettings.ambientStrengthRange = m_ambientStrengthRange;
        m_universal.renderSettings.verticalProbabilityHeightRange = m_verticalProbabilityHeightRange;
        m_universal.renderSettings.verticalProbabilityStrength = m_verticalProbabilityStrength;
        m_universal.renderSettings.depthProbabilityHeightRange = m_depthProbabilityHeightRange;
        m_universal.renderSettings.depthProbabilityStrengthRange = m_depthProbabilityStrengthRange;
        m_universal.renderSettings.depthProbabilityDensityMultiplier = m_depthProbabilityDensityMultiplier;
        m_universal.renderSettings.depthProbabilityBias = m_depthProbabilityBias;
        // Here 0 => x => LODded density sample, and 2 => z => full detail density sample. Oddly enough, the
        // perceived detail, is the other way around---using the LODded sample produces a higher-detail result.
        m_universal.renderSettings.depthProbabilityDetailIndex = m_depthProbabilityHighDetail ? 0 : 2;
        m_universal.renderSettings.selfShadowing = m_selfShadowing ? 1 : 0;
        m_universal.renderSettings.multipleScatteringAmount = m_multipleScatteringAmount;
        m_universal.renderSettings.multipleScatteringBias = m_multipleScatteringBias;
        m_universal.renderSettings.multipleScatteringRampDown = m_multipleScatteringRampDown;
        m_universal.renderSettings.multipleScatteringRampDownShape = m_multipleScatteringRampDownShape;
        m_universal.renderSettings.castShadows = m_castShadows ? 1 : 0;
        m_universal.renderSettings.maxShadowIntensity = Mathf.Sqrt(m_maxShadowIntensity);
        m_universal.renderSettings.maxSelfShadowDistance = m_maxSelfShadowDistance;
        m_universal.renderSettings.lightPollutionDimmer = m_lightPollutionDimmer;
        m_universal.renderSettings.celShade = m_celShade ? 1 : 0;
        m_universal.renderSettings.celShadeLightingBands = m_celShadeColorBands;
        m_universal.renderSettings.celShadeTransmittanceBands = m_celShadeTransmittanceBands;

        if (!m_useOffset) {
            m_coverageOffset += Time.deltaTime * m_coverageVelocity;
            m_baseOffset += Time.deltaTime * m_baseVelocity;
            m_structureOffset += Time.deltaTime * m_structureVelocity;
            m_detailOffset += Time.deltaTime * m_detailVelocity;
            m_baseWarpOffset += Time.deltaTime * m_baseWarpVelocity;
            m_detailWarpOffset += Time.deltaTime * m_detailWarpVelocity;
        }
        m_universal.renderSettings.coverageOffset = new Vector3(m_coverageOffset.x, 0, m_coverageOffset.y);
        m_universal.renderSettings.baseOffset = new Vector3(m_baseOffset.x, 0, m_baseOffset.y);
        m_universal.renderSettings.structureOffset = new Vector3(m_structureOffset.x, 0, m_structureOffset.y);
        m_universal.renderSettings.detailOffset = new Vector3(m_detailOffset.x, 0, m_detailOffset.y);
        m_universal.renderSettings.baseWarpOffset = new Vector3(m_baseWarpOffset.x, 0, m_baseWarpOffset.y);
        m_universal.renderSettings.detailWarpOffset = new Vector3(m_detailWarpOffset.x, 0, m_detailWarpOffset.y);

        m_universal.renderSettings.reprojectionFrames = m_reprojectionFrames;
        m_universal.renderSettings.coarseStepRange = m_coarseStepRange;
        m_universal.renderSettings.detailStepRange = m_detailStepRange;
        m_universal.renderSettings.stepDistanceRange = m_stepDistanceRange;
        m_universal.renderSettings.flythroughStepRange = m_flythroughStepRange;
        m_universal.renderSettings.flythroughStepDistanceRange = m_flythroughStepDistanceRange;
        m_universal.renderSettings.mediaZeroThreshold = m_mediaZeroThreshold;
        m_universal.renderSettings.transmittanceZeroThreshold = m_transmittanceZeroThreshold;
        m_universal.renderSettings.useTemporalDenoising = m_useTemporalDenoising ? 1 : 0;
        m_universal.renderSettings.temporalDenoisingRatio = 1.0f / (float) m_denoisingHistoryFrames;
    }
    
    public override UniversalCloudLayer ToUniversal() {
        return m_universal;
    }

    public override void FromUniversal(UniversalCloudLayer from, bool bypassOffset=false) {
        m_curved = (from.renderSettings.geometryType == (int) CloudDatatypes.CloudGeometryType.CurvedBoxVolume);
        m_origin.x = 0.5f * (from.renderSettings.geometryXExtent.y + from.renderSettings.geometryXExtent.x);
        m_origin.y = 0.5f * (from.renderSettings.geometryYExtent.y + from.renderSettings.geometryYExtent.x);
        m_origin.z = 0.5f * (from.renderSettings.geometryZExtent.y + from.renderSettings.geometryZExtent.x);
        m_XExtent = from.renderSettings.geometryXExtent.y - from.renderSettings.geometryXExtent.x;
        m_YExtent = from.renderSettings.geometryYExtent.y - from.renderSettings.geometryYExtent.x;
        m_ZExtent = from.renderSettings.geometryZExtent.y - from.renderSettings.geometryZExtent.x;
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
        m_rounding = from.renderSettings.rounding;
        m_roundingShape = from.renderSettings.roundingShape;
        m_heightGradientBottom = from.renderSettings.heightGradientBottom;
        m_heightGradientTop = from.renderSettings.heightGradientTop;
        m_windSkew = from.renderSettings.windSkew;

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

        m_density = from.renderSettings.density;
        m_extinctionCoefficients = new Color(from.renderSettings.extinctionCoefficients.x, from.renderSettings.extinctionCoefficients.y, from.renderSettings.extinctionCoefficients.z);
        m_scatteringCoefficients = new Color(from.renderSettings.scatteringCoefficients.x, from.renderSettings.scatteringCoefficients.y, from.renderSettings.scatteringCoefficients.z);
        m_anisotropy = from.renderSettings.anisotropy;
        m_silverIntensity = from.renderSettings.silverIntensity;
        m_silverSpread = from.renderSettings.silverSpread;
        m_ambientHeightRange = from.renderSettings.ambientHeightRange;
        m_ambientStrengthRange = from.renderSettings.ambientStrengthRange;
        m_verticalProbabilityHeightRange = from.renderSettings.verticalProbabilityHeightRange;
        m_verticalProbabilityStrength = from.renderSettings.verticalProbabilityStrength;
        m_depthProbabilityHeightRange = from.renderSettings.depthProbabilityHeightRange;
        m_depthProbabilityStrengthRange = from.renderSettings.depthProbabilityStrengthRange;
        m_depthProbabilityDensityMultiplier = from.renderSettings.depthProbabilityDensityMultiplier;
        m_depthProbabilityBias = from.renderSettings.depthProbabilityBias;
        // Here 0 => x => LODded density sample, and 2 => z => full detail density sample. Oddly enough, the
        // perceived detail, is the other way around---using the LODded sample produces a higher-detail result.
        m_depthProbabilityHighDetail = from.renderSettings.depthProbabilityDetailIndex == 0;
        m_selfShadowing = from.renderSettings.selfShadowing == 1;
        m_multipleScatteringAmount = from.renderSettings.multipleScatteringAmount;
        m_multipleScatteringBias = from.renderSettings.multipleScatteringBias;
        m_multipleScatteringRampDown = from.renderSettings.multipleScatteringRampDown;
        m_multipleScatteringRampDownShape = from.renderSettings.multipleScatteringRampDownShape;
        m_castShadows = from.renderSettings.castShadows == 1;
        m_maxShadowIntensity = from.renderSettings.maxShadowIntensity * from.renderSettings.maxShadowIntensity;
        m_maxSelfShadowDistance = from.renderSettings.maxSelfShadowDistance;
        m_lightPollutionDimmer = from.renderSettings.lightPollutionDimmer;
        m_celShade = from.renderSettings.celShade == 1;
        m_celShadeColorBands = from.renderSettings.celShadeLightingBands;
        m_celShadeTransmittanceBands = from.renderSettings.celShadeTransmittanceBands;

        if (!bypassOffset) {
            m_coverageOffset.x = from.renderSettings.coverageOffset.x;
            m_coverageOffset.y = from.renderSettings.coverageOffset.z;
            m_baseOffset.x = from.renderSettings.baseOffset.x;
            m_baseOffset.y = from.renderSettings.baseOffset.z;
            m_structureOffset.x = from.renderSettings.structureOffset.x;
            m_structureOffset.y = from.renderSettings.structureOffset.z;
            m_detailOffset.x = from.renderSettings.detailOffset.x;
            m_detailOffset.y = from.renderSettings.detailOffset.z;
            m_baseWarpOffset.x = from.renderSettings.baseWarpOffset.x;
            m_baseWarpOffset.y = from.renderSettings.baseWarpOffset.z;
            m_detailWarpOffset.x = from.renderSettings.detailWarpOffset.x;
            m_detailWarpOffset.y = from.renderSettings.detailWarpOffset.z;
        }

        m_reprojectionFrames = Math.Max(1, Math.Min(4, from.renderSettings.reprojectionFrames));
        m_coarseStepRange = from.renderSettings.coarseStepRange;
        m_detailStepRange = from.renderSettings.detailStepRange;
        m_stepDistanceRange = from.renderSettings.stepDistanceRange;
        m_flythroughStepRange = from.renderSettings.flythroughStepRange;
        m_flythroughStepDistanceRange = from.renderSettings.flythroughStepDistanceRange;
        m_mediaZeroThreshold = from.renderSettings.mediaZeroThreshold;
        m_transmittanceZeroThreshold = from.renderSettings.transmittanceZeroThreshold;
        m_useTemporalDenoising = from.renderSettings.useTemporalDenoising == 1;
        m_denoisingHistoryFrames = (int) (1.0f / from.renderSettings.temporalDenoisingRatio);
    }

    public override bool SetTexture(CloudDatatypes.CloudNoiseLayer noiseLayer, Texture texture, int tile) {
        if (texture == null) {
            return false;
        }
        if ((noiseLayer == CloudDatatypes.CloudNoiseLayer.Coverage && texture.dimension != UnityEngine.Rendering.TextureDimension.Tex2D)
            || (noiseLayer != CloudDatatypes.CloudNoiseLayer.Coverage && texture.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)) {
            return false;
        }
        m_noiseLayers[(int) noiseLayer].procedural = false;
        m_noiseLayers[(int) noiseLayer].texture = texture;
        m_noiseLayers[(int) noiseLayer].tile = tile;
        return true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralCloudVolumeBlock))]
public class ProceduralCloudVolumeBlockEditor : Editor
{
    bool m_modelingFoldout = false;
    bool m_noiseEditorFoldout = false;
    bool m_lightingFoldout = false;
    bool m_movementFoldout = false;
    bool m_qualityFoldout = false;

    // Internal class to help draw layers.
    [CustomPropertyDrawer(typeof(ProceduralCloudVolumeBlock.LayerSettings))]
    private class ProceduralCloudVolumeBlockLayerDrawer : PropertyDrawer
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
                EditorGUILayout.PropertyField(property.FindPropertyRelative("textureGenerator"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("tile"));
            }
        }
    }

    override public void OnInspectorGUI()
    {
        serializedObject.Update();
        
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

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_name"));
        
        m_modelingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_modelingFoldout, "Modeling");
        if (m_modelingFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_curved"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_origin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_XExtent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_YExtent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ZExtent"));
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rounding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_roundingShape"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_heightGradientBottom"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_heightGradientTop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_windSkew"));
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_extinctionCoefficients"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scatteringCoefficients"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_anisotropy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_silverIntensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_silverSpread"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ambientHeightRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ambientStrengthRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_verticalProbabilityHeightRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_verticalProbabilityStrength"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_depthProbabilityHeightRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_depthProbabilityStrengthRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_depthProbabilityDensityMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_depthProbabilityBias"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_depthProbabilityHighDetail"));
            SerializedProperty selfShadow = serializedObject.FindProperty("m_selfShadowing");
            EditorGUILayout.PropertyField(selfShadow);
            if (selfShadow.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_maxSelfShadowDistance"));
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpOffset"));
            } else {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverageVelocity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseVelocity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_structureVelocity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailVelocity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_baseWarpVelocity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailWarpVelocity"));
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        m_qualityFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_qualityFoldout, "Quality");
        if (m_qualityFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_reprojectionFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coarseStepRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_detailStepRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_stepDistanceRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_flythroughStepRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_flythroughStepDistanceRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_mediaZeroThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_transmittanceZeroThreshold"));
            
            SerializedProperty useDenoising = serializedObject.FindProperty("m_useTemporalDenoising");
            EditorGUILayout.PropertyField(useDenoising);
            if (useDenoising.boolValue) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_denoisingHistoryFrames"));
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif // UNITY_EDITOR

} // namespace Expanse
