using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace Expanse {

/**
 * @brief: composites together atmosphere, direct light, and clouds.
 */
public class SkyCompositor : IRenderer {

  ProfilingSampler m_profilingSampler = new ProfilingSampler("Expanse: Composite Sky");
  Material m_material;
  MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();
  private RTHandle m_fullscreenNoGeometryBuffer;
  const string kShaderPath = "Hidden/HDRP/Sky/Composite Sky";
  const string kBlueNoisePath = "blue-noise";
  /* Shader passes. */
  const int kNoGeometryPass = 0;
  const int kFullScreenPass = 1;
  const int kCubemapPass = 2;

  public override void build() {
    m_material = CoreUtils.CreateEngineMaterial(Shader.Find(kShaderPath));
  }

  /* Expects dependencies to be {AtmosphereRenderer, CloudRenderer,
   * CloudCompositor, DirectLightRenderer}. */
  public override void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Resize framebuffers if necessary. */
    checkAndResizeFramebuffers(builtinParams.screenSize.xy());

    /* Set the relevant properties. */
    m_PropertyBlock.SetMatrix("_PixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix);
    m_PropertyBlock.SetMatrix("_InversePixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix.inverse);
    m_PropertyBlock.SetVector("_WorldSpaceCameraPos1", builtinParams.worldSpaceCameraPos);

    /* Set properties/textures from the other system components. */
    AtmosphereRenderer atmosphereRenderer = (AtmosphereRenderer) dependencies[0];
    CloudRenderer cloudRenderer = (CloudRenderer) dependencies[1];
    CloudCompositor cloudCompositor = (CloudCompositor) dependencies[2];
    DirectLightRenderer directLightRenderer = (DirectLightRenderer) dependencies[3];
    atmosphereRenderer.setTextureResolution("T", "_resT", m_PropertyBlock);
    atmosphereRenderer.setTextureResolution("skyView", "_resSkyView", m_PropertyBlock);
    atmosphereRenderer.setTexture("skyView", "_atmosphereSkyView", m_PropertyBlock);
    atmosphereRenderer.setTexture("AP", "_atmosphereAerialPerspective", m_PropertyBlock);
    atmosphereRenderer.setTexture("screenspace", "_screenspaceVolumetrics", m_PropertyBlock);

    /* Set both the renderer's textures and the compositor's textures, but
     * indicate which ones to use with _useCloudTextureArray. */
    cloudRenderer.setTexture("cloudLighting", "_cloudLightingArray", m_PropertyBlock);
    cloudRenderer.setTexture("cloudTransmittanceAndHit", "_cloudTransmittanceAndHitArray", m_PropertyBlock);
    cloudRenderer.setTexture("cloudGeometry", "_cloudGBufferArray", m_PropertyBlock);
    cloudCompositor.setTexture("fullscreenLighting", "_cloudLighting", m_PropertyBlock);
    cloudCompositor.setTexture("fullscreenTransmittanceAndHit", "_cloudTransmittanceAndHit", m_PropertyBlock);
    m_PropertyBlock.SetInt("_useCloudTextureArray", (CloudLayerRenderSettings.GetLayerCount() == 1) ? 1 : 0);

    directLightRenderer.setTexture("fullscreen", "_directLight", m_PropertyBlock);

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      /* Draw the no-geometry result into its own buffer, so we can use it in the transparent 
       * pass for clip blending. */
      CoreUtils.SetRenderTarget(cmd, m_fullscreenNoGeometryBuffer);
      CoreUtils.DrawFullScreen(cmd, m_material, m_PropertyBlock, kNoGeometryPass);
      CoreUtils.SetRenderTarget(cmd, builtinParams.colorBuffer, builtinParams.depthBuffer);
      
      /* Make it available to transparents, with the clip fade value. */
      cmd.SetGlobalTexture("_EXPANSE_NO_GEOMETRY_FRAMEBUFFER", m_fullscreenNoGeometryBuffer);
      
      /* Then composite it together with the geometry result according to the clip blend. */
      m_PropertyBlock.SetTexture("_fullscreenNoGeometry", m_fullscreenNoGeometryBuffer);
      CoreUtils.DrawFullScreen(cmd, m_material, m_PropertyBlock, kFullScreenPass);
    }
  }

  /* Expects dependencies to be {AtmosphereRenderer, CloudCompositor}. */
  public void renderForCubemap(BuiltinSkyParameters builtinParams, IRenderer[] dependencies) {
    ExpanseSettings settings = builtinParams.skySettings as ExpanseSettings;
    CommandBuffer cmd = builtinParams.commandBuffer;

    /* Set the relevant properties. */
    m_PropertyBlock.SetMatrix("_PixelCoordToViewDirWS", builtinParams.pixelCoordToViewDirMatrix);
    m_PropertyBlock.SetVector("_WorldSpaceCameraPos1", builtinParams.worldSpaceCameraPos);

    /* Set properties/textures from the other system components. */
    AtmosphereRenderer atmosphereRenderer = (AtmosphereRenderer) dependencies[0];
    CloudCompositor cloudCompositor = (CloudCompositor) dependencies[1];
    atmosphereRenderer.setTextureResolution("skyView", "_resSkyView", m_PropertyBlock);
    atmosphereRenderer.setTexture("skyView", "_atmosphereSkyView", m_PropertyBlock);
    cloudCompositor.setTexture("lightAttenuation", "_cloudLightAttenuation", m_PropertyBlock);
    cloudCompositor.setTexture("reflection", "_cloudReflection", m_PropertyBlock);
    cloudCompositor.setTexture("reflectionT", "_cloudReflectionT", m_PropertyBlock);

    using (new ProfilingScope(cmd, m_profilingSampler)) {
      CoreUtils.DrawFullScreen(cmd, m_material, m_PropertyBlock, kCubemapPass);
    }
  }

  private void checkAndResizeFramebuffers(Vector2 newResolution) {
    /* Allocate if we haven't already. */
    if (m_fullscreenNoGeometryBuffer == null) {
      m_fullscreenNoGeometryBuffer = allocateRGBATexture2D("Fullscreen No Geometry Buffer", 
        new Vector2Int((int) newResolution.x, (int) newResolution.y));
    }
    /* Otherwise check and reallocate if necessary. */
    if (m_fullscreenNoGeometryBuffer.rt.width != newResolution.x 
      || m_fullscreenNoGeometryBuffer.rt.height !=  newResolution.y) {
      cleanupFramebuffers();
      m_fullscreenNoGeometryBuffer = allocateRGBATexture2D("Fullscreen No Geometry Buffer", 
        new Vector2Int((int) newResolution.x, (int) newResolution.y));
    }
  }

  private void cleanupFramebuffers() {
    if (m_fullscreenNoGeometryBuffer != null) {
      RTHandles.Release(m_fullscreenNoGeometryBuffer);
    }
    m_fullscreenNoGeometryBuffer = null;
  }

  public override void cleanup() {
    cleanupFramebuffers();
  }
  
  /* We have no textures we want to be able to expose externally. */
  public override IReadOnlyCollection<string> getTextureNames() { return new List<string>(); }
  public override void setTexture(string texture, string shaderVariable,
    MaterialPropertyBlock propertyBlock) {}
  public override void setTexture(string texture, string shaderVariable,
    ComputeShader computeShader, int kernelHandle) {}
  public override void setTexture(string texture, string shaderVariable, CommandBuffer cmd) {}
  public override void setTexture(string texture, int shaderVariable, CommandBuffer cmd) {}
  public override void setTextureResolution(string texture,
    string shaderVariable, MaterialPropertyBlock propertyBlock) {}
  public override void setTextureResolution(string texture, string shaderVariable,
    ComputeShader computeShader) {}
  public override void setTextureResolution(string texture, string shaderVariable, CommandBuffer cmd) {}
  public override Vector3 getTextureResolution(string texture) { return new Vector3(-1, -1, -1); }

};

} // namespace Expanse
