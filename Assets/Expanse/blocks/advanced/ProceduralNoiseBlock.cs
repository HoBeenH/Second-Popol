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
public class ProceduralNoiseBlock : BaseTextureGeneratorBlock
{
    /* User-exposed controls. */
    // Texture params
    [Tooltip("Name for this texture.")]
    public string m_name = "Expanse Generated Texture";
    [Tooltip("Desired texture dimension.")]
    public Datatypes.NoiseDimension m_dimension = Datatypes.NoiseDimension.TwoDimensional;
    [Tooltip("Desired texture resolution.")]
    public Vector2Int m_res2D;
    [Tooltip("Desired texture resolution.")]
    public Vector3Int m_res3D;

    // Noise params
    [Tooltip("Type of noise.")]
    public Datatypes.NoiseType m_noiseType = Datatypes.NoiseType.Perlin;
    [Tooltip("Scale of noise.")]
    public Vector2Int m_scale = new Vector2Int(16, 16);
    [Range(1, 8), Tooltip("How many octaves of noise to compute.")]
    public int m_octaves = 3;
    [Min(1), Tooltip("How much to scale each octave by.")]
    public float m_octaveScale = 2;
    [Min(0), Tooltip("How much influence each successive octave has.")]
    public float m_octaveMultiplier = 0.5f;

    // Internal render texture that is accessible via BaseTextureGeneratorBlock's
    // GetTexture() function.
    private RTHandle m_target = null;

    /* Compute shader to invoke. */
    private ComputeShader m_CS = null;

    /* Maximum allowed resolution. */
    private static Vector2Int kMaxResolution2D = new Vector2Int(8192, 8192);
    private static Vector3Int kMaxResolution3D = new Vector3Int(1024, 1024, 1024);

    /* Hash code for change tracking. */
    private int m_hashCode = 0;
    private bool m_forceUpdate = false;

    // Start is called before the first frame update
    void Start()
    {
        m_CS = Resources.Load<ComputeShader>("CloudGenerator");
        m_hashCode = 0;
        m_forceUpdate = false;
    }

    void OnEnable() 
    {
        m_hashCode = 0;
    }

    void OnDisable() 
    {
    }

    void OnDestroy() 
    {
        if (m_target != null) {
            RTHandles.Release(m_target);
            m_target = null;
        }
    }

    void Update()
    {
        // Make sure compute shader is allocated.
        if (m_CS == null) {
            m_CS = Resources.Load<ComputeShader>("CloudGenerator");
        }

        // Clamp parameters.
        m_res2D.Clamp(Vector2Int.one, kMaxResolution2D);
        m_res3D.Clamp(Vector3Int.one, kMaxResolution3D);
        m_scale = Vector2Int.Max(m_scale, Vector2Int.zero);

        // Reallocate the target texture if anything's changed.
        reallocateTargetIfNecessary();

        // Early out if hash code is unchanged.
        int newHashCode = GetHashCode();
        if (m_hashCode != newHashCode || m_forceUpdate) {
            regenerate();
        }

        // Update hash code.
        m_hashCode = newHashCode;
        m_forceUpdate = false;
    }

    private void regenerate() {
        // Look up the right kernel handle.
        string dimensionString = (m_target.rt.dimension == UnityEngine.Rendering.TextureDimension.Tex2D) ? "2D" : "3D";
        int handle = m_CS.FindKernel(Datatypes.noiseTypeToKernelName(m_noiseType) + dimensionString);
        
        // Set the output texture.
        m_CS.SetTexture(handle, "_Noise" + dimensionString, m_target);

        // Set the noise parameters.
        m_CS.SetVector("_res", new Vector4(m_target.rt.width, m_target.rt.height, m_target.rt.volumeDepth, 1));
        m_CS.SetVector("_scale", new Vector4(m_scale.x, m_scale.y, m_scale.y, 1));
        m_CS.SetInt("_octaves", m_octaves);
        m_CS.SetFloat("_octaveScale", m_octaveScale);
        m_CS.SetFloat("_octaveMultiplier", m_octaveMultiplier);

        m_CS.Dispatch(handle, 
            IRenderer.computeGroups(m_target.rt.width, 4), 
            IRenderer.computeGroups(m_target.rt.height, 4), 
            IRenderer.computeGroups(m_target.rt.volumeDepth, 4));
        
        m_target.rt.GenerateMips();
    }

