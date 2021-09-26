using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse
{

/**
 * @brief: Generates procedural nebula textures.
 */
public class NebulaGenerator : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Textures. */
  private Dictionary<string, RTHandle> m_textures = new Dictionary<string, RTHandle>();
  Vector2Int m_resolution;

  /* Profiling samplers. */
  ProfilingSampler m_profilingSampler = new ProfilingSampler("Expanse: Generate Nebula Texture");

  /* Compute shader and associated kernels. */
  ComputeShader m_CS = Resources.Load<ComputeShader>("NebulaGenerator");
  const string kNebulaKernel = "NEBULA";
  int m_nebulaHandle = 0;

  /* Shader variables. */
  const string kNebulaRW = "_Nebula_RW";

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    m_resolution = qualityToResolution(Datatypes.Quality.Potato);
    m_textures["nebulae"] = allocateEmulatedRGBACubemapTexture("Procedural Nebula", m_resolution);
    m_nebulaHandle =  m_CS.FindKernel(kNebulaKernel);
  }

  private void cleanupTextures() {
    foreach (var rt in m_textures) {
      RTHandles.Release(rt.Value);
    }
  }

  public override void cleanup() {
    cleanupTextures();
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies = null) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Resize our rendertexture if necessary. */
    checkAndResizeTextures();

    /* Set the relevant properties. */
    m_CS.SetVector("_res", new Vector4(m_resolution.x, m_resolution.y, 1, 1));

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      m_CS.SetTexture(m_nebulaHandle, kNebulaRW, m_textures["nebulae"]);
      cmd.DispatchCompute(m_CS, m_nebulaHandle, computeGroups(m_resolution.x, 4), computeGroups(m_resolution.y, 4), 6);
    }
  }

  private void checkAndResizeTextures() {
    Vector2Int newResolution = qualityToResolution(NebulaRenderSettings.GetQuality());
    if (newResolution != m_resolution) {
      cleanupTextures();
      m_resolution = newResolution;
      m_textures["nebulae"] = allocateEmulatedRGBACubemapTexture("Procedural Nebula", m_resolution);
    }
  }

  /* We define our own internal mapping of quality to resolution that
   * doesn't need to be exposed anywhere else. */
  private static Dictionary<Datatypes.Quality, Vector2Int>
    kQualityToResolutionTable =
      new Dictionary<Datatypes.Quality, Vector2Int>() {
        {Datatypes.Quality.Potato, new Vector2Int(64, 64)},
        {Datatypes.Quality.Low, new Vector2Int(128, 128)},
        {Datatypes.Quality.Medium, new Vector2Int(256, 256)},
        {Datatypes.Quality.High, new Vector2Int(512, 512)},
        {Datatypes.Quality.Ultra, new Vector2Int(1024, 1024)},
        {Datatypes.Quality.RippingThroughTheMetaverse, new Vector2Int(2048, 2048)}
      };

  private static Vector2Int qualityToResolution(Datatypes.Quality quality) {
      return kQualityToResolutionTable[quality];
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

  public override void setTexture(string texture, string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable, ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTextureResolution(string texture, string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetVector(shaderVariable, getTextureResolution(texture));
  }

  public override void setTextureResolution(string texture, string shaderVariable, ComputeShader computeShader) {
    computeShader.SetVector(shaderVariable, getTextureResolution(texture));
  }

  public override void setTextureResolution(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalVector(shaderVariable, getTextureResolution(texture));
  }

  public override Vector3 getTextureResolution(string texture) {
    return new Vector3(m_resolution.x, m_resolution.y, 1);
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/

};

} // namespace Expanse
