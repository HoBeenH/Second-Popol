using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

/**
 * @brief: generates procedural noises for one cloud layer.
 */
public class CloudGenerator : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Textures. */
  private Dictionary<string, RTHandle> m_textures = new Dictionary<string, RTHandle>();
  private CloudGeneratorResolution m_resolution;
  private Datatypes.NoiseDimension m_dimension = Datatypes.NoiseDimension.TwoDimensional;

  /* Profiling samplers. */
  ProfilingSampler m_profilingSampler = new ProfilingSampler("Expanse: Generate Cloud Textures");

  /* Compute shader. */
  ComputeShader[] m_CS = new ComputeShader[CloudDatatypes.kNumCloudNoiseLayers];

  /* Id's that correspond to kernels, shader variables, and texture names. */
  private static string kCoverageID = "coverage";
  private static string kBaseID = "base";
  private static string kStructureID = "structure";
  private static string kDetailID = "detail";
  private static string kBaseWarpID = "baseWarp";
  private static string kDetailWarpID = "detailWarp";
  private static string[] kLayerNames = new string[]{kCoverageID, kBaseID, kStructureID, kDetailID, kBaseWarpID, kDetailWarpID};
  /* For keeping track of whether or not we need to regenerate. */
  private int m_previousHashCode = 0;
  private int[] m_previousLayerHashCodes = new int[CloudDatatypes.kNumCloudNoiseLayers];

  /* Index of the layer we are assigned to generate. */
  private int m_layerIndex;
  public void setLayerIndex(int layerIndex) {
    m_layerIndex = layerIndex;
  }

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public CloudGenerator(int layerIndex) {
    m_layerIndex = layerIndex;
  }

  public override void build() {
    m_resolution = qualityToResolution(Datatypes.Quality.Medium, m_dimension);
    ComputeShader canonicalInstance = Resources.Load<ComputeShader>("CloudGenerator");
    for (int i = 0; i < m_CS.Length; i++) {
      m_CS[i] = (ComputeShader) UnityEngine.Object.Instantiate(canonicalInstance);
    }
    reallocateTextures();
  }

  public override void cleanup() {
    foreach (var rt in m_textures) {
      if (rt.Value != null) {
        RTHandles.Release(rt.Value);
      }
    }
    m_textures.Clear();
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies=null) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Get the cloud layer we are assigned. */
    UniversalCloudLayer layer = CloudLayerRenderSettings.GetLayer(m_layerIndex);

    /* Check if we need to regenerate all layers. */
    int newHashCode = layer.GetHashCode();
    bool regenerateAll = (m_previousHashCode != newHashCode);
    m_previousHashCode = newHashCode;

    /* Resize textures if necessary . */
    checkAndResizeTextures(layer.noiseTextureQuality, CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) layer.renderSettings.geometryType));

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      int i = 0;
      foreach (string name in kLayerNames) {
        UniversalCloudLayer.UniversalCloudNoiseLayer noiseLayer = layer.noiseLayers[i];
        // Only generate if we should regenerate them all, or if the noise has actually changed.
        int newLayerHashCode = noiseLayer.GetHashCode();
        if (noiseLayer.procedural && (m_previousLayerHashCodes[i] != newLayerHashCode || regenerateAll)) {
          /* Regenerate layer. */
          m_previousLayerHashCodes[i] = newLayerHashCode;
          regenerateLayer(cmd, noiseLayer, i, name, name == kCoverageID ? Datatypes.NoiseDimension.TwoDimensional : m_dimension);
        }
        i++;
      }
    }
  }

  private void regenerateLayer(CommandBuffer cmd, UniversalCloudLayer.UniversalCloudNoiseLayer noiseLayer, int i, string name, Datatypes.NoiseDimension dimension) {
    string dimensionString = (dimension == Datatypes.NoiseDimension.TwoDimensional) ? "2D" : "3D";
    int handle = m_CS[i].FindKernel(Datatypes.noiseTypeToKernelName((Datatypes.NoiseType) noiseLayer.renderSettings.noiseType) + dimensionString);

    m_CS[i].SetTexture(handle, "_Noise" + dimensionString, m_textures[name]);

    Vector3 res = new Vector3(m_textures[name].rt.width, m_textures[name].rt.height, m_textures[name].rt.volumeDepth);
    setShaderVariables(m_CS[i], noiseLayer, res);

    cmd.DispatchCompute(m_CS[i], handle, computeGroups(m_textures[name].rt.width, 4), computeGroups(m_textures[name].rt.height, 4), computeGroups(m_textures[name].rt.volumeDepth, 4));
    cmd.GenerateMips(m_textures[name]);
  }

  private void checkAndResizeTextures(Datatypes.Quality quality, Datatypes.NoiseDimension dimension) {
    CloudGeneratorResolution newResolution = qualityToResolution(quality, dimension);
    if (m_dimension != dimension || !CloudGeneratorResolution.areEqual(m_resolution, newResolution)) {
      m_resolution = newResolution;
      m_dimension = dimension;
      reallocateTextures();
    }
  }

  /* Reallocates textures according to m_dimension and m_resolution. */
  private void reallocateTextures() {
    cleanup();
    if (m_dimension == Datatypes.NoiseDimension.TwoDimensional) {
      m_textures[kCoverageID] = allocateMonochromeTexture2D("Layer " + m_layerIndex + " Cloud Coverage", Utilities.ToInt2(m_resolution.resCoverage), useMipMap: true, format: GraphicsFormat.R16_SNorm);
      m_textures[kBaseID] = allocateMonochromeTexture2D("Layer " + m_layerIndex + " Cloud Base", Utilities.ToInt2(m_resolution.resBase.xy()), useMipMap: true, format: GraphicsFormat.R16_SNorm);
      m_textures[kStructureID] = allocateMonochromeTexture2D("Layer " + m_layerIndex + " Cloud Structure", Utilities.ToInt2(m_resolution.resStructure.xy()), useMipMap: true, format: GraphicsFormat.R16_SNorm);
      m_textures[kDetailID] = allocateMonochromeTexture2D("Layer " + m_layerIndex + " Cloud Detail", Utilities.ToInt2(m_resolution.resDetail.xy()), useMipMap: true, format: GraphicsFormat.R16_SNorm);
      m_textures[kBaseWarpID] = allocateRGBATexture2D("Layer " + m_layerIndex + " Cloud Base Warp", Utilities.ToInt2(m_resolution.resBaseWarp.xy()), useMipMap: true, format: GraphicsFormat.R16G16B16A16_SNorm);
      m_textures[kDetailWarpID] = allocateRGBATexture2D("Layer " + m_layerIndex + " Cloud Detail Warp", Utilities.ToInt2(m_resolution.resDetailWarp.xy()), useMipMap: true, format: GraphicsFormat.R16G16B16A16_SNorm);
    } else {
      m_textures[kCoverageID] = allocateMonochromeTexture2D("Layer " + m_layerIndex + " Cloud Coverage", Utilities.ToInt2(m_resolution.resCoverage), useMipMap: true, format: GraphicsFormat.R8_SNorm);
      m_textures[kBaseID] = allocateMonochromeTexture3D("Layer " + m_layerIndex + " Cloud Base", Utilities.ToInt3(m_resolution.resBase), useMipMap: true, format: GraphicsFormat.R8_SNorm);
      m_textures[kStructureID] = allocateMonochromeTexture3D("Layer " + m_layerIndex + " Cloud Structure", Utilities.ToInt3(m_resolution.resStructure), useMipMap: true, format: GraphicsFormat.R8_SNorm);
      m_textures[kDetailID] = allocateMonochromeTexture3D("Layer " + m_layerIndex + " Cloud Detail", Utilities.ToInt3(m_resolution.resDetail), useMipMap: true, format: GraphicsFormat.R8_SNorm);
      m_textures[kBaseWarpID] = allocateRGBATexture3D("Layer " + m_layerIndex + " Cloud Base Warp", Utilities.ToInt3(m_resolution.resBaseWarp), useMipMap: true, format: GraphicsFormat.R8G8B8A8_SNorm);
      m_textures[kDetailWarpID] = allocateRGBATexture3D("Layer " + m_layerIndex + " Cloud Detail Warp", Utilities.ToInt3(m_resolution.resDetailWarp), useMipMap: true, format: GraphicsFormat.R8G8B8A8_SNorm);
    }
  }

