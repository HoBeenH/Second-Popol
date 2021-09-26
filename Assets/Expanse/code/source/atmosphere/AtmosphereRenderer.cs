using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse {

/**
 * @brief: renders atmosphere lookup tables and the atmosphere itself.
 */ //
public class AtmosphereRenderer : IRenderer {

/******************************************************************************/
/****************************** MEMBER VARIABLES ******************************/
/******************************************************************************/

  /* Framebuffers---managed by an internal class to keep things clean. */
  AtmosphereRenderTextures m_renderTextures = new AtmosphereRenderTextures();

  /* CPU-side copy of light transmittance texture, for altering directional
   * light colors. */
  private Texture2D m_lightTransmittancesCPU = new Texture2D((int) CelestialBodyDatatypes.kMaxCelestialBodies, 1, TextureFormat.RGBAFloat, false);
  /* This has to be a static global array so that we can easily access it
   * from the light control script. */
  public static Color[] m_bodyTransmittances = new Color[CelestialBodyDatatypes.kMaxCelestialBodies];

  /* Downsampled depth buffer for screenspace layers. */
  float m_depthDownsampleFactor = 0.5f;
  private RTHandle m_mipMappedDepthBuffer = null;
  private RTHandle m_downsampledDepthBuffer = null;

  /* Compute buffers for passing settings to the shaders. */
  ComputeBuffer m_settingsBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtmosphereSettings)));
  const string kSettingsBuffer = "_atmosphereSettingsBuffer";
  AtmosphereSettings[] m_atmosphereSettings = new AtmosphereSettings[1];

  /* Profiling samplers. */
  ProfilingSampler m_precomputeTProfilingSampler = new ProfilingSampler("Expanse: Precompute Transmittance");
  ProfilingSampler m_sampleTLightsProfilingSampler = new ProfilingSampler("Expanse: Sample Transmittance For Lights");
  ProfilingSampler m_compositeMSProfilingSampler = new ProfilingSampler("Expanse: Composite Multiple Scattering");
  ProfilingSampler m_precomputeMSProfilingSampler = new ProfilingSampler("Expanse: Precompute Multiple Scattering");
  ProfilingSampler m_renderSkyViewProfilingSampler = new ProfilingSampler("Expanse: Render Atmosphere");
  ProfilingSampler m_renderAPProfilingSampler = new ProfilingSampler("Expanse: Render Aerial Perspective");
  ProfilingSampler m_renderScreenspaceProfilingSampler = new ProfilingSampler("Expanse: Render Screenspace Volumetrics");
  ProfilingSampler m_denoiseScreenspaceProfilingSampler = new ProfilingSampler("Expanse: Denoise Screenspace Volumetrics");

  /* Compute shader and associated kernels. */
  ComputeShader m_CS = Resources.Load<ComputeShader>("AtmosphereRenderer");
  const string kTKernel = "T";
  const string kTLightsKernel = "TLIGHTS";
  const string kCompositeMSKernel = "COMPOSITEMS";
  const string kMSKernel = "MS";
  const string kSkyViewKernel = "SKYVIEW";
  const string kAPKernel = "AP";
  const string kCopyDepthKernel = "COPYDEPTH";
  const string kBlurDepthKernel = "BLURDEPTH";
  const string kScreenspaceKernel = "SCREENSPACE";
  const string kDenoiseScreenspaceKernel = "DENOISE_SCREENSPACE";
  int m_handleT = 0;
  int m_handleTLights = 0;
  int m_handleCompositeMS = 0;
  int m_handleMS = 0;
  int m_handleSkyView = 0;
  int m_handleAP = 0;
  int m_handleCopyDepth = 0;
  int m_handleBlurDepth = 0;
  int m_handleScreenspace = 0;
  int m_handleDenoiseScreenspace = 0;

  /* Shader variables. */
  const string kTTexture = "_T";
  const string kTLightsTexture = "_TLights";
  const string kMSTexture = "_MS";
  const string kMSMultithreadedTexture = "_MSMultithreaded";
  const string kSkyViewTexture = "_SkyView";
  const string kAPTexture = "_AP";
  const string kDownsampleDepthTexture = "_DownsampledDepth";
  const string kScreenspaceTexture = "_ScreenSpace";
  const string kScreenspaceHistoryTexture = "_ScreenSpaceHistory";
  const string kTReadOnlyTexture = "_TTex";
  const string kMSReadOnlyTexture = "_MSTex";
  const string kSkyViewReadOnlyTexture = "_SkyViewTex";
  const string kTLightsReadOnlyTexture = "_TLightsTex";
  const string kDownsampleDepthReadOnlyTexture = "_DownsampledDepthTex";
  const string kCloudTransmittanceTexture = "_CloudTransmittance";
  const string kCloudTransmittanceArrayTexture = "_CloudTransmittanceArray";
  const string kLightAttenuationTexture = "_CloudLightAttenuation";

  /* Since render() gets called when the screen size is set to the cubemap
   * screen size, we need to manually set the screen size from
   * ExpanseRenderer's render call. */
  private Vector2 m_screenSize = new Vector2(512, 512);

  /* The MS texture is small, but requires a large number of samples per pixel
   * to get right. Each sample is independent though, so we can split
   * the sampling strategy into multiple groups that run in parallel. */
  const int ksqrtMSParallelSampleGroups = 2;

  /* For keeping track of enabled screenspace layers. */
  int m_nullScreenspaceResultPrerendered = 0;

  /* Previous camera data for reprojection. */
  Matrix4x4 m_previousViewMatrix = Matrix4x4.identity;
  Matrix4x4 m_previousProjMatrix = Matrix4x4.identity;

