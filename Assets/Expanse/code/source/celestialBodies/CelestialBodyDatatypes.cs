using System;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: class containing common datatypes used in the implementation of
 * celestial bodies.
 * */
[GenerateHLSL]
public class CelestialBodyDatatypes {
  /* Enum for celestial bodies. Currently we support up to 8 different
   * celestial bodies. */
  [GenerateHLSL]
  public enum CelestialBody {
    Body0 = 0,
    Body1,
    Body2,
    Body3,
    Body4,
    Body5,
    Body6,
    Body7
  };
  public const uint kMaxCelestialBodies = 8;
}

} // namespace Expanse
