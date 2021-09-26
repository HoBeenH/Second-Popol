using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

/**
 * @brief: common interface for renderer classes that render results
 * to internal frame buffers.
 */
public abstract class IRenderer {



/******************************************************************************/
/********************************** INTERFACE *********************************/
/******************************************************************************/

  /**
   * @brief: main render function for this renderer.
   * */
  public abstract void render(BuiltinSkyParameters builtinParams, IRenderer[] dependencies=null);

  /**
   * @brief: replaces the constructor.
   * */
  public abstract void build();

  /**
   * @brief: destroys any resources in use.
   * */
  public abstract void cleanup();

  /**
   * @brief: returns a read-only collection of the names of all the textures
   * held by this renderer.
   * */
  public abstract IReadOnlyCollection<string> getTextureNames();

  /**
   * @brief: assigns texture this renderer owns to shader variable.
   * */
  public abstract void setTexture(string texture, string shaderVariable, MaterialPropertyBlock propertyBlock);
  public abstract void setTexture(string texture, string shaderVariable, ComputeShader computeShader, int kernelHandle);
  public abstract void setTexture(string texture, string shaderVariable, CommandBuffer cmd);
  public abstract void setTexture(string texture, int shaderVariable, CommandBuffer cmd);

  /**
   * @brief: sets a shader variable to the resolution of a texture this
   * renderer owns.
   * */
  public abstract void setTextureResolution(string texture, string shaderVariable, MaterialPropertyBlock propertyBlock);
  public abstract void setTextureResolution(string texture, string shaderVariable, ComputeShader computeShader);
  public abstract void setTextureResolution(string texture, string shaderVariable, CommandBuffer cmd);

  /**
   * @brief: returns resolution of a texture this rendere owns.
   * */
  public abstract Vector3 getTextureResolution(string texture);

/******************************************************************************/
/******************************** END INTERFACE *******************************/
/******************************************************************************/



/******************************************************************************/
/****************************** STATIC UTILITIES ******************************/
/******************************************************************************/

  /**
   * @return: given resolution and number of threads per group, returns
   * number of thread groups. */
  public static int computeGroups(int res, int threads) {
    return (int) Mathf.Ceil(((float) (res)) / ((float) threads));
  }

  /* Allocates monochrome 2D texture. */
  public static RTHandle allocateMonochromeTexture2D(string name, Vector2Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y,
                                dimension: TextureDimension.Tex2D,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    UnityEngine.Debug.Assert(table != null);
    return table;
  }

  /* Allocates RGB 2D texture. */
  public static RTHandle allocateRGBATexture2D(string name, Vector2Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16G16B16A16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y,
                                dimension: TextureDimension.Tex2D,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    UnityEngine.Debug.Assert(table != null);
    return table;
  }

  /* Allocates monochrome 3D texture. */
  public static RTHandle allocateMonochromeTexture3D(string name, Vector3Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    UnityEngine.Debug.Assert(table != null);
    return table;
  }

  /* Allocates RGB 3D texture. */
  public static RTHandle allocateRGBATexture3D(string name, Vector3Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16G16B16A16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    UnityEngine.Debug.Assert(table != null);
    return table;
  }

  public static RTHandle allocateMonochromeTexture2DArray(string name, Vector2Int resolution, int slices, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, (int) Mathf.Max(1, slices),
                                dimension: TextureDimension.Tex2DArray,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    Debug.Assert(table != null);
    return table;
  }

  public static RTHandle allocateRGBATexture2DArray(string name, Vector2Int resolution, int slices, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16G16B16A16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, (int) Mathf.Max(1, slices),
                                dimension: TextureDimension.Tex2DArray,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: useMipMap,
                                autoGenerateMips: false,
                                name: name);
    Debug.Assert(table != null);
    return table;
  }

  /* Emulates cubemap with texture 2D array of depth 6. */
  public static RTHandle allocateEmulatedMonochromeCubemapTexture(string name, Vector2Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, 6,
                                dimension: TextureDimension.Tex2DArray,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: name);
    Debug.Assert(table != null);
    return table;
  }

  /* Emulates cubemap with texture 2D array of depth 6. */
  public static RTHandle allocateEmulatedRGBACubemapTexture(string name, Vector2Int resolution, bool useMipMap=false, GraphicsFormat format=GraphicsFormat.R16G16B16A16_SFloat) {
    var table = RTHandles.Alloc(resolution.x, resolution.y, 6,
                                dimension: TextureDimension.Tex2DArray,
                                colorFormat: format,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: name);
    Debug.Assert(table != null);
    return table;
  }

  public static RTHandle allocateDefaultTexture2D() {
    var table = RTHandles.Alloc(1, 1,
                                dimension: TextureDimension.Tex2D,
                                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                enableRandomWrite: true,
                                name: "DefaultTexture2D");
    Debug.Assert(table != null);
    return table;
  }

  public static RTHandle allocateDefaultTexture3D() {
    var table = RTHandles.Alloc(1, 1,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                enableRandomWrite: true,
                                name: "DefaultTexture3D");
    Debug.Assert(table != null);
    return table;
  }

  public static RTHandle allocateDefaultTextureCube() {
    var table = RTHandles.Alloc(1, 1,
                                dimension: TextureDimension.Cube,
                                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                enableRandomWrite: true,
                                name: "DefaultTextureCube");
    Debug.Assert(table != null);
    return table;
  }

  public static void clearToWhite(RenderTexture toClear) {
    RenderTexture rt = RenderTexture.active;
    RenderTexture.active = toClear;
    GL.Clear(true, true, Color.white);
    RenderTexture.active = rt;
  }

  public static void clearToBlack(RenderTexture toClear) {
    RenderTexture rt = RenderTexture.active;
    RenderTexture.active = toClear;
    GL.Clear(true, true, Color.black);
    RenderTexture.active = rt;
  }

  // Static default resources.
  public static RTHandle kDefaultTextureCube = null;
  public static RTHandle kDefaultTexture2D = null;
  public static RTHandle kDefaultTexture3D = null;
  public static ComputeBuffer kDefaultComputeBuffer = null;
  public static int kDefaultResourcesRefCount = 0;

  public static void cleanupStaticMembers() {
    kDefaultResourcesRefCount--;
    if (kDefaultResourcesRefCount == 0) {
      // Clean up
      if (kDefaultComputeBuffer != null) {
        kDefaultComputeBuffer.Release();
        kDefaultComputeBuffer = null;
      }
      if (kDefaultTextureCube != null) {
        RTHandles.Release(kDefaultTextureCube);
        kDefaultTextureCube = null;
      }
      if (kDefaultTexture2D != null) {
        RTHandles.Release(kDefaultTexture2D);
        kDefaultTexture2D = null;
      }
      if (kDefaultTexture3D != null) {
        RTHandles.Release(kDefaultTexture3D);
        kDefaultTexture3D = null;
      }
    }
  }

  public static void buildStaticMembers()
  {
    kDefaultResourcesRefCount++;

    // Clean up
    cleanupStaticMembers();

    // Rebuild
    if (kDefaultComputeBuffer == null) {
      kDefaultComputeBuffer = new ComputeBuffer(1, 4);
    }
    if (kDefaultTextureCube == null) {
      kDefaultTextureCube = allocateDefaultTextureCube();
    }
    if (kDefaultTexture2D == null) {
      kDefaultTexture2D = allocateDefaultTexture2D();
    }
    if (kDefaultTexture3D == null) {
      kDefaultTexture3D = allocateDefaultTexture3D();
    }
  }

/******************************************************************************/
/**************************** END STATIC UTILITIES ****************************/
/******************************************************************************/

};

} // namespace Expanse