/******************************************************************************/
/**************************** END MEMBER VARIABLES ****************************/
/******************************************************************************/


/******************************************************************************/
/***************************** SHADER PROPERTIES ******************************/
/******************************************************************************/

  private static readonly int kPreviousViewMatrixID = Shader.PropertyToID("_previousViewMatrix");
  private static readonly int kPreviousProjectionMatrixID = Shader.PropertyToID("_previousProjectionMatrix");
  private static readonly int kPixelCoordToViewDirWSID = Shader.PropertyToID("_PixelCoordToViewDirWS");
  private static readonly int kPixelCoordToViewDirWS_ManualSetID = Shader.PropertyToID("_PixelCoordToViewDirWS_ManualSet");
  private static readonly int kWorldSpaceCameraPos1ID = Shader.PropertyToID("_WorldSpaceCameraPos1");
  private static readonly int kSettingsBufferID = Shader.PropertyToID(kSettingsBuffer);

/******************************************************************************/
/*************************** END SHADER PROPERTIES ****************************/
/******************************************************************************/


/******************************************************************************/
/**************************** CONSTRUCTION/CLEANUP ****************************/
/******************************************************************************/

  public override void build() {
    /* Allocate default depth buffer textures. */
    if (m_mipMappedDepthBuffer != null) {
      RTHandles.Release(m_mipMappedDepthBuffer);
    }
    if (m_downsampledDepthBuffer != null) {
      RTHandles.Release(m_downsampledDepthBuffer);
    }
    m_mipMappedDepthBuffer = allocateRGBATexture2D("Atmosphere Mip-Mapped Depth Buffer", Utilities.ToInt2(m_screenSize), useMipMap: true);
    m_downsampledDepthBuffer = allocateRGBATexture2D("Atmosphere Downsampled Depth Buffer", Utilities.ToInt2(m_depthDownsampleFactor * m_screenSize), useMipMap: false);

    /* Look up kernel handles. */
    m_handleT = m_CS.FindKernel(kTKernel);
    m_handleTLights = m_CS.FindKernel(kTLightsKernel);
    m_handleCompositeMS = m_CS.FindKernel(kCompositeMSKernel);
    m_handleMS = m_CS.FindKernel(kMSKernel);
    m_handleSkyView = m_CS.FindKernel(kSkyViewKernel);
    m_handleAP = m_CS.FindKernel(kAPKernel);
    m_handleCopyDepth = m_CS.FindKernel(kCopyDepthKernel);
    m_handleBlurDepth = m_CS.FindKernel(kBlurDepthKernel);
    m_handleScreenspace = m_CS.FindKernel(kScreenspaceKernel);
    m_handleDenoiseScreenspace = m_CS.FindKernel(kDenoiseScreenspaceKernel);
  }

  public override void cleanup() {
    m_renderTextures.cleanup();

    m_settingsBuffer.Release();
    m_settingsBuffer = null;

    if (m_downsampledDepthBuffer != null) {
      RTHandles.Release(m_downsampledDepthBuffer);
      m_downsampledDepthBuffer = null;
    }
    if (m_mipMappedDepthBuffer != null) {
      RTHandles.Release(m_mipMappedDepthBuffer);
      m_mipMappedDepthBuffer = null;
    }
  }

