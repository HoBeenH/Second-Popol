using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

/**
 * @brief: composites cloud layers together.
 */
public class CloudCompositor : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Framebuffers. */
  private Dictionary<string, RTHandle[]> m_framebuffers = new Dictionary<string, RTHandle[]>();
  const string kFullscreenLightingFramebuffer = "fullscreenLighting";
  const string kFullscreenTransmittanceAndHitFramebuffer = "fullscreenTransmittanceAndHit";
  const string kReflectionFramebuffer = "reflection";
  const string kReflectionTFramebuffer = "reflectionT";
  string[] kShadowMapFramebuffer = new string[CelestialBodyDatatypes.kMaxCelestialBodies];
  const string kLightAttenuationFramebuffer = "lightAttenuation";
  private int m_numShadowMapBuffers = 1;

  /* HACK: kind of hacky, static RTHandle array for shadow maps. This way,
   * they are accessible from the light control script. */
  private static RTHandle[] m_shadowMaps = new RTHandle[CelestialBodyDatatypes.kMaxCelestialBodies];

  /* Default texture for setting empty textures. */
  private RTHandle m_defaultTexture2D = allocateDefaultTexture2D();

  /* Profiling samplers. */
  ProfilingSampler m_fullscreenSampler =  new ProfilingSampler("Expanse: Composite Cloud Layer Fullscreen");
  ProfilingSampler m_reflectionSampler =  new ProfilingSampler("Expanse: Composite Cloud Layer Reflection Probe");
  ProfilingSampler m_shadowSampler =  new ProfilingSampler("Expanse: Composite Cloud Layer Shadow Map");
  ProfilingSampler m_shadowBlurSampler =  new ProfilingSampler("Expanse: Blur Cloud Layer Shadow Map");
  ProfilingSampler m_lightAttenuationSampler =  new ProfilingSampler("Expanse: Copy Cloud Light Attenuation");

  /* Compute shaders and associated kernels. We have to instantiate multiple
   * compute shaders to avoid having them clobbering each other. */
  ComputeShader m_fullscreenCS;
  ComputeShader[] m_shadowMapCS = new ComputeShader[CelestialBodyDatatypes.kMaxCelestialBodies];
  const string kFullscreen0Kernel = "FULLSCREEN_0";
  const string kFullscreen1Kernel = "FULLSCREEN_1";
  const string kFullscreen2Kernel = "FULLSCREEN_2";
  const string kFullscreenNKernel = "FULLSCREEN_N";
  const string kFullscreenSortedNKernel = "FULLSCREEN_SORTED_N";
  const string kReflectionKernel = "REFLECTION";
  const string kShadowMapKernel = "SHADOWMAP";
  const string kBlurHorizontalKernel = "BLURHORIZONTAL";
  const string kBlurVerticalKernel = "BLURVERTICAL";
  const string kLightAttenuationKernel = "LIGHTATTENUATION";

  /* Shader variables. */
  const string kCompositeLightingFramebufferTexture = "_LightingFramebuffer";
  const string kCompositeTransmittanceHitFramebufferTexture = "_TransmittanceAndHitFramebuffer";
  const string kCompositeShadowMapFramebufferTexture = "_ShadowMapFramebuffer";
  const string kReflectionTFramebufferTexture = "_ReflectionTFramebuffer";
  const string kLightingFramebufferTexture = "_CloudLightingFramebuffers";
  const string kLightAttenuationTexture = "_LightAttenuationFramebuffer";
  const string kGBufferTexture = "_CloudGBuffers";
  const string kReflectionTexture = "_CloudReflectionFramebuffers";
  const string kReflectionTTexture = "_CloudReflectionTFramebuffers";
  const string kTransmittanceHitFramebufferTexture = "_CloudTransmittanceHitFramebuffers";
  const string kShadowMapFramebufferToBlurTexture = "_ShadowMapFramebufferToBlur";
  const string kBodyShadowmapTexture = "_BodyShadowMap";
  const string kSortedIndexBuffer = "_SortedLayerIndexBuffer";

  /* Constant upscale factor for shadow maps. This allows us to use hermite
   * interpolation to further smooth their appearance, without having to
   * actually render more pixels. */
  const int kShadowMapCompositeResolutionFactor = 1;
  private Vector2Int m_shadowMapResolution = new Vector2Int(128, 128);

  /* Signifier for whether or not we have pre-rendered the null result in the
   * case that we have zero layers. */
  private bool m_prerenderedNullResult = false;


  /* Signifier for whether or not we have cleared the light attenuation texture
   * in the case that we have zero layers. */
  private bool m_clearedLightAttenuation = false;

  /* Compute buffer for keeping track of indices. */
  ComputeBuffer m_indexBuffer;

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/***************************** SHADER PROPERTIES ******************************/
/******************************************************************************/

  private static int[] kHasShadowMapIDs = new int[CelestialBodyDatatypes.kMaxCelestialBodies];
  private static int[] kBodyShadowmapTextureIDs = new int[CelestialBodyDatatypes.kMaxCelestialBodies];
  private static string[] kShadowmapTextureNames = new string[CelestialBodyDatatypes.kMaxCelestialBodies];

