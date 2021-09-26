using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse {

/**
 * @brief: Generates procedural star textures.
 */
public class StarGenerator : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Textures. */
  private Dictionary<string, RTHandle> m_textures = new Dictionary<string, RTHandle>();
  Vector2Int m_resolution;

  /* Profiling samplers. */
  ProfilingSampler m_profilingSampler = new ProfilingSampler("Expanse: Generate Star Texture");

  /* Compute shader and associated kernels. */
  ComputeShader m_CS = Resources.Load<ComputeShader>("StarGenerator");
  const string kStarKernel = "STAR";
  int m_starHandle = 0;

  /* Shader variables. */
  const string kStarRW = "_Star_RW";

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    m_resolution = qualityToResolution(Datatypes.Quality.Potato);
    m_textures["stars"] = allocateEmulatedRGBACubemapTexture("Procedural Stars", m_resolution, useMipMap: true);
    m_starHandle = m_CS.FindKernel(kStarKernel);
  }

  private void cleanupTextures() {
    foreach (var rt in m_textures) {
      RTHandles.Release(rt.Value);
    }
    m_textures.Clear();
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

  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies=null) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Resize our rendertexture if necessary. */
    checkAndResizeTextures(settings);

    /* Set the relevant properties. */
    m_CS.SetVector("_resolution", new Vector4(m_resolution.x, m_resolution.y, 0, 0));

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      m_CS.SetTexture(m_starHandle, kStarRW, m_textures["stars"]);
      cmd.DispatchCompute(m_CS, m_starHandle, computeGroups(m_resolution.x, 4), computeGroups(m_resolution.y, 4), 6);
      cmd.GenerateMips(m_textures["stars"]);
    }
  }

  private void checkAndResizeTextures(ExpanseSettings settings) {
    Vector2Int newResolution = qualityToResolution(StarRenderSettings.GetQuality());
    if (newResolution != m_resolution) {
      cleanupTextures();
      m_resolution = newResolution;
      m_textures["stars"] = allocateEmulatedRGBACubemapTexture("Procedural Stars", m_resolution, useMipMap: true);
    }
  }

  /* We define our own internal mapping of quality to resolution that
   * doesn't need to be exposed anywhere else. */
  private static Dictionary<Datatypes.Quality, Vector2Int>
    kQualityToResolutionTable =
      new Dictionary<Datatypes.Quality, Vector2Int>() {
        {Datatypes.Quality.Potato, new Vector2Int(256, 256)},
        {Datatypes.Quality.Low, new Vector2Int(512, 512)},
        {Datatypes.Quality.Medium, new Vector2Int(1024, 1024)},
        {Datatypes.Quality.High, new Vector2Int(2048, 2048)},
        {Datatypes.Quality.Ultra, new Vector2Int(4096, 4096)},
        {Datatypes.Quality.RippingThroughTheMetaverse, new Vector2Int(4096, 4096)}
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

  public override void setTexture(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_textures[texture]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
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
    return new Vector3(m_resolution.x, m_resolution.y, 1);
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/

};

} // namespace Expanse