/******************************************************************************/
/************************** END CONSTRUCTION/CLEANUP **************************/
/******************************************************************************/



/******************************************************************************/
/********************************** RENDERING *********************************/
/******************************************************************************/

  /* Expects dependencies to be {CloudRenderer, CloudCompositor}. */
  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;
    CloudRenderer cloudRenderer = (CloudRenderer) dependencies[0];
    CloudCompositor cloudCompositor = (CloudCompositor) dependencies[1];

    /* Get quality settings. */
    QualitySettingsBlock quality = QualityRenderSettings.Get();

    /* Resize our render textures if necessary. */
    m_renderTextures.checkAndResize(quality.m_atmosphereTextureQuality, quality.m_screenspaceFogQuality, m_screenSize);

    /* Set the relevant properties. */
    setShaderVariables(builtinParams);
    setGlobalBuffers(builtinParams, CloudLayerRenderSettings.GetLayerCount());

    /* Compute the transmittance LUT. */
    using (new ProfilingScope(cmd, m_precomputeTProfilingSampler)) {
      m_CS.SetTexture(m_handleT, kTTexture, m_renderTextures.m_framebuffers["T"]);
      cmd.DispatchCompute(m_CS, m_handleT, computeGroups(m_renderTextures.res.T.x, 8), computeGroups(m_renderTextures.res.T.y, 8), 1);
      cmd.SetGlobalTexture(kTReadOnlyTexture, m_renderTextures.m_framebuffers["T"]);
    }

    /* Render transmittances for lights---only do this for the main camera and editor camera. */
    bool renderLightTransmittances = builtinParams.hdCamera.camera.CompareTag("MainCamera");
#if UNITY_EDITOR
    // Check if we are the scene view camera.
    if (UnityEditor.SceneView.currentDrawingSceneView != null) {
      renderLightTransmittances = renderLightTransmittances || (UnityEditor.SceneView.currentDrawingSceneView.camera == builtinParams.hdCamera.camera);
    }