/******************************************************************************/
/*************************** END SHADER PROPERTIES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    for (int i = 0; i < kHasShadowMapIDs.Length; i++) {
      kHasShadowMapIDs[i] = Shader.PropertyToID("_hasShadowMap" + i);
      kBodyShadowmapTextureIDs[i] = Shader.PropertyToID(kBodyShadowmapTexture + i);
      kShadowMapFramebuffer[i] = "shadowMap" + i;
      kShadowmapTextureNames[i] = "cloudShadow" + i;
    }

    /* Initialize the framebuffers to default resolutions. */
    m_framebuffers[kFullscreenLightingFramebuffer] = new RTHandle[1];
    m_framebuffers[kReflectionFramebuffer] = new RTHandle[1];
    m_framebuffers[kReflectionTFramebuffer] = new RTHandle[1];
    m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer] = new RTHandle[1];
    allocateFullscreenFramebuffers(new Vector2Int(64, 64));
    allocateReflectionFramebuffers(new Vector2Int(256, 256));
    allocateShadowMapFramebuffers(1, m_shadowMapResolution);

    /* We only allocate the light attenuation framebuffer once. */
    m_framebuffers[kLightAttenuationFramebuffer] = new RTHandle[1];
    m_framebuffers[kLightAttenuationFramebuffer][0] = allocateRGBATexture2D("Cloud Light Attenuation Framebuffer", new Vector2Int((int) CelestialBodyDatatypes.kMaxCelestialBodies, 1));

    /* Initialize the compute shader instances. */
    buildComputeShaders();
  }

  public override void cleanup() {
    cleanupFullscreenFramebuffers();
    cleanupReflectionFramebuffers();
    cleanupShadowMapFramebuffers();
    RTHandles.Release(m_framebuffers[kLightAttenuationFramebuffer][0]);
    RTHandles.Release(m_defaultTexture2D);
    if (m_indexBuffer != null) {
      m_indexBuffer.Release();
    }
  }

  private void allocateFullscreenFramebuffers(Vector2Int res) {
    m_framebuffers[kFullscreenLightingFramebuffer][0] = allocateRGBATexture2D("Cloud Compositing Lighting Framebuffer", res);
    m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer][0] = allocateRGBATexture2D("Cloud Compositing Transmittance and Hit Framebuffer", res, useMipMap: true);
  }

  private void cleanupFullscreenFramebuffers() {
    RTHandles.Release(m_framebuffers[kFullscreenLightingFramebuffer][0]);
    RTHandles.Release(m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer][0]);
  }

  private void allocateReflectionFramebuffers(Vector2Int res) {
    m_framebuffers[kReflectionFramebuffer][0] = allocateRGBATexture2D("Cloud Compositing Reflection Framebuffer", res, useMipMap: true);
    m_framebuffers[kReflectionTFramebuffer][0] = allocateMonochromeTexture2D("Cloud Compositing Reflection T Framebuffer", res);
  }

  private void cleanupReflectionFramebuffers() {
    RTHandles.Release(m_framebuffers[kReflectionFramebuffer][0]);
    RTHandles.Release(m_framebuffers[kReflectionTFramebuffer][0]);
  }

  private void allocateShadowMapFramebuffers(int numShadowMapBuffers, Vector2Int res) {
    for (int i = 0; i < numShadowMapBuffers; i++) {
      m_framebuffers[kShadowMapFramebuffer[i]] = new RTHandle[2];
      m_framebuffers[kShadowMapFramebuffer[i]][0] = allocateRGBATexture2D("Cloud Shadowmap Framebuffer " + i, res * kShadowMapCompositeResolutionFactor, useMipMap: true);
      m_framebuffers[kShadowMapFramebuffer[i]][1] = allocateRGBATexture2D("Cloud Shadowmap Framebuffer " + i, res * kShadowMapCompositeResolutionFactor, useMipMap: true);
    }
    m_numShadowMapBuffers = numShadowMapBuffers;
  }

  private void cleanupShadowMapFramebuffers() {
    for (int i = 0; i < m_numShadowMapBuffers; i++) {
      RTHandles.Release(m_framebuffers[kShadowMapFramebuffer[i]][0]);
      RTHandles.Release(m_framebuffers[kShadowMapFramebuffer[i]][1]);
    }
    m_numShadowMapBuffers = 0;
  }

  private void buildComputeShaders() {
    ComputeShader canonicalComputeShader = Resources.Load<ComputeShader>("CloudCompositor");
    m_fullscreenCS = (ComputeShader) UnityEngine.Object.Instantiate(canonicalComputeShader);
    for (int i = 0; i < m_shadowMapCS.Length; i++) {
      m_shadowMapCS[i] = (ComputeShader) UnityEngine.Object.Instantiate(canonicalComputeShader);
    }
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  /* Dependencies is expected to have a single entry, which is a cloud
   * renderer. */
  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    if (m_shadowMapCS[0] == null) {
      buildComputeShaders();
    }

    CloudRenderer cloudRenderer = (CloudRenderer) dependencies[0];

    compositeFullscreen(builtinParams, cloudRenderer);

    compositeShadowMaps(builtinParams, cloudRenderer);

    /* Set global textures to be available for transparent shaders. */
    CommandBuffer cmd = builtinParams.commandBuffer;
    cloudRenderer.setTexture("cloudLighting", "_EXPANSE_CLOUD_LIGHTING_FRAMEBUFFERS", cmd);
    cloudRenderer.setTexture("cloudTransmittanceAndHit", "_EXPANSE_CLOUD_TRANSMITTANCE_FRAMEBUFFERS", cmd);
    cmd.SetGlobalTexture("_EXPANSE_CLOUD_LIGHTING_COMPOSITE", m_framebuffers[kFullscreenLightingFramebuffer][0]);
    cmd.SetGlobalTexture("_EXPANSE_CLOUD_TRANSMITTANCE_COMPOSITE", m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer][0]);
    cmd.SetGlobalTexture("_EXPANSE_CLOUD_REFLECTION", m_framebuffers[kReflectionFramebuffer][0]);
    cmd.SetGlobalInt("_EXPANSE_CLOUD_USE_ARRAY", (CloudLayerRenderSettings.GetLayerCount() == 1) ? 1 : 0);
  }

  private void compositeFullscreen(BuiltinSkyParameters builtinParams,
    CloudRenderer cloudRenderer) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Only render null result if we haven't already. */
    int enabledLayers = CloudLayerRenderSettings.GetLayerCount();
    if (enabledLayers == 0 && m_prerenderedNullResult) {
      return;
    }
    
    QualitySettingsBlock quality = QualityRenderSettings.Get();

    /* Retrieve the screen resolution and reallocate framebuffer if necessary. */
    checkAndResizeFramebuffers(builtinParams.screenSize.xy());

    /* Optimize alpha sorting by selecting the appropriate kernel. */
    int fullscreenHandle = chooseFullscreenKernel(enabledLayers, quality.m_compositeCloudsByHeight);
    int reflectionHandle = m_fullscreenCS.FindKernel(kReflectionKernel);

    setFullscreenShaderVariables(builtinParams, cmd, m_fullscreenCS, fullscreenHandle);
    setFullscreenTextures(cloudRenderer, m_fullscreenCS, fullscreenHandle);
    
    /* Don't composite fullscreen if we have one layer. We'll just use the
     * textures directly. */
    if (CloudLayerRenderSettings.GetLayerCount() != 1) {
      using (new ProfilingScope(cmd, m_fullscreenSampler)) {
        cmd.DispatchCompute(m_fullscreenCS, fullscreenHandle, computeGroups(m_framebuffers[kFullscreenLightingFramebuffer][0].rt.width, 8), computeGroups(m_framebuffers[kFullscreenLightingFramebuffer][0].rt.height, 8), 1);
      }
    }

    if (enabledLayers > 1) {
      /* Generate mips to use in screenspace layers. */
      cmd.GenerateMips(m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer][0]);
    }

    /* Composite reflection probes cheaply by just summing the lighting results 
     * without alpha blending. */
    using (new ProfilingScope(cmd, m_reflectionSampler)) {
      cloudRenderer.setTexture("cloudReflection", kReflectionTexture, m_fullscreenCS, reflectionHandle);
      cloudRenderer.setTexture("cloudReflectionT", kReflectionTTexture, m_fullscreenCS, reflectionHandle);
      m_fullscreenCS.SetVector("_reflectionRes", new Vector2(m_framebuffers[kReflectionFramebuffer][0].rt.width, m_framebuffers[kReflectionFramebuffer][0].rt.height));
      m_fullscreenCS.SetTexture(reflectionHandle, kCompositeLightingFramebufferTexture, m_framebuffers[kReflectionFramebuffer][0]);
      m_fullscreenCS.SetTexture(reflectionHandle, kReflectionTFramebufferTexture, m_framebuffers[kReflectionTFramebuffer][0]);
      cmd.DispatchCompute(m_fullscreenCS, reflectionHandle, computeGroups(m_framebuffers[kReflectionFramebuffer][0].rt.width, 8), computeGroups(m_framebuffers[kReflectionFramebuffer][0].rt.height, 8), 1);
      cmd.GenerateMips(m_framebuffers[kReflectionFramebuffer][0]);
    }

    m_prerenderedNullResult = (enabledLayers == 0);
  }

  private void compositeShadowMaps(BuiltinSkyParameters builtinParams,
    CloudRenderer cloudRenderer) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Only composite if there are layers to composite. */
    if (CloudLayerRenderSettings.GetLayerCount() == 0 || CloudLayerRenderSettings.GetShadowLayerCount() == 0) {
      if (!m_clearedLightAttenuation) {
        clearLightAttenuation();
        disableShadowMaps();
        m_clearedLightAttenuation = true;
      }
      return;
    }

    /* Reallocate shadow map framebuffers if the number of shadow bodies has
     * changed. */
    int shadowLights = LightingRenderSettings.GetCloudShadowLights();
    checkAndResizeShadowMapFramebuffers(shadowLights, cloudRenderer.getShadowmapResolution());

    /* Composite the shadow map for each body. */
    for (int i = 0; i < shadowLights; i++) {
      compositeBodyShadowMap(builtinParams, cloudRenderer, i);
      /* Set the static global for access by the light control scripts. */
      m_shadowMaps[i] = m_framebuffers[kShadowMapFramebuffer[i]][0];
    }

    /* Write out highest shadow map mip levels to an 8-pixel texture to use in
     * the sky renderer. */
    renderLightAttenuation(builtinParams, shadowLights);

    m_clearedLightAttenuation = false;
  }

  private void compositeBodyShadowMap(BuiltinSkyParameters builtinParams,
    CloudRenderer cloudRenderer, int i) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Find all the kernel handles. */
    int compositeHandle = m_shadowMapCS[i].FindKernel(kShadowMapKernel);
    int blurHorizontalHandle = m_shadowMapCS[i].FindKernel(kBlurHorizontalKernel);
    int blurVerticalHandle = m_shadowMapCS[i].FindKernel(kBlurVerticalKernel);

    /* Set all the shader variables. */
    setShadowMapShaderVariables(cloudRenderer, m_shadowMapCS[i], i);

    /* Composite. */
    cloudRenderer.setTexture(kShadowmapTextureNames[i], kLightingFramebufferTexture, m_shadowMapCS[i], compositeHandle);
    m_shadowMapCS[i].SetTexture(compositeHandle, kCompositeShadowMapFramebufferTexture, m_framebuffers[kShadowMapFramebuffer[i]][0]);
    using (new ProfilingScope(cmd, m_shadowSampler)) {
      cmd.DispatchCompute(m_shadowMapCS[i], compositeHandle, computeGroups(m_shadowMapResolution.x * kShadowMapCompositeResolutionFactor, 8), computeGroups(m_shadowMapResolution.y * kShadowMapCompositeResolutionFactor, 8), 1);
    }

    /* Blur horizontally. */
    m_shadowMapCS[i].SetTexture(blurHorizontalHandle, kShadowMapFramebufferToBlurTexture, m_framebuffers[kShadowMapFramebuffer[i]][0]);
    m_shadowMapCS[i].SetTexture(blurHorizontalHandle, kCompositeShadowMapFramebufferTexture, m_framebuffers[kShadowMapFramebuffer[i]][1]);
    using (new ProfilingScope(cmd, m_shadowBlurSampler)) {
      cmd.DispatchCompute(m_shadowMapCS[i], blurHorizontalHandle, computeGroups(m_shadowMapResolution.x * kShadowMapCompositeResolutionFactor, 8), computeGroups(m_shadowMapResolution.y * kShadowMapCompositeResolutionFactor, 8), 1);
    }

    /* Blur vertically. */
    m_shadowMapCS[i].SetTexture(blurVerticalHandle, kShadowMapFramebufferToBlurTexture, m_framebuffers[kShadowMapFramebuffer[i]][1]);
    m_shadowMapCS[i].SetTexture(blurVerticalHandle, kCompositeShadowMapFramebufferTexture, m_framebuffers[kShadowMapFramebuffer[i]][0]);
    using (new ProfilingScope(cmd, m_shadowBlurSampler)) {
      cmd.DispatchCompute(m_shadowMapCS[i], blurVerticalHandle, computeGroups(m_shadowMapResolution.x * kShadowMapCompositeResolutionFactor, 8), computeGroups(m_shadowMapResolution.y * kShadowMapCompositeResolutionFactor, 8), 1);
    }

    /* Generate mips for attenuating aerial perspective. */
    cmd.GenerateMips(m_framebuffers[kShadowMapFramebuffer[i]][0]);
    
    /* This has to be done in order to signify that the cookie atlas should be rebuilt. */
    m_framebuffers[kShadowMapFramebuffer[i]][0].rt.IncrementUpdateCount();
  }

  private void renderLightAttenuation(BuiltinSkyParameters builtinParams, int shadowBodies) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    // TODO: fix
    int lightAttenuationHandle = m_shadowMapCS[0].FindKernel(kLightAttenuationKernel);

    /* By default, set all textures to be white. */
    for (int i = 0; i < CelestialBodyDatatypes.kMaxCelestialBodies; i++) {
      m_shadowMapCS[0].SetInt(kHasShadowMapIDs[i], 0);
      m_shadowMapCS[0].SetTexture(lightAttenuationHandle, kBodyShadowmapTextureIDs[i], Texture2D.whiteTexture);
    }

    /* For enabled shadow bodies, set textures to their shadow maps. */
    for (int i = 0; i < shadowBodies; i++) {
      int enabledIndex = LightingRenderSettings.CloudShadowIndexToAtmosphereIndex(i);
      m_shadowMapCS[0].SetInt(kHasShadowMapIDs[enabledIndex], 1);
      m_shadowMapCS[0].SetTexture(lightAttenuationHandle, kBodyShadowmapTextureIDs[enabledIndex], m_framebuffers[kShadowMapFramebuffer[i]][0]);
    }

    using (new ProfilingScope(cmd, m_lightAttenuationSampler)) {
      m_shadowMapCS[0].SetTexture(lightAttenuationHandle, kLightAttenuationTexture, m_framebuffers[kLightAttenuationFramebuffer][0]);
      cmd.DispatchCompute(m_shadowMapCS[0], lightAttenuationHandle, 1, 1, 1);
    }
  }

  private void disableShadowMaps() {
    for (int i = 0; i < m_shadowMaps.Length; i++) {
      m_shadowMaps[i] = null;
    }
  }

  private void clearLightAttenuation() {
    clearToWhite(m_framebuffers[kLightAttenuationFramebuffer][0]);
  }

  private int chooseFullscreenKernel(int enabledLayers, bool compositeByHeight) {
    switch (enabledLayers) {
      case 0: return m_fullscreenCS.FindKernel(kFullscreen0Kernel);
      case 1: return m_fullscreenCS.FindKernel(kFullscreen1Kernel);
      case 2: return compositeByHeight ? m_fullscreenCS.FindKernel(kFullscreenSortedNKernel) : m_fullscreenCS.FindKernel(kFullscreen2Kernel);
      default: return compositeByHeight ? m_fullscreenCS.FindKernel(kFullscreenSortedNKernel) : m_fullscreenCS.FindKernel(kFullscreenNKernel);
    }
  }

  private void checkAndResizeFramebuffers(Vector2 screenSize) {
    Vector2Int newResolution = new Vector2Int((int) screenSize.x, (int) screenSize.y);
    Vector2Int currentResolution = new Vector2Int(m_framebuffers[kFullscreenLightingFramebuffer][0].rt.width,  m_framebuffers[kFullscreenLightingFramebuffer][0].rt.height);
    if (currentResolution != newResolution) {
      cleanupFullscreenFramebuffers();
      allocateFullscreenFramebuffers(newResolution);
      /* Signify that we must re-render the null result, if we have no layers. */
      m_prerenderedNullResult = false;
    }
  }

  private void checkAndResizeShadowMapFramebuffers(int numShadowMapBuffers, Vector2Int resolution) {
    if (m_numShadowMapBuffers != numShadowMapBuffers || m_shadowMapResolution != resolution) {
      cleanupShadowMapFramebuffers();
      allocateShadowMapFramebuffers(numShadowMapBuffers, resolution);
      m_shadowMapResolution = resolution;
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
    propertyBlock.SetTexture(shaderVariable, m_framebuffers[texture][0]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_framebuffers[texture][0]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture][0]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture][0]);
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
    return new Vector3(m_framebuffers[texture][0].rt.width, m_framebuffers[texture][0].rt.height, 1);
  }

  public static RTHandle getShadowMap(int bodyIndex) {
    return m_shadowMaps[bodyIndex];
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/



/******************************************************************************/
/************************** SHADER VARIABLE HANDLERS **************************/
/******************************************************************************/

  private void setFullscreenShaderVariables(BuiltinSkyParameters builtinParams, CommandBuffer cmd, ComputeShader cs, int handle) {
    cs.SetVector("_res", new Vector2(m_framebuffers[kFullscreenLightingFramebuffer][0].rt.width, m_framebuffers[kFullscreenLightingFramebuffer][0].rt.height));
    cmd.SetGlobalMatrix("_PixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix);
    cs.SetVector("_WorldSpaceCameraPos1", builtinParams.worldSpaceCameraPos);
    
    QualitySettingsBlock quality = QualityRenderSettings.Get();
    
    if (quality.m_compositeCloudsByHeight) {
      /* Build index buffer. */
      List<(double, int)> heightAndIndex = new List<(double, int)>();
      for (int i = 0; i < CloudLayerRenderSettings.GetLayerCount(); i++) {
        UniversalCloudLayer layer = CloudLayerRenderSettings.GetLayer(i);
        double height = layer.renderSettings.geometryHeight;
        /* Use y extent to compute height if this layer is 3d. */
        if (CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) layer.renderSettings.geometryType) == Datatypes.NoiseDimension.ThreeDimensional) {
          height = (layer.renderSettings.geometryYExtent.x + layer.renderSettings.geometryYExtent.y) / 2.0;
        }
        heightAndIndex.Add((height, i));
      }
      heightAndIndex = heightAndIndex.OrderBy(x => -x.Item1).ToList();
      int[] indices = new int[heightAndIndex.Count];
      for (int i = 0; i < indices.Length; i++) {
        indices[i] = heightAndIndex[i].Item2;
      }
      if (m_indexBuffer != null) {
        m_indexBuffer.Release();
      }
      m_indexBuffer = new ComputeBuffer(Mathf.Max(1, indices.Length), sizeof(int));
      m_indexBuffer.SetData(indices);
      cs.SetBuffer(handle, kSortedIndexBuffer, m_indexBuffer);
    }
  }

  private void setFullscreenTextures(CloudRenderer cloudRenderer, ComputeShader cs, int handle) {
    cloudRenderer.setTexture("cloudLighting",
      kLightingFramebufferTexture, cs, handle);
    cloudRenderer.setTexture("cloudTransmittanceAndHit",
      kTransmittanceHitFramebufferTexture, cs, handle);
    cloudRenderer.setTexture("cloudGeometry", kGBufferTexture, cs, handle);
    cs.SetInt("_layers", CloudLayerRenderSettings.GetLayerCount());
    cs.SetTexture(handle, kCompositeLightingFramebufferTexture, m_framebuffers[kFullscreenLightingFramebuffer][0]);
    cs.SetTexture(handle, kCompositeTransmittanceHitFramebufferTexture, m_framebuffers[kFullscreenTransmittanceAndHitFramebuffer][0]);
  }

  private void setShadowMapShaderVariables(CloudRenderer cloudRenderer, ComputeShader cs, int shadowBodyIndex) {
    cs.SetVector("_res", new Vector2(m_shadowMapResolution.x * kShadowMapCompositeResolutionFactor, m_shadowMapResolution.y * kShadowMapCompositeResolutionFactor));
    cs.SetInt("_layers", CloudLayerRenderSettings.GetShadowLayerCount()); 
    cs.SetInt("_blurRadius", 2); // HACK/TODO: make settable?
    cs.SetFloat("_resolutionScaleFactor", (float) kShadowMapCompositeResolutionFactor); // HACK/TODO: make settable?
  }

/******************************************************************************/
/************************ END SHADER VARIABLE HANDLERS ************************/
/******************************************************************************/

};

} // namespace Expanse
