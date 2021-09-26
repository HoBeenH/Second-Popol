using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse {

/**
 * @brief: renders clouds.
 */
public class CloudRenderer : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Framebuffers we render clouds to. We need 2 of each buffer so that
   * we can reproject from frame to frame. Framebuffer 0 is the
   * final framebuffer, and framebuffer 1 is the temp framebuffer. */
  private Dictionary<string, RTHandle[]> m_framebuffers = new Dictionary<string, RTHandle[]>();
  const string kLightingFramebuffer = "cloudLighting";
  const string kTransmittanceHitFramebuffer = "cloudTransmittanceAndHit";
  const string kGBuffer = "cloudGeometry";
  const string kMotionVectorsBuffer = "motionVectors";
  string[] kShadowFramebuffer = new string[CelestialBodyDatatypes.kMaxCelestialBodies];
  const string kReflectionFramebuffer = "cloudReflection";
  const string kReflectionTFramebuffer = "cloudReflectionT";
  const int kFinalFramebuffer = 0;
  const int kTempFramebuffer = 1;

  /* Profiling samplers. */
  ProfilingSampler[] m_gbufferProfilingSamplers = new ProfilingSampler[CloudDatatypes.kMaxCloudLayers];
  ProfilingSampler[] m_fullScreenProfilingSamplers = new ProfilingSampler[CloudDatatypes.kMaxCloudLayers];
  ProfilingSampler[] m_reprojectionProfilingSamplers = new ProfilingSampler[CloudDatatypes.kMaxCloudLayers];
  ProfilingSampler[] m_reflectionProfilingSamplers = new ProfilingSampler[CloudDatatypes.kMaxCloudLayers];
  ProfilingSampler[] m_shadowProfilingSamplers = new ProfilingSampler[CloudDatatypes.kMaxCloudLayers];
  ProfilingSampler m_motionVectorsProfilingSampler = new ProfilingSampler("Expanse: Copy Motion Vectors");

  /* Compute shader instances and settings buffers for each layer/body combo,
   * as well as associated kernels. */
  ComputeShader[,] m_CS = new ComputeShader[CloudDatatypes.kMaxCloudLayers, CelestialBodyDatatypes.kMaxCelestialBodies];
  const string kFullscreenKernel = "FULLSCREEN";
  const string kReprojectKernel = "REPROJECT";
  const string kShadowMapKernel = "SHADOWMAP";
  const string kReflectionKernel = "REFLECTION";

  /* Shader uniform variables. */
  const string kLightingFramebufferTexture = "_LightingFramebuffers";
  const string kTransmittanceHitFramebufferTexture = "_TransmittanceHitFramebuffers";
  const string kGBufferTexture = "_GBuffer";
  const string kReflectionTFramebufferTexture = "_ReflectionT";
  const string kMotionVectorTextureCopy = "_CameraMotionVectorsTexture_Copy";
  const string kPrevLightingFramebufferTexture = "_PrevLightingFramebuffers";
  const string kPrevTransmittanceHitFramebufferTexture = "_PrevTransmittanceHitFramebuffers";
  const string kShadowMapFramebufferTexture = "_ShadowMapFramebuffers";

  /* For tracking enabled layers and celestial bodies. */
  private int m_enabledLayers = (int) CloudDatatypes.kMaxCloudLayers;
  private int m_shadowLayers = 2;
  private int m_shadowBodies = 2;
  /* For tracking shadowmap resolution. */
  private Vector2Int m_shadowMapResolution = CloudDatatypes.cloudShadowMapQualityToResolution(Datatypes.Quality.Potato);

  /* Previous camera data for reprojection. */
  Matrix4x4 m_previousViewMatrix = Matrix4x4.identity;
  Matrix4x4 m_previousProjMatrix = Matrix4x4.identity;

  /* For keeping track of rendering subresolution. */
  Vector2 m_subresolution = new Vector2(1.0f, 1.0f);

  /* For rendering motion vectors. */
  Material m_motionVectorsMaterial;
  MaterialPropertyBlock m_motionVectorsPropertyBlock = new MaterialPropertyBlock();
  string kMotionVectorShaderPath = "Hidden/HDRP/Sky/Copy Motion Vectors";

  /* For rendering gameplay queries. */
  ComputeBuffer m_gameplayQueryBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GameplayQueries.QueryInfo)));
  const string kGameplayQueriesKernel = "GAMEPLAY";
  const string kGameplayQueryBuffer = "_GameplayQueryBuffer";
  ProfilingSampler m_gameplayProfilingSampler = new ProfilingSampler("Expanse: Process Gameplay Queries");

  /* For rendering reflection probes. */
  Vector2Int kReflectionProbeResolution = new Vector2Int(256, 256);
  int kReflectionReprojectionFrames = 2;
  int kReflectionDenoisingFrames = 32;

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    for (int i = 0; i < CelestialBodyDatatypes.kMaxCelestialBodies; i++) {
      kShadowFramebuffer[i] = "cloudShadow" + i;
    }

    /* Initialize the framebuffers to some default resolution. */
    m_framebuffers[kLightingFramebuffer] = new RTHandle[2];
    m_framebuffers[kTransmittanceHitFramebuffer] = new RTHandle[2];
    m_framebuffers[kGBuffer] = new RTHandle[1];
    m_framebuffers[kMotionVectorsBuffer] = new RTHandle[1];
    allocateFullscreenFramebuffers(new Vector2Int(64, 64), new Vector2Int(64, 64), 1);

    m_framebuffers[kReflectionFramebuffer] = new RTHandle[1];
    m_framebuffers[kReflectionTFramebuffer] = new RTHandle[1];
    allocateReflectionFramebuffers(kReflectionProbeResolution, 1);

    /* Initialize one shadow map by default, with one cloud layer enabled. */
    allocateShadowMapFramebuffers(m_shadowBodies, m_shadowLayers, m_shadowMapResolution);

    /* Initialize all profiling samplers, compute buffers, and compute shader
     * instances. */
    for (int i = 0; i < CloudDatatypes.kMaxCloudLayers; i++) {
      m_fullScreenProfilingSamplers[i] = new ProfilingSampler("Expanse: Render Cloud Layer " + i);
      m_gbufferProfilingSamplers[i] = new ProfilingSampler("Expanse: Render Cloud Layer GBuffer " + i);
      m_reprojectionProfilingSamplers[i] = new ProfilingSampler("Expanse: Reproject Cloud Layer " + i);
      m_reflectionProfilingSamplers[i] = new ProfilingSampler("Expanse: Reflection Probe Cloud Layer " + i);
      m_shadowProfilingSamplers[i] = new ProfilingSampler("Expanse: Render Cloud Shadow Map " + i);
    }
    buildComputeShaders();

    m_motionVectorsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find(kMotionVectorShaderPath));
  }

  public override void cleanup() {
    /* Destroy the framebuffers. */
    cleanupFullscreenFramebuffers();
    cleanupReflectionFramebuffers();
    cleanupShadowMapFramebuffers();
    m_framebuffers.Clear();

    m_gameplayQueryBuffer.Release();
    m_gameplayQueryBuffer = null;
  }

  private void allocateFullscreenFramebuffers(Vector2Int res, Vector2Int fullRes, int numLayers) {
    m_framebuffers[kLightingFramebuffer][0] = allocateRGBATexture2DArray("Cloud Lighting Framebuffers Final", res, numLayers);
    m_framebuffers[kLightingFramebuffer][1] = allocateRGBATexture2DArray("Cloud Lighting Framebuffers Temp", res, numLayers);
    m_framebuffers[kTransmittanceHitFramebuffer][0] = allocateRGBATexture2DArray("Cloud Transmittance And Hit Framebuffers Final", res, numLayers, useMipMap:true);
    m_framebuffers[kTransmittanceHitFramebuffer][1] = allocateRGBATexture2DArray("Cloud Transmittance And Hit Framebuffers Temp", res, numLayers, useMipMap:true);
    m_framebuffers[kGBuffer][0] = allocateRGBATexture2DArray("Cloud GBuffer", res, numLayers);
    m_framebuffers[kMotionVectorsBuffer][0] = allocateRGBATexture2D("Motion Vectors Copy", fullRes);
    m_enabledLayers = numLayers;
  }

  private void allocateReflectionFramebuffers(Vector2Int res, int numLayers) {
    m_framebuffers[kReflectionFramebuffer][0] = allocateRGBATexture2DArray("Cloud Reflection Framebuffers", res, numLayers);
    m_framebuffers[kReflectionTFramebuffer][0] = allocateMonochromeTexture2DArray("Cloud Reflection T Framebuffers", res, numLayers);
  }

  private void allocateShadowMapFramebuffers(int shadowBodies, int numLayers, Vector2Int resolution) {
    for (int i = 0; i < shadowBodies; i++) {
      m_framebuffers[kShadowFramebuffer[i]] = new RTHandle[1];
      m_framebuffers[kShadowFramebuffer[i]][0] = allocateRGBATexture2DArray("Cloud Shadowmap Framebuffer " + i, resolution, numLayers, useMipMap:true);
    }
    m_shadowBodies = shadowBodies;
    m_shadowLayers = numLayers;
  }

  private void cleanupFullscreenFramebuffers() {
    foreach (var rt in m_framebuffers[kLightingFramebuffer]) {
      if (rt != null) {
        RTHandles.Release(rt);
      }
    }
    foreach (var rt in m_framebuffers[kTransmittanceHitFramebuffer]) {
      if (rt != null) {
        RTHandles.Release(rt);
      }
    }
    RTHandles.Release(m_framebuffers[kGBuffer][0]);
    RTHandles.Release(m_framebuffers[kMotionVectorsBuffer][0]);
  }

  private void cleanupReflectionFramebuffers() {
    RTHandles.Release(m_framebuffers[kReflectionFramebuffer][0]);
    RTHandles.Release(m_framebuffers[kReflectionTFramebuffer][0]);
  }

  private void cleanupShadowMapFramebuffers() {
    for (int i = 0; i < m_shadowBodies; i++) {
      RTHandles.Release(m_framebuffers[kShadowFramebuffer[i]][0]);
      m_framebuffers.Remove(kShadowFramebuffer[i]);
    }
  }

  private void buildComputeShaders() {
    ComputeShader canonicalComputeShader = Resources.Load<ComputeShader>("CloudRenderer");
    for (int i = 0; i < CloudDatatypes.kMaxCloudLayers; i++) {
      for (int j = 0; j < CelestialBodyDatatypes.kMaxCelestialBodies; j++) {
        m_CS[i, j] = (ComputeShader) UnityEngine.Object.Instantiate(canonicalComputeShader);
      }
    }
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  /* Expects dependencies to be an array of cloud generators. */
  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    if (m_CS[0, 0] == null) {
      buildComputeShaders();
    }

    CommandBuffer cmd = builtinParams.commandBuffer;
    QualitySettingsBlock quality = QualityRenderSettings.Get();

    /* Identify the enabled layers and reallocate framebuffers if necessary. */
    Vector2 screenSize = builtinParams.screenSize.xy();
    checkAndResizeFullscreenFramebuffers(screenSize, quality.m_cloudSubresolution);

    GameplayQueries.BeginProcessing();
    if (CloudLayerRenderSettings.GetLayerCount() == 0) {
      /* No use rendering if we have no layers to render! */
      writeGameplayQueriesNullResult();
      return;
    }

    /* If we have some gameplay queries to process, copy them into our
     * gameplay queries compute buffer. */
    if (GameplayQueries.GetInProgressQueries().Count > 0) {
      copyGameplayQueriesToBuffer();
    }

    /* Before we render each layer, swap the framebuffers so we can use the
     * previous buffer as a history buffer when reprojecting. */
    RTHandle temp = m_framebuffers[kLightingFramebuffer][kFinalFramebuffer];
    m_framebuffers[kLightingFramebuffer][kFinalFramebuffer] = m_framebuffers[kLightingFramebuffer][kTempFramebuffer];
    m_framebuffers[kLightingFramebuffer][kTempFramebuffer] = temp;
    temp = m_framebuffers[kTransmittanceHitFramebuffer][kFinalFramebuffer];
    m_framebuffers[kTransmittanceHitFramebuffer][kFinalFramebuffer] = m_framebuffers[kTransmittanceHitFramebuffer][kTempFramebuffer];
    m_framebuffers[kTransmittanceHitFramebuffer][kTempFramebuffer] = temp;

    /* Copy motion vectors into texture we can access. */
    using (new ProfilingScope(cmd, m_motionVectorsProfilingSampler)) {
      CoreUtils.SetRenderTarget(cmd, m_framebuffers[kMotionVectorsBuffer][0].rt);
      CoreUtils.DrawFullScreen(cmd, m_motionVectorsMaterial, m_motionVectorsPropertyBlock, 0);
      CoreUtils.SetRenderTarget(cmd, builtinParams.colorBuffer, builtinParams.depthBuffer);
    }

    /* Set the common camera/pose settings. */
    cmd.SetGlobalMatrix("_PixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix);

    /* Loop over the layers and render each one to its dedicated framebuffer. */
    for (int i = 0; i < CloudLayerRenderSettings.GetLayerCount(); i++) {
      renderLayer(builtinParams, i, (CloudGenerator) dependencies[i], m_CS[i, 0]);
    }

    /* Now, identify which lights and which layers are set up to cast shadows,
     * and reallocate their framebuffers if necessary. */
    checkAndResizeShadowFramebuffers();
    for (int i = 0; i < CloudLayerRenderSettings.GetShadowLayerCount(); i++) {
      for (int j = 0; j < m_shadowBodies; j++) {
        /* Render shadow map! */
        int actualIndex = CloudLayerRenderSettings.GetShadowLayerIndex(i);
        renderLayerShadowMap(cmd, actualIndex, i, j, (CloudGenerator) dependencies[actualIndex], m_CS[actualIndex, j]);
      }
    }

    /* Generate mips for the final framebuffer if we've only got one enabled layer. */
    if (CloudLayerRenderSettings.GetLayerCount() == 1) {
      cmd.GenerateMips(m_framebuffers[kTransmittanceHitFramebuffer][kFinalFramebuffer]);
    }

    if (GameplayQueries.GetInProgressQueries().Count > 0) {
      /* Request back the gameplay query data and clear queries to indicate that 
       * they have been processed. */
      cmd.RequestAsyncReadback(m_gameplayQueryBuffer, onGameplayBufferReadback);
      GameplayQueries.EndProcessing();
    }

    /* Track the previous camera matrices. */
    m_previousViewMatrix = builtinParams.hdCamera.camera.worldToCameraMatrix;
    m_previousProjMatrix = builtinParams.hdCamera.camera.projectionMatrix;
  }

  /**
   * @brief: Renders a single cloud layer.
   * */
  private void renderLayer(BuiltinSkyParameters builtinParams,
    int index, CloudGenerator generator, ComputeShader cs) {
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Get the handles to the shaders kernels. */
    int handleFullscreen = cs.FindKernel(kFullscreenKernel);
    int handleReproject = cs.FindKernel(kReprojectKernel);
    int handleReflection = cs.FindKernel(kReflectionKernel);
    int handleGameplayQueries = cs.FindKernel(kGameplayQueriesKernel);

    /* Get the layer's universal representation. */
    UniversalCloudLayer layer = CloudLayerRenderSettings.GetLayer(index);

    /* Set the shader variables. */
    setFullscreenShaderVariables(cs, builtinParams, index);
    setNoiseTextures(cmd, generator, layer);

    /* Render the actual pixels we are allowed to this frame. */
    using (new ProfilingScope(cmd, m_fullScreenProfilingSamplers[index])) {
      cs.SetTexture(handleFullscreen, kLightingFramebufferTexture, m_framebuffers[kLightingFramebuffer][kFinalFramebuffer]);
      cs.SetTexture(handleFullscreen, kTransmittanceHitFramebufferTexture, m_framebuffers[kTransmittanceHitFramebuffer][kFinalFramebuffer]);
      cmd.DispatchCompute(cs, handleFullscreen, computeGroups(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.width / layer.renderSettings.reprojectionFrames, 8), computeGroups(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.height / layer.renderSettings.reprojectionFrames, 8), 1);
    }

    /* Reproject to compensate for the rest. This will also render this layer's
     * g-buffer for occlusion testing.*/
    if (layer.renderSettings.reprojectionFrames > 1 || layer.renderSettings.useTemporalDenoising > 0) {
      using (new ProfilingScope(cmd, m_reprojectionProfilingSamplers[index])) {
        cs.SetTexture(handleReproject, kMotionVectorTextureCopy, m_framebuffers[kMotionVectorsBuffer][0]);
        cs.SetTexture(handleReproject, kLightingFramebufferTexture, m_framebuffers[kLightingFramebuffer][kFinalFramebuffer]);
        cs.SetTexture(handleReproject, kTransmittanceHitFramebufferTexture, m_framebuffers[kTransmittanceHitFramebuffer][kFinalFramebuffer]);
        cs.SetTexture(handleReproject, kPrevLightingFramebufferTexture, m_framebuffers[kLightingFramebuffer][kTempFramebuffer]);
        cs.SetTexture(handleReproject, kPrevTransmittanceHitFramebufferTexture, m_framebuffers[kTransmittanceHitFramebuffer][kTempFramebuffer]);
        cs.SetTexture(handleReproject, kGBufferTexture, m_framebuffers[kGBuffer][0]);
        cmd.DispatchCompute(cs, handleReproject, computeGroups(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.width, 8), computeGroups(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.height, 8), 1);
      }
    }

    /* Render the reflection probe. */
    using (new ProfilingScope(cmd, m_reflectionProfilingSamplers[index])) {
      cs.SetVector("_reflectionProbeResolution", new Vector2((float) kReflectionProbeResolution.x, (float) kReflectionProbeResolution.y));
      cs.SetInt("_reflectionProbeReprojectionFrames", kReflectionReprojectionFrames);
      cs.SetInt("_reflectionProbeDenoisingFrames", kReflectionDenoisingFrames);
      cs.SetTexture(handleReflection, kLightingFramebufferTexture, m_framebuffers[kReflectionFramebuffer][0]);
      cs.SetTexture(handleReflection, kReflectionTFramebufferTexture, m_framebuffers[kReflectionTFramebuffer][0]);
      cmd.DispatchCompute(cs, handleReflection, computeGroups(m_framebuffers[kReflectionFramebuffer][0].rt.width / kReflectionReprojectionFrames, 8), computeGroups(m_framebuffers[kReflectionFramebuffer][0].rt.height / kReflectionReprojectionFrames, 8), 1);
    }

    /* Process any gameplay queries that we may have. */
    int gameplayQueriesCount = GameplayQueries.GetInProgressQueries().Count;
    // HACK: for now, this only works with 3D volumetric cloud layers.
    if (gameplayQueriesCount > 0 && CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) layer.renderSettings.geometryType) == Datatypes.NoiseDimension.ThreeDimensional) {
      using (new ProfilingScope(cmd, m_gameplayProfilingSampler)) {
        cs.SetBuffer(handleGameplayQueries, kGameplayQueryBuffer, m_gameplayQueryBuffer);
        cs.SetInt("_numGameplayQueries", gameplayQueriesCount);
        cmd.DispatchCompute(cs, handleGameplayQueries, computeGroups(gameplayQueriesCount, 8), 1, 1);
      }
    }
  }

  private void renderLayerShadowMap(CommandBuffer cmd, int layerIndex, int layerShadowIndex,
    int bodyShadowIndex, CloudGenerator generator, ComputeShader cs) {

    /* Set all the shader variables and textures */
    int handleShadowMap = cs.FindKernel(kShadowMapKernel);
    setShadowMapShaderVariables(cs, handleShadowMap, layerIndex, layerShadowIndex, bodyShadowIndex);
    setNoiseTextures(cmd, generator, CloudLayerRenderSettings.GetLayer(layerIndex));

    /* Dispatch the compute shader. */
    using (new ProfilingScope(cmd, m_shadowProfilingSamplers[layerShadowIndex])) {
      cs.SetTexture(handleShadowMap, kShadowMapFramebufferTexture, m_framebuffers[kShadowFramebuffer[bodyShadowIndex]][0]);
      cmd.DispatchCompute(cs, handleShadowMap, computeGroups(m_shadowMapResolution.x, 8), computeGroups(m_shadowMapResolution.y, 8), 1);
    }
  }

  private void checkAndResizeFullscreenFramebuffers(Vector2 screenSize, float subres) {
    /* If the number of layers has changed or the resolution is different, reallocate. */
    m_subresolution = new Vector2(Mathf.Floor(screenSize.x * subres) / screenSize.x, Mathf.Floor(screenSize.y * subres) / screenSize.y);
    Vector2Int newResolution = new Vector2Int((int) Mathf.Floor(screenSize.x * m_subresolution.x), (int) Mathf.Floor(screenSize.y * m_subresolution.y));
    Vector2Int currentResolution = new Vector2Int(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.width,  m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.height);
    if (CloudLayerRenderSettings.GetLayerCount() != m_enabledLayers || currentResolution != newResolution) {
      cleanupFullscreenFramebuffers();
      allocateFullscreenFramebuffers(newResolution, Utilities.ToInt2(screenSize), CloudLayerRenderSettings.GetLayerCount());
    }
  }

  /* Returns (List of shadow enabled layers, list of shadow enabled bodies) */
  private void checkAndResizeShadowFramebuffers() {
    QualitySettingsBlock quality = QualityRenderSettings.Get();
    /* Get the potentially new resolution. */
    Vector2Int newResolution = CloudDatatypes.cloudShadowMapQualityToResolution(quality.m_cloudShadowMapQuality);
    /* If anything has changed, reallocate. */
    if (m_shadowMapResolution != newResolution || m_shadowBodies != LightingRenderSettings.GetCloudShadowLights()
      || m_shadowLayers != CloudLayerRenderSettings.GetShadowLayerCount()) {
      cleanupShadowMapFramebuffers();
      allocateShadowMapFramebuffers(LightingRenderSettings.GetCloudShadowLights(), CloudLayerRenderSettings.GetShadowLayerCount(), newResolution);
      m_shadowMapResolution = newResolution;
    }
  }

