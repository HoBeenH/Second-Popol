using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: class containing common datatypes used across Expanse's
 * implementation.
 * */
[GenerateHLSL]
public class Datatypes {
  /* Enum for specifying any sort of quality level. Generally, used as a
   * high-level wrapper around resolutions or sampling behaviors. */
  [GenerateHLSL]
  public enum Quality {
    Potato = 0,
    Low,
    Medium,
    High,
    Ultra,
    RippingThroughTheMetaverse
  }
  public const uint kMaxQuality = 6;

  /* Enum for specifying a particular type of noise. */
  [GenerateHLSL]
  public enum NoiseType {
    Constant = 0,
    Value,
    Perlin,
    Worley,
    InverseWorley,
    PerlinWorley,
    PerlinWorley2,
    Curl
  }
  public const uint kNumNoiseTypes = 8;
  private static Dictionary<NoiseType, string> cloudNoiseTypeToKernelName = new Dictionary<NoiseType, string>(){
  	{NoiseType.Constant, "CONSTANT"},
  	{NoiseType.Value, "VALUE"},
  	{NoiseType.Perlin, "PERLIN"},
    {NoiseType.Worley, "WORLEY"},
    {NoiseType.InverseWorley, "INVERSEWORLEY"},
    {NoiseType.PerlinWorley, "PERLINWORLEY"},
    {NoiseType.PerlinWorley2, "PERLINWORLEY2"},
    {NoiseType.Curl, "CURL"}
  };
  public static string noiseTypeToKernelName(NoiseType type) {
    return cloudNoiseTypeToKernelName[type];
  }

  /* Enum for specifying dimension of noise. */
  [GenerateHLSL]
  public enum NoiseDimension {
    TwoDimensional = 0,
    ThreeDimensional
  }
  public const uint kNumNoiseDimensions = 2;
}

} // namespace Expanse