#endif
    if (renderLightTransmittances) {
      using (new ProfilingScope(cmd, m_sampleTLightsProfilingSampler)) {
        m_CS.SetTexture(m_handleTLights, kTLightsTexture, m_renderTextures.m_framebuffers["TLights"]);
        cmd.DispatchCompute(m_CS, m_handleTLights, computeGroups((int) CelestialBodyDatatypes.kMaxCelestialBodies, 4), 1, 1);
        cmd.SetGlobalTexture(kTLightsReadOnlyTexture, m_renderTextures.m_framebuffers["TLights"]);
        cmd.RequestAsyncReadback(m_renderTextures.m_framebuffers["TLights"], 0, TextureFormat.RGBAHalf, onTLightsReadback);
      }
    }

    /* Compute the multiple scattering LUT. */
    using (new ProfilingScope(cmd, m_precomputeMSProfilingSampler)) {
      m_CS.SetTexture(m_handleMS, kMSMultithreadedTexture, m_renderTextures.m_framebuffers["MSMultithreaded"]);
      cmd.DispatchCompute(m_CS, m_handleMS, computeGroups(ksqrtMSParallelSampleGroups * m_renderTextures.res.MS.x, 8), computeGroups(ksqrtMSParallelSampleGroups * m_renderTextures.res.MS.y, 8), 1);
    }

    /* Composite the multi-threaded results. */
    using (new ProfilingScope(cmd, m_compositeMSProfilingSampler)) {
      m_CS.SetTexture(m_handleCompositeMS, kMSMultithreadedTexture, m_renderTextures.m_framebuffers["MSMultithreaded"]);
      m_CS.SetTexture(m_handleCompositeMS, kMSTexture, m_renderTextures.m_framebuffers["MS"]);
      cmd.DispatchCompute(m_CS, m_handleCompositeMS, computeGroups(m_renderTextures.res.MS.x, 8), computeGroups(m_renderTextures.res.MS.y, 8), 1);
      cmd.SetGlobalTexture(kMSReadOnlyTexture, m_renderTextures.m_framebuffers["MS"]);
    }

    /* Compute the sky view LUT. */
    using (new ProfilingScope(cmd, m_renderSkyViewProfilingSampler)) {
      m_CS.SetTexture(m_handleSkyView, kSkyViewTexture, m_renderTextures.m_framebuffers["skyView"]);
      cmd.DispatchCompute(m_CS, m_handleSkyView, computeGroups(m_renderTextures.res.skyView.x, 8), computeGroups(m_renderTextures.res.skyView.y, 8), 1);
      cmd.SetGlobalTexture(kSkyViewReadOnlyTexture, m_renderTextures.m_framebuffers["skyView"]);
    }

    /* Compute the aerial perspective LUT. */
    using (new ProfilingScope(cmd, m_renderAPProfilingSampler)) {
      cloudCompositor.setTexture("lightAttenuation", kLightAttenuationTexture, cmd);
      m_CS.SetTexture(m_handleAP, kAPTexture, m_renderTextures.m_framebuffers["AP"]);
      cmd.DispatchCompute(m_CS, m_handleAP, computeGroups(m_renderTextures.res.AP.x, 4), computeGroups(m_renderTextures.res.AP.y, 4), computeGroups(m_renderTextures.res.AP.z, 8));
    }

    /* Bind global aerial perspective LUT. */
    cmd.SetGlobalTexture("_EXPANSE_AERIAL_PERPSECTIVE", m_renderTextures.m_framebuffers["AP"]);
  }

  /* Expects dependencies to be {CloudRenderer, CloudCompositor}. */
  public void renderScreenspaceVolumetrics(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    /* Only render the null result if we haven't rendered it already. HACK: for some reason, we
     * need to render it at least 3 times. Render it 8 times to be safe. */
    if (AtmosphereLayerRenderSettings.GetFogLayers() == 0 && m_nullScreenspaceResultPrerendered >= 8) {
      return;
    }

    /* Get quality settings. */
    QualitySettingsBlock quality = QualityRenderSettings.Get();

    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    setShaderVariables(builtinParams);
    checkAndResizeDownsampledDepthBuffer(1.0f / Mathf.Pow(2, quality.m_screenspaceDepthDownscale - 1));

    /* All the shader variables should still be set from the update call.
     * Copy depth buffer, generate mips, and blur it to avoid artifacts. */
    if (AtmosphereLayerRenderSettings.GetFogLayersUsingDepth() > 0) {
      using (new ProfilingScope(cmd, m_renderScreenspaceProfilingSampler)) {
        m_CS.SetTexture(m_handleCopyDepth, kDownsampleDepthTexture, m_mipMappedDepthBuffer);
        cmd.DispatchCompute(m_CS, m_handleCopyDepth, computeGroups((int) m_screenSize.x, 8), computeGroups((int) m_screenSize.y, 8), 1);
        cmd.GenerateMips(m_mipMappedDepthBuffer);
        cmd.SetGlobalTexture(kDownsampleDepthReadOnlyTexture, m_mipMappedDepthBuffer);
        m_CS.SetTexture(m_handleBlurDepth, kDownsampleDepthTexture, m_downsampledDepthBuffer);
        cmd.DispatchCompute(m_CS, m_handleBlurDepth, computeGroups(m_downsampledDepthBuffer.rt.width, 8), computeGroups(m_downsampledDepthBuffer.rt.height, 8), 1);
      }
    }

    /* Swap current with history buffer. */
    RTHandle temp = m_renderTextures.m_framebuffers["screenspace"];
    m_renderTextures.m_framebuffers["screenspace"] = m_renderTextures.m_framebuffers["screenspaceHistory"];
    m_renderTextures.m_framebuffers["screenspaceHistory"] = temp;

    /* Compute the screenspace volumetrics. */
    CloudRenderer cloudRenderer = (CloudRenderer) dependencies[0];
    CloudCompositor cloudCompositor = (CloudCompositor) dependencies[1];
    using (new ProfilingScope(cmd, m_renderScreenspaceProfilingSampler)) {
      cmd.SetGlobalTexture(kDownsampleDepthReadOnlyTexture, m_downsampledDepthBuffer);
      cloudRenderer.setTexture("cloudTransmittanceAndHit", kCloudTransmittanceArrayTexture, m_CS, m_handleScreenspace);
      cloudCompositor.setTexture("fullscreenTransmittanceAndHit", kCloudTransmittanceTexture, m_CS, m_handleScreenspace);
      m_CS.SetTexture(m_handleScreenspace, kScreenspaceTexture, m_renderTextures.m_framebuffers["screenspace"]);
      cmd.DispatchCompute(m_CS, m_handleScreenspace, computeGroups(m_renderTextures.res.screenspace.x, 16), computeGroups(m_renderTextures.res.screenspace.y, 8), computeGroups(m_renderTextures.res.screenspace.z, 1));
    }

    /* Denoise the screenspace volumetrics. */
    if (quality.m_fogUseTemporalDenoising && AtmosphereLayerRenderSettings.GetFogLayers() > 0) {
      using (new ProfilingScope(cmd, m_denoiseScreenspaceProfilingSampler)) {
        m_CS.SetMatrix(kPreviousViewMatrixID, m_previousViewMatrix);
        m_CS.SetMatrix(kPreviousProjectionMatrixID, m_previousProjMatrix);
        m_CS.SetTexture(m_handleDenoiseScreenspace, kScreenspaceTexture, m_renderTextures.m_framebuffers["screenspace"]);
        m_CS.SetTexture(m_handleDenoiseScreenspace, kScreenspaceHistoryTexture, m_renderTextures.m_framebuffers["screenspaceHistory"]);
        cmd.DispatchCompute(m_CS, m_handleDenoiseScreenspace, computeGroups(m_renderTextures.res.screenspace.x, 16), computeGroups(m_renderTextures.res.screenspace.y, 8), computeGroups(m_renderTextures.res.screenspace.z, 1));
      }
    }

    /* Bind global fog LUT + depth skew. */
    cmd.SetGlobalTexture("_EXPANSE_FOG", m_renderTextures.m_framebuffers["screenspace"]);

    if (AtmosphereLayerRenderSettings.GetFogLayers() == 0) {
      m_nullScreenspaceResultPrerendered++;
    } else {
      m_nullScreenspaceResultPrerendered = 0;
    }

    /* Track the previous camera matrices. */
    m_previousViewMatrix = builtinParams.hdCamera.camera.worldToCameraMatrix;
    m_previousProjMatrix = builtinParams.hdCamera.camera.projectionMatrix;
  }

  /* Resizes depth buffer according to a potentially new depth downsample
   * factor. */
  private void checkAndResizeDownsampledDepthBuffer(float newDepthDownsampleFactor) {
    if (m_depthDownsampleFactor != newDepthDownsampleFactor) {
      m_depthDownsampleFactor = newDepthDownsampleFactor;
      // Make sure to release old buffers.
      RTHandles.Release(m_mipMappedDepthBuffer);
      RTHandles.Release(m_downsampledDepthBuffer);
      m_mipMappedDepthBuffer = null;
      m_downsampledDepthBuffer = null;
      // Create new buffers.
      m_mipMappedDepthBuffer = allocateRGBATexture2D("Atmosphere Mip-Mapped Depth Buffer", Utilities.ToInt2(m_screenSize), useMipMap: true);
      m_downsampledDepthBuffer = allocateRGBATexture2D("Atmosphere Downsampled Depth Buffer", Utilities.ToInt2(m_depthDownsampleFactor * m_screenSize), useMipMap: false);
    }
  }

  /* Callback for reading back TLights to the CPU. Doing this asynchronously
   * means we don't have to block the entire main thread to wait for the
   * readback to finish. */
  private byte[] onTLightsReadbackTempBuffer = new byte[]{0, 0, 0, 0};
  private void onTLightsReadback(AsyncGPUReadbackRequest tLightsRequest) {
    // Clear temp buffer.
    for (int i = 0; i < onTLightsReadbackTempBuffer.Length; i++) {
      onTLightsReadbackTempBuffer[i] = 0;
    }

    Unity.Collections.NativeArray<byte> tLightsCPU = tLightsRequest.GetData<byte>();
    for (int i = 0; i < m_bodyTransmittances.Length; i++) {
      int offset = i * 8;

      onTLightsReadbackTempBuffer[0] = tLightsCPU[offset];
      onTLightsReadbackTempBuffer[1] = tLightsCPU[offset+1];
      float r = Utilities.toTwoByteFloatMemoryOptimized(onTLightsReadbackTempBuffer);

      onTLightsReadbackTempBuffer[0] = tLightsCPU[offset+2];
      onTLightsReadbackTempBuffer[1] = tLightsCPU[offset+3];
      float g = Utilities.toTwoByteFloatMemoryOptimized(onTLightsReadbackTempBuffer);

      onTLightsReadbackTempBuffer[0] = tLightsCPU[offset+4];
      onTLightsReadbackTempBuffer[1] = tLightsCPU[offset+5];
      float b = Utilities.toTwoByteFloatMemoryOptimized(onTLightsReadbackTempBuffer);

      m_bodyTransmittances[i].r = r;
      m_bodyTransmittances[i].g = g;
      m_bodyTransmittances[i].b = b;
      m_bodyTransmittances[i].a = 1;
    }
  }

