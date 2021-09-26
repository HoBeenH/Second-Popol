using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

class ExpanseRenderer : SkyRenderer {

  /* Hash codes used for judging when to regenerate procedural textures. */
  int m_starHashCode, m_nebulaHashCode;

  /* Procedural generators. */
  StarGenerator m_starGenerator = new StarGenerator();
  NebulaGenerator m_nebulaGenerator = new NebulaGenerator();
  /* Start with 1 cloud generator---this will be reassigned. */
  CloudGenerator[] m_cloudGenerators = new CloudGenerator[1];

  /* Renderers. */
  AtmosphereRenderer m_atmosphereRenderer = new AtmosphereRenderer();
  Expanse.CloudRenderer m_cloudRenderer = new Expanse.CloudRenderer();
  DirectLightRenderer m_directLightRenderer = new DirectLightRenderer();

  /* Compositors. */
  CloudCompositor m_cloudCompositor = new CloudCompositor();
  SkyCompositor m_skyCompositor = new SkyCompositor();

  /* Function arguments---cached to reduce garbage generation. */
  IRenderer[] m_atmosphereArgs;
  IRenderer[] m_cloudCompositorArgs;
  IRenderer[] m_screenspaceArgs;
  IRenderer[] m_directLightArgs;
  IRenderer[] m_skyCompositorArgs;
  IRenderer[] m_skyCompositorCubemapArgs;

  /* Reference to camera settings block. */
  CameraSettingsBlock m_cameraSettings = null;

  public override void Build() {
    m_starGenerator.build();
    m_nebulaGenerator.build();
    for (int i = 0; i < m_cloudGenerators.Length; i++) {
      m_cloudGenerators[i] = new CloudGenerator(i);
      m_cloudGenerators[i].build();
    }
    m_atmosphereRenderer.build();
    m_cloudRenderer.build();
    m_directLightRenderer.build();
    m_cloudCompositor.build();
    m_skyCompositor.build();

    m_atmosphereArgs = new IRenderer[]{m_cloudRenderer, m_cloudCompositor};
    m_cloudCompositorArgs = new IRenderer[]{m_cloudRenderer};
    m_screenspaceArgs = new IRenderer[]{m_cloudRenderer, m_cloudCompositor};
    m_directLightArgs = new IRenderer[]{m_starGenerator, m_nebulaGenerator};
    m_skyCompositorArgs = new IRenderer[]{m_atmosphereRenderer, m_cloudRenderer, m_cloudCompositor, m_directLightRenderer};
    m_skyCompositorCubemapArgs = new IRenderer[]{m_atmosphereRenderer, m_cloudCompositor};

    IRenderer.buildStaticMembers();
    PlanetRenderSettings.build();
    QualityRenderSettings.build();
    AerialPerspectiveRenderSettings.build();
    NightSkyRenderSettings.build();
    StarRenderSettings.build();
    NebulaRenderSettings.build();
    CelestialBodyRenderSettings.build();
    LightingRenderSettings.build();
    CloudLayerRenderSettings.build();
    AtmosphereLayerRenderSettings.build();
  }

  public override void Cleanup() {
    /* Clean up generator resources. */
    m_starGenerator.cleanup();
    m_nebulaGenerator.cleanup();
    m_atmosphereRenderer.cleanup();
    m_cloudRenderer.cleanup();
    m_directLightRenderer.cleanup();
    m_cloudCompositor.cleanup();
    m_skyCompositor.cleanup();
    foreach (CloudGenerator cloudGenerator in m_cloudGenerators) {
      cloudGenerator.cleanup();
    }

    PlanetRenderSettings.cleanup();
    QualityRenderSettings.cleanup();
    AerialPerspectiveRenderSettings.cleanup();
    NightSkyRenderSettings.cleanup();
    StarRenderSettings.cleanup();
    NebulaRenderSettings.cleanup();
    CelestialBodyRenderSettings.cleanup();
    LightingRenderSettings.cleanup();
    CloudLayerRenderSettings.cleanup();
    AtmosphereLayerRenderSettings.cleanup();

    IRenderer.cleanupStaticMembers();

    m_cameraSettings = null;
  }

  protected override bool Update(BuiltinSkyParameters builtinParams) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    PlanetRenderSettings.SetShaderGlobals(settings, cmd);
    QualityRenderSettings.SetShaderGlobals(settings, cmd);
    AerialPerspectiveRenderSettings.SetShaderGlobals(settings, cmd);
    NightSkyRenderSettings.SetShaderGlobals(settings, cmd);
    StarRenderSettings.SetShaderGlobals(settings, cmd);
    NebulaRenderSettings.SetShaderGlobals(settings, cmd);
    CelestialBodyRenderSettings.SetShaderGlobals(settings, cmd);
    LightingRenderSettings.SetShaderGlobals(settings, cmd);
    CloudLayerRenderSettings.SetShaderGlobals(settings, cmd);
    AtmosphereLayerRenderSettings.SetShaderGlobals(settings, cmd);
    