    private void reallocateTargetIfNecessary() {
        if (m_dimension == Datatypes.NoiseDimension.TwoDimensional) {
            if (m_target == null
                || m_target.rt.dimension != UnityEngine.Rendering.TextureDimension.Tex2D
                || m_target.rt.width != m_res2D.x || m_target.rt.height != m_res2D.y
                || (m_noiseType == Datatypes.NoiseType.Curl 
                        ? m_target.rt.graphicsFormat == GraphicsFormat.R16G16B16A16_SNorm
                        : m_target.rt.graphicsFormat == GraphicsFormat.R16_SNorm)) {
                if (m_target != null) {
                    RTHandles.Release(m_target);
                    m_target = null;
                }
                m_target = (m_noiseType == Datatypes.NoiseType.Curl) 
                    ? IRenderer.allocateMonochromeTexture2D(m_name, m_res2D, useMipMap: true, format: GraphicsFormat.R16_SNorm)
                    : IRenderer.allocateRGBATexture2D(m_name, m_res2D, useMipMap: true, format: GraphicsFormat.R16G16B16A16_SNorm);
            }
        } else {
            if (m_target == null
                || m_target.rt.dimension != UnityEngine.Rendering.TextureDimension.Tex3D
                || m_target.rt.width != m_res3D.x || m_target.rt.height != m_res3D.y || m_target.rt.volumeDepth != m_res3D.z
                || (m_noiseType == Datatypes.NoiseType.Curl 
                        ? m_target.rt.graphicsFormat == GraphicsFormat.R8G8B8A8_SNorm
                        : m_target.rt.graphicsFormat == GraphicsFormat.R8_SNorm)) {
                if (m_target != null) {
                    RTHandles.Release(m_target);
                    m_target = null;
                }
                m_target = (m_noiseType == Datatypes.NoiseType.Curl) 
                    ? IRenderer.allocateMonochromeTexture3D(m_name, m_res3D, useMipMap: true, format: GraphicsFormat.R8_SNorm)
                    : IRenderer.allocateRGBATexture3D(m_name, m_res3D, useMipMap: true, format: GraphicsFormat.R8G8B8A8_SNorm);
            }
        }
    }

    public override RTHandle GetTexture() {
        return m_target;
    }

    public override void ForceUpdate() {
        m_forceUpdate = true;
    }

    public override int GetHashCode() {
        int hash = 1;
        unchecked {
            hash = (m_target == null) ? hash : hash * 23 + m_target.GetHashCode();
            hash = hash * 23 + m_dimension.GetHashCode();
            hash = hash * 23 + m_res2D.GetHashCode();
            hash = hash * 23 + m_res3D.GetHashCode();
            hash = hash * 23 + m_noiseType.GetHashCode();
            hash = hash * 23 + m_scale.GetHashCode();
            hash = hash * 23 + m_octaves.GetHashCode();
            hash = hash * 23 + m_octaveScale.GetHashCode();
            hash = hash * 23 + m_octaveMultiplier.GetHashCode();
        }
        return hash;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralNoiseBlock))]
public class ProceduralNoiseBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_name"));
    SerializedProperty dim = serializedObject.FindProperty("m_dimension");
    EditorGUILayout.PropertyField(dim);
    if (dim.enumValueIndex == (int) Datatypes.NoiseDimension.TwoDimensional) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_res2D"));
    } else {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_res3D"));
    }
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_noiseType"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scale"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_octaves"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_octaveScale"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_octaveMultiplier"));
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse