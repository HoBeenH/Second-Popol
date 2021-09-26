using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: helper class containing all the settings for a cloud noise layer.
 * */
[Serializable]
public class CloudNoiseLayerSettings {
  [Tooltip("Whether to use procedural noise or a texture this noise layer.")]
  public BoolParameter procedural = new BoolParameter(true);
  [Tooltip("Noise texture for this layer.")]
  public TextureParameter noiseTexture2D = new TextureParameter(null);
  [Tooltip("Noise texture for this layer.")]
  public TextureParameter noiseTexture3D = new TextureParameter(null);
  [Tooltip("Noise type for this layer.")]
  public EnumParameter<Datatypes.NoiseType> noiseType = new EnumParameter<Datatypes.NoiseType>(Datatypes.NoiseType.Perlin);
  [Tooltip("Scale of 0th octave.")]
  public Vector2Parameter scale = new Vector2Parameter(new Vector2(16, 16));
  [Tooltip("Number of octaves.")]
  public ClampedIntParameter octaves = new ClampedIntParameter(4, 1, 8);
  [Tooltip("How much to scale each successive octave by.")]
  public MinFloatParameter octaveScale = new MinFloatParameter(2, 0);
  [Tooltip("How much to multiply the intensity of each successive octave by.")]
  public MinFloatParameter octaveMultiplier = new MinFloatParameter(0.5f, 0);
  [Tooltip("Tile factor.")]
  public MinIntParameter tile = new MinIntParameter(1, 1);

  public override int GetHashCode() {
    int hash = base.GetHashCode();
    unchecked {
      hash = hash * 23 + procedural.value.GetHashCode();
      if (procedural.value) {
        hash = hash * 23 + noiseType.value.GetHashCode();
        hash = hash * 23 + scale.value.GetHashCode();
        hash = hash * 23 + octaves.value.GetHashCode();
        hash = hash * 23 + octaveScale.value.GetHashCode();
        hash = hash * 23 + octaveMultiplier.value.GetHashCode();
        hash = hash * 23 + tile.value.GetHashCode();
      } else {
        hash = (noiseTexture2D.value == null) ? hash : hash * 23 + noiseTexture2D.value.GetHashCode();
        hash = (noiseTexture3D.value == null) ? hash : hash * 23 + noiseTexture3D.value.GetHashCode();
      }
    }
    return hash;
  }
}

/**
 * @brief: class containing all the common settings for a cloud layer.
 * */
[Serializable]
public class CloudLayerSettings {
  /* General. */
  [Tooltip("Whether or not this cloud layer is enabled.")]
  public BoolParameter enabled = new BoolParameter(false);

  /* Geometry. */
  [Tooltip("Type of geometry for this cloud layer.")]
  public EnumParameter<CloudDatatypes.CloudGeometryType> geometryType = new EnumParameter<CloudDatatypes.CloudGeometryType>(CloudDatatypes.CloudGeometryType.CurvedPlane);
  [Tooltip("X extent of this cloud layer's geometry.")]
  public Vector2Parameter geometryXExtent = new Vector2Parameter(new Vector2(-100000, 100000));
  [Tooltip("Y extent of this cloud layer's geometry.")]
  public Vector2Parameter geometryYExtent = new Vector2Parameter(new Vector2(2000, 4000));
  [Tooltip("Z extent of this cloud layer's geometry.")]
  public Vector2Parameter geometryZExtent = new Vector2Parameter(new Vector2(-100000, 100000));
  [Tooltip("Height of this cloud layer's geometry.")]
  public FloatParameter geometryHeight = new FloatParameter(12000);

  /* Noise. */
  [Tooltip("Quality of procedural noises for this layer. If no procedural noises are enabled, this parameter does not change anything.")]
  public EnumParameter<Datatypes.Quality> noiseTextureQuality = new EnumParameter<Datatypes.Quality>(Datatypes.Quality.Medium);
  [Tooltip("How much the coverage noise erodes the base noise.")]
  public ClampedFloatParameter coverageIntensity = new ClampedFloatParameter(0.5f, 0, 1);
  [Tooltip("How much the structure noise erodes the base noise.")]
  public ClampedFloatParameter structureIntensity = new ClampedFloatParameter(0.4f, 0, 1);
  [Tooltip("How much the detail noise erodes the base noise.")]
  public ClampedFloatParameter detailIntensity = new ClampedFloatParameter(0.1f, 0, 1);
  [Tooltip("How much the base noise is warped by its warp texture.")]
  public ClampedFloatParameter baseWarpIntensity = new ClampedFloatParameter(0.05f, 0, 1);
  [Tooltip("How much the detail noise is warped by its warp texture.")]
  public ClampedFloatParameter detailWarpIntensity = new ClampedFloatParameter(0.01f, 0, 1);
  [Tooltip("Bottom of the height gradient.")]
  public FloatRangeParameter heightGradientBottom = new FloatRangeParameter(new Vector2(0.05f, 0.1f), 0.0f, 1.0f);
  [Tooltip("Top of the height gradient.")]
  public FloatRangeParameter heightGradientTop = new FloatRangeParameter(new Vector2(0.5f, 1.0f), 0.0f, 1.0f);
  [Tooltip("Controls how much the clouds skew over height, to mimick the effect of the tops of clouds rolling over themselves in the wind. Order is (skew x, skew z).")]
  public Vector2Parameter windSkew = new Vector2Parameter(new Vector2(0, 0));
  [Tooltip("Controls how round the tops of clouds are, as opposed to blocky.")]
  public MinFloatParameter rounding = new MinFloatParameter(2, 1);
  [Tooltip("Controls the shape of the rounding effect.")]
  public MinFloatParameter roundingShape = new MinFloatParameter(2, 1);
  public ObjectParameter<CloudNoiseLayerSettings> coverageNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());
  public ObjectParameter<CloudNoiseLayerSettings> baseNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());
  public ObjectParameter<CloudNoiseLayerSettings> structureNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());
  public ObjectParameter<CloudNoiseLayerSettings> detailNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());
  public ObjectParameter<CloudNoiseLayerSettings> baseWarpNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());
  public ObjectParameter<CloudNoiseLayerSettings> detailWarpNoiseLayer = new ObjectParameter<CloudNoiseLayerSettings>(new CloudNoiseLayerSettings());

  /* Movement. */
  [Tooltip("Sampling offsets for coverage map.")]
  public Vector3Parameter coverageOffset = new Vector3Parameter(new Vector3(0, 0, 0));
  [Tooltip("Sampling offsets for base noise.")]
  public Vector3Parameter baseOffset = new Vector3Parameter(new Vector3(0, 0, 0));
  [Tooltip("Sampling offsets for coverage map.")]
  public Vector3Parameter structureOffset = new Vector3Parameter(new Vector3(0, 0, 0));
  [Tooltip("Sampling offsets for coverage map.")]
  public Vector3Parameter detailOffset = new Vector3Parameter(new Vector3(0, 0, 0));
  [Tooltip("Sampling offsets for coverage map.")]
  public Vector3Parameter baseWarpOffset = new Vector3Parameter(new Vector3(0, 0, 0));
  [Tooltip("Sampling offsets for coverage map.")]
  public Vector3Parameter detailWarpOffset = new Vector3Parameter(new Vector3(0, 0, 0));

  /* Lighting. */
  /* 2D and 3D. */
  [Tooltip("Density of this cloud layer.")]
  public MinFloatParameter density = new MinFloatParameter(250, 0);
  [Tooltip("Density attenuation distance for this cloud layer.")]
  public MinFloatParameter attenuationDistance = new MinFloatParameter(25000, 0);
  [Tooltip("Density attenuation bias for this cloud layer.")]
  public MinFloatParameter attenuationBias = new MinFloatParameter(25000, 0);
  [Tooltip("Beginning and ending distance for density rampup.")]
  public FloatRangeParameter rampUp = new FloatRangeParameter(new Vector2(0, 0), 0, 200000);
  [Tooltip("Extinction coefficients for this cloud layer.")]
  public Vector3Parameter extinctionCoefficients = new Vector3Parameter(new Vector3(4e-6f, 4e-6f, 4e-6f));
  [Tooltip("Scattering coefficients for this cloud layer.")]
  public Vector3Parameter scatteringCoefficients = new Vector3Parameter(new Vector3(4e-6f, 4e-6f, 4e-6f));
  [Tooltip("Amount of approximated multiple scattering.")]
  public ClampedFloatParameter multipleScatteringAmount = new ClampedFloatParameter(0.7f, 0, 1);
  [Tooltip("Bias to approximated multiple scattering.")]
  public ClampedFloatParameter multipleScatteringBias = new ClampedFloatParameter(0.25f, 0, 1);
  [Tooltip("How much to ramp down multiple scattering as samples approach the light. This is useful for making sure that denser clouds block enough light when the sun is behind them.")]
  public ClampedFloatParameter multipleScatteringRampDown = new ClampedFloatParameter(0.33f, 0, 1);
  [Tooltip("How sharply the multiple scattering ramps down as samples approach the light.")]
  public MinFloatParameter multipleScatteringRampDownShape = new MinFloatParameter(7, 0);
  [Tooltip("Spread of cloud silver lining.")]
  public ClampedFloatParameter silverSpread = new ClampedFloatParameter(0.5f, 0, 1);
  [Tooltip("Intensity of cloud silver lining.")]
  public ClampedFloatParameter silverIntensity = new ClampedFloatParameter(0.5f, 0, 1);
  [Tooltip("Anistropy of cloud scattering.")]
  public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.1f, -1, 1);
  [Tooltip("Ambient lighting the clouds receive from the sky. Expanse doesn't compute self-shadowing of ambient light, so this can help to lower the ambient light contribution to a level that looks more physically correct.")]
  public MinFloatParameter ambient = new MinFloatParameter(1, 0);
  [Tooltip("Height range of ambient lighting the clouds receive from the sky. The bottom of clouds generally receives less ambient light than the top. Tweaking this can help reveal self-shadowed details.")]
  public FloatRangeParameter ambientHeightRange = new FloatRangeParameter(new Vector2(0, 1), 0, 1);
  [Tooltip("Strength range of ambient lighting the clouds receive from the sky, applied over the specified height range.")]
  public FloatRangeParameter ambientStrengthRange = new FloatRangeParameter(new Vector2(0.5f, 1), 0, 5);
  [Tooltip("Whether to use cel/\"toon\" shading. Good for stylized clouds.")]
  public BoolParameter celShade = new BoolParameter(false);
  [Tooltip("Band for lighting.")]
  public MinFloatParameter celShadeLightingBands = new MinFloatParameter(1500, 0);
  [Tooltip("Band for transmittance.")]
  public ClampedFloatParameter celShadeTransmittanceBands = new ClampedFloatParameter(0.5f, 0, 1);
  
  /* 2D. */
  [Tooltip("Apparent thickness of this 2D cloud layer.")]
  public MinFloatParameter apparentThickness = new MinFloatParameter(250, 0);
  /* 3D. */
  [Tooltip("Unit height range to apply vertical in-scattering probability to.")]
  public FloatRangeParameter verticalProbabilityHeightRange = new FloatRangeParameter(new Vector2(0.05f, 0.2f), 0, 1);
  [Tooltip("Strength of vertical in-scattering probablity.")]
  public MinFloatParameter verticalProbabilityStrength = new MinFloatParameter(0.8f, 0);
  [Tooltip("Unit height range to apply depth in-scattering probability to.")]
  public FloatRangeParameter depthProbabilityHeightRange = new FloatRangeParameter(new Vector2(0.05f, 0.75f), 0, 1);
  [Tooltip("Strength range of depth in-scattering probability, from min height to max height.")]
  public FloatRangeParameter depthProbabilityStrengthRange = new FloatRangeParameter(new Vector2(0.5f, 2), 0, 5);
  [Tooltip("Pre-multiplier on density for depth in-scattering probability. Can be useful for bringing the density into a range where the effect is noticeable.")]
  public MinFloatParameter depthProbabilityDensityMultiplier = new MinFloatParameter(10, 0);
  [Tooltip("Bias to depth in-scattering probability.")]
  public ClampedFloatParameter depthProbabilityBias = new ClampedFloatParameter(0.1f, 0, 1);
  [Tooltip("Whether or not to use a higher detail version of depth probability. Off by default to accommodate previous presets.")]
  public BoolParameter depthProbabilityHighDetail = new BoolParameter(false);
  [Tooltip("Amount the light pollution affects the clouds. Useful for when light pollution is being used primarily as an artistic effect for the sky.")]
  public ClampedFloatParameter lightPollutionDimmer = new ClampedFloatParameter(1.0f, 0, 1);

  /* Sampling And Quality. */
  [Tooltip("Whether or not this layer casts shadows on the ground and geometry.")]
  public BoolParameter castShadows = new BoolParameter(false);
  [Tooltip("How dark the shadows cast by this cloud layer can be.")]
  public ClampedFloatParameter maxShadowIntensity = new ClampedFloatParameter(1.0f, 0, 1);
  [Tooltip("Whether or not the clouds cast shadows on themselves.")]
  public BoolParameter selfShadowing = new BoolParameter(false);
  [Tooltip("Determines the quality of the clouds' self shadowing. Note that high quality doesn't always look better---it just uses a higher level of detail when sampling the shadows. It needs to be used carefully in conjunction with the multiple scattering parameters to create a desirable look.")]
  public BoolParameter highQualityShadows = new BoolParameter(false);
  [Tooltip("Max distance that clouds can cast shadows onto themselves/each other.")]
  public ClampedFloatParameter maxSelfShadowDistance = new ClampedFloatParameter(1000, 1, 10000);
  [Tooltip("Step number range for coarse ray marching.")]
  public FloatRangeParameter coarseStepRange = new FloatRangeParameter(new Vector2(16, 32), 1, 256);
  [Tooltip("Step number range for detail ray marching.")]
  public FloatRangeParameter detailStepRange = new FloatRangeParameter(new Vector2(64, 96), 1, 256);
  [Tooltip("Distance range over which to apply the step number ranges.")]
  public FloatRangeParameter stepDistanceRange = new FloatRangeParameter(new Vector2(125, 20000), 50, 200000);
  [Tooltip("Min and max step distance for flythrough ray marching.")]
  public FloatRangeParameter flythroughStepRange = new FloatRangeParameter(new Vector2(8, 512), 1, 4096);
  [Tooltip("Distance range over which to apply the step number ranges.")]
  public FloatRangeParameter flythroughStepDistanceRange = new FloatRangeParameter(new Vector2(0, 50000), 0, 200000);
  [Tooltip("Threshold below which normalized cloud density is considered to be zero.")]
  public ClampedFloatParameter mediaZeroThreshold = new ClampedFloatParameter(0.0001f, 0, 1);
  [Tooltip("Threshold below which cloud transmittance is considered to be zero.")]
  public ClampedFloatParameter transmittanceZeroThreshold = new ClampedFloatParameter(0.0001f, 0, 1);
  [Tooltip("Max number of consecutive zero samples before detail ray marching switches back to coarse ray marching.")]
  public MinIntParameter maxConsecutiveZeroSamples = new MinIntParameter(8, 0);
  [Tooltip("Number of history frames to use for reprojection.")]
  public ClampedIntParameter reprojectionFrames = new ClampedIntParameter(4, 1, 4);
  [Tooltip("Whether or not to use TAA-based denoising to allow fewer samples to be taken per frame.")]
  public BoolParameter useTemporalDenoising = new BoolParameter(false);
  [Tooltip("How many history frames to use for TAA-based denoising. Fewer frames will result in less blurring but more noise. More frames will reduce noise but introduce blurring.")]
  public ClampedIntParameter denoisingHistoryFrames = new ClampedIntParameter(8, 1, 64);

  /* This only takes into account parameters that require a regeneration
   * of the cloud noises. */
  public override int GetHashCode() {
    int hash = base.GetHashCode();
    unchecked {
      hash = hash * 23 + enabled.value.GetHashCode();
      if (enabled.value) {
        hash = hash * 23 + noiseTextureQuality.value.GetHashCode();
        hash = hash * 23 + coverageNoiseLayer.value.GetHashCode();
        hash = hash * 23 + baseNoiseLayer.value.GetHashCode();
        hash = hash * 23 + structureNoiseLayer.value.GetHashCode();
        hash = hash * 23 + detailNoiseLayer.value.GetHashCode();
        hash = hash * 23 + baseWarpNoiseLayer.value.GetHashCode();
        hash = hash * 23 + detailWarpNoiseLayer.value.GetHashCode();
      }
    }
    return hash;
  }
}

} // namespace Expanse