/******************************************************************************/
/******************************* END RENDERING ********************************/
/******************************************************************************/



/******************************************************************************/
/****************************** GETTERS/SETTERS *******************************/
/******************************************************************************/

  public static Color GetBodyTransmittance(int bodyIndex) {
    return m_bodyTransmittances[bodyIndex];
  }

  /* Also reallocates downsampled depth buffer. */
  public void setScreenSize(Vector2 screenSize) {
    if (m_screenSize != screenSize) {
      m_screenSize = screenSize;
      // Make sure to release old buffers.
      RTHandles.Release(m_mipMappedDepthBuffer);
      RTHandles.Release(m_downsampledDepthBuffer);
      m_mipMappedDepthBuffer = null;
      m_downsampledDepthBuffer = null;
      // Create new buffers.
      m_mipMappedDepthBuffer = allocateRGBATexture2D("Atmosphere Mip-Mapped Depth Buffer", Utilities.ToInt2(m_screenSize), useMipMap: true);
      m_downsampledDepthBuffer = allocateRGBATexture2D("Atmosphere Downsampled Depth Buffer", Utilities.ToInt2(m_depthDownsampleFactor * m_screenSize), useMipMap: false);
    }
  }

  public override IReadOnlyCollection<string> getTextureNames() {
    return m_renderTextures.m_framebuffers.Keys;
  }

  public override void setTexture(string texture, string shaderVariable,
    MaterialPropertyBlock propertyBlock) {
    propertyBlock.SetTexture(shaderVariable, m_renderTextures.m_framebuffers[texture]);
  }

  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {
    computeShader.SetTexture(kernelHandle, shaderVariable, m_renderTextures.m_framebuffers[texture]);
  }

  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_renderTextures.m_framebuffers[texture]);
  }

  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {
    cmd.SetGlobalTexture(shaderVariable, m_renderTextures.m_framebuffers[texture]);
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
    RTHandle tex = m_renderTextures.m_framebuffers[texture];
    return new Vector3(tex.rt.width, tex.rt.height, tex.rt.volumeDepth);
  }

