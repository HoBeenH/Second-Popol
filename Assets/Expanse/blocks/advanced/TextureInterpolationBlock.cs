using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using Expanse;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class TextureInterpolationBlock : BaseTextureGeneratorBlock
{
    [Tooltip("First texture to lerp.")]
    public BaseTextureGeneratorBlock m_textureA = null;
    [Tooltip("Second texture to lerp.")]
    public BaseTextureGeneratorBlock m_textureB = null;
    [Range(0, 1), Tooltip("Amount to lerp textures.")]
    public float m_blend = 0.5f;
    [Min(0), Tooltip("Amount to tile texture A.")]
    public float m_tileA = 1;
    [Min(0), Tooltip("Amount to tile texture B.")]
    public float m_tileB = 1;
    [Tooltip("If enabled, amortizes interpolation across multiple frames (16, to be specific). This can drastically improve performance.")]
    public bool m_amortize = true;


    // Internal render texture that is accessible via BaseTextureGeneratorBlock's
    // GetTexture() function.
    private RTHandle m_target = null;
    private bool m_render = true;

    /* Compute shader to invoke. */
    private ComputeShader m_CS = null;

    // Start is called before the first frame update
    void Start()
    {
        m_CS = Resources.Load<ComputeShader>("ExpanseCommon");
    }

    void Update()
    {
        // Make sure compute shader is allocated.
        if (m_CS == null) {
            m_CS = Resources.Load<ComputeShader>("ExpanseCommon");
        }

        // Early out if either of the two source textures is unspecified.
        if (m_textureA == null || m_textureB == null) {
            return;
        }

        // Make sure texture parameters are compatible.
        if (!validateParameters()) {
            return;
        }

        reallocateTargetIfNecessary();

        // Execute the lerp!
        if (m_render) {
            render();
        }
    }

    private void reallocateTargetIfNecessary() {
        // Gather desired texture params.
        UnityEngine.Rendering.TextureDimension dim = m_textureA.GetTexture().rt.dimension;
        int width = m_textureA.GetTexture().rt.width;
        int height = m_textureA.GetTexture().rt.height;
        int depth = m_textureA.GetTexture().rt.volumeDepth;
        // TODO: maybe pick this based off which one has more channels?
        GraphicsFormat format = m_textureA.GetTexture().rt.graphicsFormat;

        bool needsReallocation = false;
        if (m_target == null) {
            needsReallocation = true;
        } else {
            needsReallocation = needsReallocation || m_target.rt.dimension != dim;
            needsReallocation = needsReallocation || m_target.rt.width != width || m_target.rt.height != height || m_target.rt.volumeDepth != depth;
            needsReallocation = needsReallocation || m_target.rt.graphicsFormat != format;
        }

        if (needsReallocation) {
            if (m_target != null) {
                RTHandles.Release(m_target);
                m_target = null;
            }
            m_target = (dim == UnityEngine.Rendering.TextureDimension.Tex2D)
                ? IRenderer.allocateRGBATexture2D("interpolated result", new Vector2Int(width, height), true, format)
                : IRenderer.allocateRGBATexture3D("interpolated result", new Vector3Int(width, height, depth), true, format);
        }
    }

    private bool validateParameters() {
        if (m_textureA.GetTexture() == null || m_textureB.GetTexture() == null) {
            return false;
        }
        if (m_textureA.GetTexture().rt.dimension != m_textureB.GetTexture().rt.dimension) {
            Debug.LogError("TextureInterpolationBlock: mismatching texture dimensions. Texture A: " + m_textureA.GetTexture().rt.dimension + " Texture B: " + m_textureB.GetTexture().rt.dimension);
            return false;
        }
        return true;
    }

    private void render() 
    {
        // Look up the right kernel handle.
        string dimensionString = (m_target.rt.dimension == UnityEngine.Rendering.TextureDimension.Tex2D) ? "2D" : "3D";
        int handle = m_CS.FindKernel("LERP" + dimensionString + (m_amortize ? "_AMORTIZED" : ""));

        // Set the output + input textures.
        m_CS.SetTexture(handle, "_Target" + dimensionString, m_target);
        m_CS.SetTexture(handle, "_inA" + dimensionString, m_textureA.GetTexture());
        m_CS.SetTexture(handle, "_inB" + dimensionString, m_textureB.GetTexture());

        // Set the parameters
        Vector4 res = new Vector4(m_target.rt.width, m_target.rt.height, m_target.rt.volumeDepth, 1);
        m_CS.SetVector("_targetRes", res);
        m_CS.SetFloat("_lerpAmount", m_blend);
        m_CS.SetFloat("_tileA", m_tileA);
        m_CS.SetFloat("_tileB", m_tileB);
        m_CS.SetInt("_frameCount", Time.frameCount);

        // Do it!
        if (m_amortize) {
            m_CS.Dispatch(handle, 
                IRenderer.computeGroups(m_target.rt.width / 2, 4), 
                IRenderer.computeGroups(m_target.rt.height / 2, 4), 
                IRenderer.computeGroups((int) Mathf.Max(1, m_target.rt.volumeDepth), 4));
            if (Time.frameCount % 4 == 0) {
                m_target.rt.GenerateMips();
            }
        } else {
            m_CS.Dispatch(handle, 
                IRenderer.computeGroups(m_target.rt.width, 4), 
                IRenderer.computeGroups(m_target.rt.height, 4), 
                IRenderer.computeGroups(m_target.rt.volumeDepth, 4));
            m_target.rt.GenerateMips();
        }
    }

    public override RTHandle GetTexture() {
        return m_target;
    }

    public override void ForceUpdate() {
        if (m_textureA != null) {
            m_textureA.ForceUpdate();
        }
        if (m_textureB != null) {
            m_textureB.ForceUpdate();
        }
    }

    public void EnableRendering() {
        m_render = true;
    }

    public void DisableRendering() {
        m_render = false;
    }
}

} // namespace Expanse