    return false;
  }

  protected bool updateProceduralTextures(BuiltinSkyParameters builtinParams) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;

    /* Update the cloud noises. */
    updateCloudLayerGenerators(settings);
    foreach (CloudGenerator generator in m_cloudGenerators) {
      generator.render(builtinParams);
    }

    /* Update the procedural nebula. */
    if (NebulaRenderSettings.Procedural()) {
      int newNebulaHashCode = NebulaRenderSettings.GetNebulaeHashCode();
      if (newNebulaHashCode != m_nebulaHashCode) {
        m_nebulaGenerator.render(builtinParams);
        m_nebulaHashCode = newNebulaHashCode;
      }
    }

    /* Update the procedural stars. */
    if (StarRenderSettings.Procedural()) {
      int newStarHashCode = StarRenderSettings.GetStarHashCode();
      if (newStarHashCode != m_starHashCode) {
        m_starGenerator.render(builtinParams);
        m_starHashCode = newStarHashCode;
      }
    }

    return false;
  }

  /* Helper for managing cloud layer noise generators. */
  private void updateCloudLayerGenerators(ExpanseSettings settings) {
    if (m_cloudGenerators.Length != CloudLayerRenderSettings.GetLayerCount()) {
      // Cleanup all the old generators.
      for (int i = 0; i < m_cloudGenerators.Length; i++) {
        m_cloudGenerators[i].cleanup();
      }
      m_cloudGenerators = new CloudGenerator[CloudLayerRenderSettings.GetLayerCount()];
    }
    for (int i = 0; i < m_cloudGenerators.Length; i++) {
      if (m_cloudGenerators[i] == null) {
        m_cloudGenerators[i] = new CloudGenerator(i);
        m_cloudGenerators[i].build();
      } else {
        m_cloudGenerators[i].setLayerIndex(i);
      }
    }
  }

  public override void RenderSky(BuiltinSkyParameters builtinParams,
    bool renderForCubemap, bool renderSunDisk) {
    if (!renderForCubemap) 
    {
      updateProceduralTextures(builtinParams);

      /* Render the atmosphere. */
      m_atmosphereRenderer.setScreenSize(builtinParams.screenSize.xy());
      m_atmosphereRenderer.render(builtinParams, m_atmosphereArgs);

      /* Render the clouds. */
      m_cloudRenderer.render(builtinParams, m_cloudGenerators);

      /* Composite the cloud framebuffers. If no layers are enabled,
       * this will still work. */
      m_cloudCompositor.render(builtinParams, m_cloudCompositorArgs);

      /* We have to render screenspace volumetrics in here, since they
       * require the camera's depth buffer, and the cloud compositor's
       * transmittance buffer. */
      m_atmosphereRenderer.renderScreenspaceVolumetrics(builtinParams, m_screenspaceArgs);

      /* Render the direct light (celestial bodies, the ground, the stars
       * and nebulae). */
      m_directLightRenderer.render(builtinParams, m_directLightArgs);

      /* Finally, composite everything together. */
      m_skyCompositor.render(builtinParams, m_skyCompositorArgs);
    } 
    else 
    {
      /* Decide if we should actually render the cubemap; we should only render it once
       * per frame. */
      if (m_cameraSettings == null) {
        m_cameraSettings = (CameraSettingsBlock) UnityEngine.Object.FindObjectOfType<CameraSettingsBlock>();
      }
      if (m_cameraSettings == null) {
        Debug.LogError("Expanse requires a camera settings block to function. Please add one.");
        return;
      }
      bool renderCubemap = builtinParams.hdCamera.camera == m_cameraSettings.m_ambientProbeCamera;
#if UNITY_EDITOR
      // Check if we prefer to use the editor camera and, if the currently rendering
      // camera is the editor camera.
      if (m_cameraSettings.m_preferEditorCamera && UnityEditor.SceneView.currentDrawingSceneView != null) 
      {
        renderCubemap = builtinParams.hdCamera.camera == UnityEditor.SceneView.currentDrawingSceneView.camera;
      }
#endif
      if (renderCubemap) 
      {
        /* Composite for cubemap, basically just sampling the sky texture. Only do this if this
         * is the main camera though. */
        m_skyCompositor.renderForCubemap(builtinParams, m_skyCompositorCubemapArgs);
      }
    }

  }
}

} // namespace Expanse