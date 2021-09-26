using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace Expanse {

/**
 * @brief: class containing common datatypes used in the implementation of
 * clouds.
 * */
[GenerateHLSL]
public class CloudDatatypes {
  /* Enum for cloud layers. Currently we support up to 8 different
   * cloud layers. */
  [GenerateHLSL]
  public enum CloudLayer {
    Layer0 = 0,
    Layer1,
    Layer2,
    Layer3,
    Layer4,
    Layer5,
    Layer6,
    Layer7
  };
  public const uint kMaxCloudLayers = 8;

  /* Enum for specifying the noise layers that make up a full cloud layer. */
  [GenerateHLSL]
  public enum CloudNoiseLayer {
    Coverage = 0,
    Base,
    Structure,
    Detail,
    BaseWarp,
    DetailWarp
  };
  public const uint kNumCloudNoiseLayers = 6;
  /* Precomputed shader variables to avoid string ops that generate garbage. */
  private static Dictionary<CloudNoiseLayer, int>
    cloudNoiseLayerToShaderVariable2D =
      new Dictionary<CloudNoiseLayer, int>() {
      {CloudNoiseLayer.Coverage, Shader.PropertyToID("_CloudCoverage2D")},
      {CloudNoiseLayer.Base, Shader.PropertyToID("_CloudBase2D")},
      {CloudNoiseLayer.Structure, Shader.PropertyToID("_CloudStructure2D")},
      {CloudNoiseLayer.Detail, Shader.PropertyToID("_CloudDetail2D")},
      {CloudNoiseLayer.BaseWarp, Shader.PropertyToID("_CloudBaseWarp2D")},
      {CloudNoiseLayer.DetailWarp, Shader.PropertyToID("_CloudDetailWarp2D")}
  };
  private static Dictionary<CloudNoiseLayer, int>
    cloudNoiseLayerToShaderVariable3D =
      new Dictionary<CloudNoiseLayer, int>() {
      {CloudNoiseLayer.Coverage, Shader.PropertyToID("_CloudCoverage3D")},
      {CloudNoiseLayer.Base, Shader.PropertyToID("_CloudBase3D")},
      {CloudNoiseLayer.Structure, Shader.PropertyToID("_CloudStructure3D")},
      {CloudNoiseLayer.Detail, Shader.PropertyToID("_CloudDetail3D")},
      {CloudNoiseLayer.BaseWarp, Shader.PropertyToID("_CloudBaseWarp3D")},
      {CloudNoiseLayer.DetailWarp, Shader.PropertyToID("_CloudDetailWarp3D")}
  };
  public static int cloudNoiseLayerTypeToShaderID(CloudNoiseLayer layerType, Datatypes.NoiseDimension dimension) {
    if (dimension == Datatypes.NoiseDimension.ThreeDimensional) {
      return cloudNoiseLayerToShaderVariable3D[layerType];
    } else {
      return cloudNoiseLayerToShaderVariable2D[layerType];
    }
  }

  /* Enum for cloud geometries. */
  [GenerateHLSL]
  public enum CloudGeometryType {
    Plane = 0,          /* Clouds are 2D, on a flat plane at some altitude. */
    CurvedPlane,        /* Clouds are 2D, on a plane that's curved with the planet at some altitude. */
    BoxVolume,          /* Clouds are volumetric and distributed throughout a rectangular box. */
    CurvedBoxVolume     /* Clouds are volumetric and distributed throughout a rectangular box that's curved with the planet. */
  };
  public const uint kNumCloudGeometryTypes = 4;

  /* Given a geometry type, what noise dimension does it use? */
  private static Dictionary<CloudGeometryType, Datatypes.NoiseDimension>
    cloudGeometryTypeToNoiseDimensionTable =
      new Dictionary<CloudGeometryType, Datatypes.NoiseDimension>() {
      {CloudGeometryType.Plane, Datatypes.NoiseDimension.TwoDimensional},
      {CloudGeometryType.CurvedPlane, Datatypes.NoiseDimension.TwoDimensional},
      {CloudGeometryType.BoxVolume, Datatypes.NoiseDimension.ThreeDimensional},
      {CloudGeometryType.CurvedBoxVolume, Datatypes.NoiseDimension.ThreeDimensional}
  };
  public static Datatypes.NoiseDimension cloudGeometryTypeToNoiseDimension(CloudGeometryType t) {
    return cloudGeometryTypeToNoiseDimensionTable[t];
  }

  /* Constant framebuffer resolution for cloud shadow maps. */
  public static Vector2Int cloudShadowMapQualityToResolution(Datatypes.Quality quality) {
    switch (quality) {
      case Datatypes.Quality.Potato: {
        return new Vector2Int(128, 128);
      }
      case Datatypes.Quality.Low: {
        return new Vector2Int(256, 256);
      }
      case Datatypes.Quality.Medium: {
        return new Vector2Int(512, 512);
      }
      case Datatypes.Quality.High: {
        return new Vector2Int(1024, 1024);
      }
      case Datatypes.Quality.Ultra: {
        return new Vector2Int(2048, 2048);
      }
      case Datatypes.Quality.RippingThroughTheMetaverse: {
        return new Vector2Int(4096, 4096);
      }
      default: {
        /* To be safe, uses potato quality. */
        return new Vector2Int(128, 128);
      }
    }
  }
}

} // namespace Expanse