/******************************************************************************/
/**************************** END GETTERS/SETTERS *****************************/
/******************************************************************************/



/******************************************************************************/
/**************************** RENDER TEXTURE CLASS ****************************/
/******************************************************************************/

  /**
   * @brief: Internal class for managing render textures.
   * */
  private class AtmosphereRenderTextures {
    public struct AtmosphereRenderTextureResolutions {
      public Vector2Int T;
      public Vector2Int MS;
      public Vector2Int skyView;
      public Vector3Int AP;
      public Vector3Int screenspace;
      /* Static equals function that requires no additional memory. */
      public static bool areEqual(AtmosphereRenderTextureResolutions a, AtmosphereRenderTextureResolutions b) {
        return a.T == b.T && a.MS == b.MS && a.skyView == b.skyView && a.AP == b.AP && a.screenspace == b.screenspace;
      }
    };

    public AtmosphereRenderTextureResolutions res;
    public Dictionary<string, RTHandle> m_framebuffers = new Dictionary<string, RTHandle>();

    public AtmosphereRenderTextures() {
      res = qualityToResolution(Datatypes.Quality.Potato, Datatypes.Quality.Potato);
      rebuildTextures();
    }

    public void cleanup() {
      foreach (var rt in m_framebuffers) {
        RTHandles.Release(rt.Value);
      }
    }

    public void checkAndResize(Datatypes.Quality atmosphereQuality, Datatypes.Quality screenspaceQuality, Vector2 screenSize) {
      AtmosphereRenderTextureResolutions newRes = qualityToResolution(atmosphereQuality, screenspaceQuality);
      if (!AtmosphereRenderTextureResolutions.areEqual(res, newRes)) {
        cleanup();
        res = newRes;
        rebuildTextures();
      }
    }

    private void rebuildTextures() {
      m_framebuffers["T"] = allocateRGBATexture2D("Atmosphere Transmittance LUT", res.T);
      m_framebuffers["TLights"] = allocateRGBATexture2D("Atmosphere Light Transmittance LUT", new Vector2Int((int) CelestialBodyDatatypes.kMaxCelestialBodies, 1));
      m_framebuffers["MS"] = allocateRGBATexture2D("Atmosphere Multiple Scattering LUT", res.MS);
      m_framebuffers["MSMultithreaded"] = allocateRGBATexture2D("Multithreaded Atmosphere Multiple Scattering LUT", res.MS * ksqrtMSParallelSampleGroups);
      m_framebuffers["skyView"] = allocateRGBATexture2D("Atmosphere Sky View LUT", res.skyView);
      m_framebuffers["AP"] = allocateRGBATexture3D("Atmosphere Aerial Perspective LUT", res.AP);
      m_framebuffers["screenspace"] = allocateRGBATexture3D("Atmosphere Screenspace Volumetrics Framebuffer", res.screenspace);
      m_framebuffers["screenspaceHistory"] = allocateRGBATexture3D("Atmosphere Screenspace Volumetrics History Framebuffer", res.screenspace);
    }

    public static AtmosphereRenderTextureResolutions qualityToResolution(Datatypes.Quality atmosphereQuality, Datatypes.Quality screenspaceQuality) {
      AtmosphereRenderTextureResolutions res = new AtmosphereRenderTextureResolutions();
      switch (atmosphereQuality) {
        case Datatypes.Quality.Potato: {
          /* I don't think you can get more optimized than this without
           * looking so bad it would be impossible to put in a game. */
          res.T = new Vector2Int(8, 32);
          res.MS = new Vector2Int(8, 8);
          res.skyView = new Vector2Int(48, 24);
          res.AP = new Vector3Int(16, 16, 8);
          break;
        }
        case Datatypes.Quality.Low: {
          res.T = new Vector2Int(64, 128);
          res.MS = new Vector2Int(8, 8);
          res.skyView = new Vector2Int(96, 32);
          res.AP = new Vector3Int(16, 16, 8);
          break;
        }
        case Datatypes.Quality.Medium: {
          res.T = new Vector2Int(64, 256);
          res.MS = new Vector2Int(16, 16);
          res.skyView = new Vector2Int(128, 64);
          res.AP = new Vector3Int(32, 32, 16);
          break;
        }
        case Datatypes.Quality.High: {
          res.T = new Vector2Int(64, 256);
          res.MS = new Vector2Int(32, 32);
          res.skyView = new Vector2Int(192, 64);
          res.AP = new Vector3Int(32, 32, 16);
          break;
        }
        case Datatypes.Quality.Ultra: {
          res.T = new Vector2Int(128, 512);
          res.MS = new Vector2Int(32, 32);
          res.skyView = new Vector2Int(256, 64);
          res.AP = new Vector3Int(64, 64, 16);
          break;
        }
        case Datatypes.Quality.RippingThroughTheMetaverse: {
          res.T = new Vector2Int(256, 1024);
          res.MS = new Vector2Int(64, 64);
          res.skyView = new Vector2Int(512, 128);
          res.AP = new Vector3Int(64, 64, 16);
          break;
        }
        default: {
          /* To be safe, uses potato quality. */
          res.T = new Vector2Int(8, 32);
          res.MS = new Vector2Int(8, 8);
          res.skyView = new Vector2Int(48, 24);
          res.AP = new Vector3Int(16, 16, 8);
          break;
        }
      }
      switch (screenspaceQuality) {
        case Datatypes.Quality.Potato: {
          res.screenspace = new Vector3Int(114, 64, 16);
          break;
        }
        case Datatypes.Quality.Low: {
          res.screenspace = new Vector3Int(114, 64, 32);
          break;
        }
        case Datatypes.Quality.Medium: {
          res.screenspace = new Vector3Int(228, 128, 32);
          break;
        }
        case Datatypes.Quality.High: {
          res.screenspace = new Vector3Int(228, 128, 64);
          break;
        }
        case Datatypes.Quality.Ultra: {
          res.screenspace = new Vector3Int(456, 256, 64);
          break;
        }
        case Datatypes.Quality.RippingThroughTheMetaverse: {
          res.screenspace = new Vector3Int(456, 256, 64);
          break;
        }
        default: {
          /* To be safe, uses potato quality. */
          res.screenspace = new Vector3Int(114, 64, 16);
          break;
        }
      }
      return res;
    }
  };