/******************************************************************************/
/******************************* END RENDERING ********************************/
/******************************************************************************/



/******************************************************************************/
/****************************** GETTERS/SETTERS *******************************/
/******************************************************************************/

  public override IReadOnlyCollection<string> getTextureNames() {
    return m_textures.Keys;
  }

  public override void setTexture(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable,
    CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, int shaderVariable,
    CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTextureResolution(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetVector(shaderVariable, getTextureResolution(texture));
  }

  public override void setTextureResolution(string texture, string shaderVariable,
    ComputeShader computeShader) {
    computeShader.SetVector(shaderVariable, getTextureResolution(texture));
  }

  public override void setTextureResolution(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalVector(shaderVariable, getTextureResolution(texture));
  }

  public override Vector3 getTextureResolution(string texture) {
    return new Vector3(m_textures[texture].rt.width, m_textures[texture].rt.height, m_textures[texture].rt.volumeDepth);
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/



/******************************************************************************/
/************************** SHADER VARIABLE HANDLERS **************************/
/******************************************************************************/

  private void setShaderVariables(ComputeShader cs, UniversalCloudLayer.UniversalCloudNoiseLayer noiseLayer, Vector3 res) {
    cs.SetVector("_res", res);
    cs.SetVector("_scale", noiseLayer.renderSettings.scale);
    cs.SetInt("_octaves", noiseLayer.renderSettings.octaves);
    cs.SetFloat("_octaveScale", noiseLayer.renderSettings.octaveScale);
    cs.SetFloat("_octaveMultiplier", noiseLayer.renderSettings.octaveMultiplier);
  }

/******************************************************************************/
/************************ END SHADER VARIABLE HANDLERS ************************/
/******************************************************************************/



/******************************************************************************/
/****************************** RESOLUTION CLASS ******************************/
/******************************************************************************/

  /* We define our own internal mapping of quality to resolution that
   * doesn't need to be exposed anywhere else. */
  private struct CloudGeneratorResolution {
    public Vector2 resCoverage;
    public Vector3 resBase;
    public Vector3 resStructure;
    public Vector3 resDetail;
    public Vector3 resBaseWarp;
    public Vector3 resDetailWarp;
    /* HACK: should properly implement proper Equals() function. */
    public static bool areEqual(CloudGeneratorResolution r1, CloudGeneratorResolution r2) {
      return (r1.resCoverage == r2.resCoverage) &&
        (r1.resBase == r2.resBase) &&
        (r1.resStructure == r2.resStructure) &&
        (r1.resDetail == r2.resDetail) &&
        (r1.resBaseWarp == r2.resBaseWarp) &&
        (r1.resDetailWarp == r2.resDetailWarp);
    }
  }
  private static Dictionary<Datatypes.Quality, CloudGeneratorResolution>
    kQualityToResolutionTable2D =
      new Dictionary<Datatypes.Quality, CloudGeneratorResolution>() {
      {Datatypes.Quality.Potato, new CloudGeneratorResolution() {
          resCoverage = new Vector2(64, 64),
          resBase = new Vector3(64, 64, 1),
          resStructure = new Vector3(64, 64, 1),
          resDetail = new Vector3(64, 64, 1),
          resBaseWarp = new Vector3(64, 64, 1),
          resDetailWarp = new Vector3(64, 64, 1)
        }},
      {Datatypes.Quality.Low, new CloudGeneratorResolution() {
          resCoverage = new Vector2(64, 64),
          resBase = new Vector3(64, 64, 1),
          resStructure = new Vector3(64, 64, 1),
          resDetail = new Vector3(64, 64, 1),
          resBaseWarp = new Vector3(64, 64, 1),
          resDetailWarp = new Vector3(64, 64, 1)
        }},
      {Datatypes.Quality.Medium, new CloudGeneratorResolution() {
          resCoverage = new Vector2(512, 512),
          resBase = new Vector3(512, 512, 1),
          resStructure = new Vector3(128, 128, 1),
          resDetail = new Vector3(64, 64, 1),
          resBaseWarp = new Vector3(256, 256, 1),
          resDetailWarp = new Vector3(64, 64, 1)
        }},
      {Datatypes.Quality.High, new CloudGeneratorResolution() {
        resCoverage = new Vector2(512, 512),
        resBase = new Vector3(512, 512, 1),
        resStructure = new Vector3(128, 128, 1),
        resDetail = new Vector3(64, 64, 1),
        resBaseWarp = new Vector3(256, 256, 1),
        resDetailWarp = new Vector3(64, 64, 1)
        }},
      {Datatypes.Quality.Ultra, new CloudGeneratorResolution() {
        resCoverage = new Vector2(1024, 1024),
        resBase = new Vector3(1024, 1024, 1),
        resStructure = new Vector3(512, 512, 1),
        resDetail = new Vector3(256, 256, 1),
        resBaseWarp = new Vector3(1024, 1024, 1),
        resDetailWarp = new Vector3(256, 256, 1)
        }},
      {Datatypes.Quality.RippingThroughTheMetaverse, new CloudGeneratorResolution() {
        resCoverage = new Vector2(1024, 1024),
        resBase = new Vector3(2048, 2048, 1),
        resStructure = new Vector3(1024, 1024, 1),
        resDetail = new Vector3(512, 512, 1),
        resBaseWarp = new Vector3(1024, 1024, 1),
        resDetailWarp = new Vector3(256, 256, 1)
        }}
    };
  private static Dictionary<Datatypes.Quality, CloudGeneratorResolution>
    kQualityToResolutionTable3D =
      new Dictionary<Datatypes.Quality, CloudGeneratorResolution>() {
      {Datatypes.Quality.Potato, new CloudGeneratorResolution() {
        resCoverage = new Vector2(256, 256),
        resBase = new Vector3(64, 64, 64),
        resStructure = new Vector3(32, 32, 32),
        resDetail = new Vector3(64, 64, 64),
        resBaseWarp = new Vector3(32, 32, 32),
        resDetailWarp = new Vector3(32, 32, 32)
        }},
      {Datatypes.Quality.Low, new CloudGeneratorResolution() {
        resCoverage = new Vector2(512, 512),
        resBase = new Vector3(128, 128, 128),
        resStructure = new Vector3(64, 64, 64),
        resDetail = new Vector3(64, 64, 64),
        resBaseWarp = new Vector3(32, 32, 32),
        resDetailWarp = new Vector3(32, 32, 32)
        }},
      {Datatypes.Quality.Medium, new CloudGeneratorResolution() {
        resCoverage = new Vector2(1024, 1024),
        resBase = new Vector3(256, 256, 256),
        resStructure = new Vector3(64, 64, 64),
        resDetail = new Vector3(64, 64, 64),
        resBaseWarp = new Vector3(64, 64, 64),
        resDetailWarp = new Vector3(64, 64, 64)
        }},
      {Datatypes.Quality.High, new CloudGeneratorResolution() {
        resCoverage = new Vector2(1024, 1024),
        resBase = new Vector3(256, 256, 256),
        resStructure = new Vector3(128, 128, 128),
        resDetail = new Vector3(128, 128, 128),
        resBaseWarp = new Vector3(64, 64, 64),
        resDetailWarp = new Vector3(64, 64, 64)
        }},
      {Datatypes.Quality.Ultra, new CloudGeneratorResolution() {
        resCoverage = new Vector2(1024, 1024),
        resBase = new Vector3(512, 512, 512),
        resStructure = new Vector3(256, 256, 256),
        resDetail = new Vector3(128, 128, 128),
        resBaseWarp = new Vector3(64, 64, 64),
        resDetailWarp = new Vector3(64, 64, 64)
        }},
      {Datatypes.Quality.RippingThroughTheMetaverse, new CloudGeneratorResolution() {
          resCoverage = new Vector2(1024, 1024),
          resBase = new Vector3(512, 512, 512),
          resStructure = new Vector3(256, 256, 256),
          resDetail = new Vector3(256, 256, 256),
          resBaseWarp = new Vector3(128, 128, 128),
          resDetailWarp = new Vector3(128, 128, 128)
        }}
    };
  private static CloudGeneratorResolution qualityToResolution(Datatypes.Quality quality, Datatypes.NoiseDimension dimension) {
    if (dimension == Datatypes.NoiseDimension.TwoDimensional) {
      return kQualityToResolutionTable2D[quality];
    } else {
      return kQualityToResolutionTable3D[quality];
    }
  }

/******************************************************************************/
/**************************** END RESOLUTION CLASS ****************************/
/******************************************************************************/

};

} // namespace Expanse