/******************************************************************************/
/******************************* END RENDERING ********************************/
/******************************************************************************/



/******************************************************************************/
/***************************** GAMEPLAY QUERIES *******************************/
/******************************************************************************/

  private void writeGameplayQueriesNullResult() {
    List<GameplayQueries.QueryInfo> queries = GameplayQueries.GetInProgressQueries();
    List<Action<GameplayQueries.QueryInfo>> callbacks = GameplayQueries.GetInProgressCallbacks();
    for (int i = 0; i < queries.Count; i++) {
      GameplayQueries.QueryInfo q = queries[i];
      // Null result is visiblity = 1, density = 0.
      q.visibility = 1;
      q.density = 0;
      callbacks[i](q);
    }
    GameplayQueries.EndProcessing();
    GameplayQueries.ClearProcessed();
  }

  private void copyGameplayQueriesToBuffer() {
    List<GameplayQueries.QueryInfo> queries = GameplayQueries.GetInProgressQueries();
    // Reallocate compute buffer if query count has changed.
    if (m_gameplayQueryBuffer.count != queries.Count) {
      m_gameplayQueryBuffer.Release();
      m_gameplayQueryBuffer = new ComputeBuffer(queries.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GameplayQueries.QueryInfo)));
    }
    m_gameplayQueryBuffer.SetData(queries);
  }

  /* Callback for reading back gameplay queries to the CPU. Doing this
   * asynchronously avoids the pipeline stall that would otherwise occur. */
  private void onGameplayBufferReadback(AsyncGPUReadbackRequest r) {
    Unity.Collections.NativeArray<GameplayQueries.QueryInfo> queryBufferCPU = r.GetData<GameplayQueries.QueryInfo>();
    List<Action<GameplayQueries.QueryInfo>> callbacks = GameplayQueries.GetProcessedCallbacks();
    int completed = 0;
    for (int i = 0; i < queryBufferCPU.Length; i++) {
      // HACK: drop callbacks whose queries didn't get an answer. This tends to 
      // happen when a layer gets disabled mid-update.
      if (i >= callbacks.Count) {
        break;
      }
      GameplayQueries.QueryInfo q = queryBufferCPU[i];
      callbacks[i](q);
      completed++;
    }
    // Only clear however many queries we've successfully read back.
    GameplayQueries.ClearProcessed(completed);
  }