/******************************************************************************/
/************************** END RENDER TEXTURE CLASS **************************/
/******************************************************************************/



/******************************************************************************/
/************************** SHADER VARIABLE HANDLERS **************************/
/******************************************************************************/

  [GenerateHLSL(needAccessors=false)]
  private struct AtmosphereSettings {
    public int frameCount;
    /* Framebuffer resolution for aerial perspective. */
    public Vector2 screenSize;
    public float farClip;
    /* Texture resolutions. */
    public Vector2 resT;
    public Vector2 resMS;
    public Vector2 resSkyView;
    public Vector3 resAP;
    public Vector3 resScreenspace;
    public Vector2 resDownsampledDepth;
  }

  private void setShaderVariables(BuiltinSkyParameters builtinParams) {
    CommandBuffer cmd = builtinParams.commandBuffer;
    cmd.SetGlobalMatrix(kPixelCoordToViewDirWSID, builtinParams.pixelCoordToViewDirMatrix);
    m_CS.SetMatrix(kPixelCoordToViewDirWS_ManualSetID, builtinParams.pixelCoordToViewDirMatrix);
    m_CS.SetVector(kWorldSpaceCameraPos1ID, builtinParams.worldSpaceCameraPos);
  }

  private void setGlobalBuffers(BuiltinSkyParameters builtinParams, int numCloudLayers) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Update the compute buffer. */
    setAtmosphereSettings(builtinParams);

    /* Bind it to the global structured buffer in the shaders. */
    cmd.SetGlobalBuffer(kSettingsBufferID, m_settingsBuffer);
  }

  private void setAtmosphereSettings(BuiltinSkyParameters builtinParams) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    m_atmosphereSettings[0].frameCount = Time.frameCount;
    /* Framebuffer size. */
    m_atmosphereSettings[0].screenSize = m_screenSize;
    m_atmosphereSettings[0].farClip = builtinParams.hdCamera.frustum.planes[5].distance;
    /* Texture resolutions. */
    m_atmosphereSettings[0].resT = m_renderTextures.res.T;
    m_atmosphereSettings[0].resMS = m_renderTextures.res.MS;
    m_atmosphereSettings[0].resSkyView = m_renderTextures.res.skyView;
    m_atmosphereSettings[0].resAP = m_renderTextures.res.AP;
    m_atmosphereSettings[0].resScreenspace = m_renderTextures.res.screenspace;
    m_atmosphereSettings[0].resDownsampledDepth = m_depthDownsampleFactor * m_screenSize;
    m_settingsBuffer.SetData(m_atmosphereSettings);
  }

/******************************************************************************/
/************************ END SHADER VARIABLE HANDLERS ************************/
/******************************************************************************/

};

} // namespace Expanse
