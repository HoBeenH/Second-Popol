using System;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: class containing common datatypes used in the implementation of
 * atmosphere layers.
 * */
[GenerateHLSL]
public class AtmosphereDatatypes {
  /* Enum for atmosphere layers. Currently we support up to 8 different
   * atmosphere layers. */
  [GenerateHLSL]
  public enum AtmosphereLayer {
    Layer0 = 0,
    Layer1,
    Layer2,
    Layer3,
    Layer4,
    Layer5,
    Layer6,
    Layer7
  };
  public const uint kMaxAtmosphereLayers = 8;

  /* Enum for phase function types. */
  [GenerateHLSL]
  public enum PhaseFunction {
    Isotropic = 0,
    Rayleigh,
    Mie
  };
  public const uint kNumPhaseFunctions = 3;

  /* Enum for atmosphere layer density distribution types. */
  [GenerateHLSL]
  public enum DensityDistribution {
    Exponential = 0,
    Tent,
    ScreenspaceUniform,
    ScreenspaceHeightFog
  };
  public const uint kNumDensityDistributions = 4;

  public static bool integrateInScreenspace(DensityDistribution d) {
    return d == DensityDistribution.ScreenspaceUniform || d == DensityDistribution.ScreenspaceHeightFog;
  }
}

} // namespace Expanse