/******************************************************************************/
/*************************** END GAMEPLAY QUERIES *****************************/
/******************************************************************************/



/******************************************************************************/
/****************************** GETTERS/SETTERS *******************************/
/******************************************************************************/

  public override IReadOnlyCollection<string> getTextureNames() {
    return m_framebuffers.Keys;
  }

  public override void setTexture(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_framebuffers[texture][kFinalFramebuffer]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_framebuffers[texture][kFinalFramebuffer]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture][kFinalFramebuffer]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_framebuffers[texture][kFinalFramebuffer]);
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
    return new Vector3(m_framebuffers[texture][kFinalFramebuffer].rt.width, m_framebuffers[texture][kFinalFramebuffer].rt.height, m_framebuffers[texture][kFinalFramebuffer].rt.volumeDepth);
  }

  public Vector2Int getShadowmapResolution() {
    return m_shadowMapResolution;
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/



/******************************************************************************/
/************************** SHADER VARIABLE HANDLERS **************************/
/******************************************************************************/

  private void setFullscreenShaderVariables(ComputeShader cs, BuiltinSkyParameters builtinParams, int index) {
    cs.SetVector("_WorldSpaceCameraPos1", builtinParams.worldSpaceCameraPos);
    cs.SetMatrix("_previousViewMatrix", m_previousViewMatrix);
    cs.SetMatrix("_previousProjectionMatrix", m_previousProjMatrix);
    cs.SetVector("_framebufferResolution", new Vector2(m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.width, m_framebuffers[kLightingFramebuffer][kFinalFramebuffer].rt.height));
    cs.SetVector("_subresolution", m_subresolution);
    cs.SetInt("_layerIndex", index);
    cs.SetInt("_frameCount", Time.frameCount);
  }

  private void setShadowMapShaderVariables(ComputeShader cs, int handleShadowMap, int layerIndex, int layerShadowIndex, int bodyIndex) {
    cs.SetInt("_shadowLightIndex",  bodyIndex);
    cs.SetVector("_shadowMapResolution", new Vector2(m_shadowMapResolution.x, m_shadowMapResolution.y));
    cs.SetInt("_shadowLayerIndex", layerShadowIndex);
    cs.SetInt("_layerIndex", layerIndex);
    cs.SetInt("_frameCount", Time.frameCount);
  }

  private void setNoiseTextures(CommandBuffer cmd, CloudGenerator generator, UniversalCloudLayer layer) {
    setNoiseTexture(cmd, generator, layer, 0, "coverage", CloudDatatypes.CloudNoiseLayer.Coverage);
    setNoiseTexture(cmd, generator, layer, 1, "base", CloudDatatypes.CloudNoiseLayer.Base);
    setNoiseTexture(cmd, generator, layer, 2, "structure", CloudDatatypes.CloudNoiseLayer.Structure);
    setNoiseTexture(cmd, generator, layer, 3, "detail", CloudDatatypes.CloudNoiseLayer.Detail);
    setNoiseTexture(cmd, generator, layer, 4, "baseWarp", CloudDatatypes.CloudNoiseLayer.BaseWarp);
    setNoiseTexture(cmd, generator, layer, 5, "detailWarp", CloudDatatypes.CloudNoiseLayer.DetailWarp);
  }

  /* Helper for setting noise textures to make it more compact. Returns
   * tile factor. */
  private void setNoiseTexture(CommandBuffer cmd, CloudGenerator generator,
    UniversalCloudLayer layer, int noiseLayerIndex, string layerName, CloudDatatypes.CloudNoiseLayer layerType) {

    UniversalCloudLayer.UniversalCloudNoiseLayer noiseLayer = layer.noiseLayers[noiseLayerIndex];

    /*  Each of the layers is one of:
     *  -procedural, which always has a generated texture
     *  -authored, which may or may not have a static texture */
    Datatypes.NoiseDimension dimension = CloudDatatypes.cloudGeometryTypeToNoiseDimension((CloudDatatypes.CloudGeometryType) layer.renderSettings.geometryType);
    if (!noiseLayer.procedural) {
      /* Check the textures. */
      if (dimension == Datatypes.NoiseDimension.TwoDimensional || layerType == CloudDatatypes.CloudNoiseLayer.Coverage) {
        if (noiseLayer.noiseTexture != null) {
          cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.TwoDimensional), noiseLayer.noiseTexture);
        } else {
          cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.TwoDimensional), IRenderer.kDefaultTexture2D);
        }
        cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.ThreeDimensional), IRenderer.kDefaultTexture3D);
      } else {
        if (noiseLayer.noiseTexture != null) {
          cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.ThreeDimensional), noiseLayer.noiseTexture);
        } else {
          cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.ThreeDimensional), IRenderer.kDefaultTexture3D);
        }
        cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.TwoDimensional), IRenderer.kDefaultTexture2D);
      }
    } else {
      /* Use generator textures. */
      if (dimension == Datatypes.NoiseDimension.TwoDimensional || layerType == CloudDatatypes.CloudNoiseLayer.Coverage) {
        generator.setTexture(layerName, CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.TwoDimensional), cmd);
        cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.ThreeDimensional), IRenderer.kDefaultTexture3D);
      } else {
        generator.setTexture(layerName, CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.ThreeDimensional), cmd);
        cmd.SetGlobalTexture(CloudDatatypes.cloudNoiseLayerTypeToShaderID(layerType, Datatypes.NoiseDimension.TwoDimensional), IRenderer.kDefaultTexture2D);
      }
    }
  }

/******************************************************************************/
/************************ END SHADER VARIABLE HANDLERS ************************/
/******************************************************************************/

};

} // namespace Expanse
