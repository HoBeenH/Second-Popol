using System;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: class containing common datatypes used in the implementation of
 * nebulae.
 * */
[GenerateHLSL]
public class NebulaDatatypes {
  /* Enum for nebula layers. Currently we support up to 4 different
   * nebula layers. */
  [GenerateHLSL]
  public enum NebulaLayer {
    Layer0 = 0,
    Layer1,
    Layer2,
    Layer3
  };
  public const uint kMaxNebulaLayers = 4;
}

} // namespace Expanse
