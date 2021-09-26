using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse {

/**
 * @brief: renders all "direct light" contributions. This includes,
 *  -the planet
 *  -celestial bodies
 *  -stars
 *  -nebulae
 */
public class DirectLightRenderer : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Framebuffers. */
  private Dictionary<string, RTHandle> m_framebuffers = new Dictionary<string, RTHandle>();
  /* Default texture cube for setting empty textures. */
  RTHandle m_defaultTextureCube;

  /* Compute buffers for passing settings to the shaders. */
  ComputeBuffer m_settingsBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectLightSettings)));
  const string kSettingsBuffer = "_settingsBuffer";
  const string kBodySettingsBuffer = "_bodySettingsBuffer";
  DirectLightSettings[] m_directLightSettings = new DirectLightSettings[1];

  /* Profiling samplers. */
  ProfilingSampler m_profilingSampler = new ProfilingSampler("Expanse: Draw Direct Light");

  /* Compute shader and associated kernels. */
  ComputeShader m_CS = Resources.Load<ComputeShader>("DirectLightRenderer");
  const string kDirectLightKernel = "DIRECTLIGHT";
  int m_directLightHandle = 0;

  /* Shader variables. */
  const string kFramebufferRW = "_Framebuffer_RW";
  const string kBodyAlbedoTexture = "_bodyAlbedoTex";
  const string kBodyEmissionTexture = "_bodyEmissionTex";
  const string kAuthoredStarTexture = "_authoredStarTexture";
  const string kAuthoredNebulaeTexture = "_authoredNebulaeTexture";
  const string kPlanetAlbedoTexture = "_planetAlbedoTex";
  const string kPlanetEmissionTexture = "_planetEmissionTex";

  /* For keeping track of the screen's resolution. */
  Vector2Int m_resolution = new Vector2Int(128, 128);

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    m_framebuffers["fullscreen"] = allocateRGBATexture2D("Direct Light Fullscreen", m_resolution);
    m_defaultTextureCube = allocateDefaultTextureCube();
    m_directLightHandle = m_CS.FindKernel(kDirectLightKernel);
  }

  public void cleanupFramebuffers() {
    foreach (var rt in m_framebuffers) {
      RTHandles.Release(rt.Value);
    }
  }

  public override void cleanup() {
    cleanupFramebuffers();
    RTHandles.Release(m_defaultTextureCube);
    m_defaultTextureCube = null;
    m_settingsBuffer.Release();
    m_settingsBuffer = null;
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  /* Expects dependencies to be {StarGenerator, NebulaGenerator} */
  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Resize our rendertexture if necessary. */
    checkAndResizeFramebuffer(new Vector2Int((int) builtinParams.screenSize.x, (int) builtinParams.screenSize.y));

    /* Set the relevant shader variables. */
    setShaderVariables(builtinParams);
    setSettingsBuffers(settings, m_directLightHandle);
    setProceduralTextures((StarGenerator) dependencies[0], (NebulaGenerator) dependencies[1]);

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      m_CS.SetTexture(m_directLightHandle, kFramebufferRW, m_framebuffers["fullscreen"]);
      cmd.DispatchCompute(m_CS, m_directLightHandle, computeGroups(m_resolution.x, 8), computeGroups(m_resolution.y, 8), 1);
    }
  }

  private void checkAndResizeFramebuffer(Vector2Int newResolution) {
    if (newResolution != m_resolution) {
      cleanupFramebuffers();
      m_resolution = newResolution;
      m_framebuffers["fullscreen"] = allocateRGBATexture2D("Direct Light Fullscreen", m_resolution);
    }
  }

/******************************************************************************/
/******************************* END RENDERING ********************************/
/******************************************************************************/



/******************************************************************************/
/****************************** GETTERS/SETTERS *******************************/
/******************************************************************************/

  public override IReadOnlyCollection<string> getTextureNames() {
    return m_framebuffers.Keys;
  }

  public override void setTexture(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_framebuffers[texture]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_framebuffers[texture]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture]);
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



/******************************************************************************/
/************************** SHADER VARIABLE HANDLERS **************************/
/******************************************************************************/

  [GenerateHLSL(needAccessors=false)]
  private struct DirectLightSettings {
    public Vector2 resolution;
  }

  private void setShaderVariables(BuiltinSkyParameters builtinParams) {
    CommandBuffer cmd = builtinParams.commandBuffer;
    cmd.SetGlobalMatrix("_PixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix);
    m_CS.SetVector("_WorldSpaceCameraPos1", builtinParams.worldSpaceCameraPos);
  }

  private void setProceduralTextures(StarGenerator starGenerator, NebulaGenerator nebulaGenerator) {
    m_CS.SetVector("_resStar", starGenerator.getTextureResolution("stars"));
    starGenerator.setTexture("stars", "_proceduralStarTexture", m_CS, m_directLightHandle);
    nebulaGenerator.setTexture("nebulae", "_proceduralNebulaeTexture", m_CS, m_directLightHandle);
  }

  private void setSettingsBuffers(ExpanseSettings settings, int handle) {
    setGeneralSettings(settings, handle);
  }

  private void setGeneralSettings(ExpanseSettings settings, int handle) {
    m_directLightSettings[0].resolution = m_resolution;
    m_settingsBuffer.SetData(m_directLightSettings);
    m_CS.SetBuffer(handle, kSettingsBuffer, m_settingsBuffer);
  }

/******************************************************************************/
/************************ END SHADER VARIABLE HANDLERS ************************/
/******************************************************************************/

};

} // namespace Expanse
