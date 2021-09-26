using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeCloudVolume : MonoBehaviour
{

    public ProceduralCloudVolumeBlock m_cloudVolume;
    [Range(0, 1)]
    public float m_coverage = 0.8f;
    public float m_height = 2000;
    [Min(0)]
    public float m_thickness = 1800;
    [Min(0)]
    public float m_density = 3000;
    [Range(0, 1)]
    public float m_shadowing = 0.5f;
    [Range(0, 1)]
    public float m_ambient = 0.5f;
    [Range(0, 1)]
    public float m_silverLining = 0.5f;
    [Range(0, 1)]
    public float m_silverLiningSpread = 0.25f;
    [Range(0, 1)]
    public float m_raininess = 0.0f;
    [Range(0, 1)]
    public float m_swirl = 0.0f;
    public Vector2 m_wind = new Vector2(1, 1);
    public Datatypes.Quality m_quality = Datatypes.Quality.Medium;
    private const float kCoverageCutoff = 0.7f;

    // Update is called once per frame
    void Update()
    {
        // Remap coverage to a better distribution, and pull down density according
        // to coverage so we get a smooth fadeout.
        float coverageSqrt = Mathf.Pow(m_coverage, 0.33f);
        m_cloudVolume.m_coverageIntensity = Mathf.Max(coverageSqrt, kCoverageCutoff);        
        float densityMultiplier = 1;
        if (coverageSqrt < kCoverageCutoff) {
            densityMultiplier = 1 - (kCoverageCutoff - coverageSqrt) / (kCoverageCutoff);
        }
        m_cloudVolume.m_density = m_density * densityMultiplier;

        m_cloudVolume.m_origin = new Vector3(m_cloudVolume.m_origin.x, m_height, m_cloudVolume.m_origin.z);
        m_cloudVolume.m_YExtent = m_thickness;

        // Estimate MS parameters via density.
        m_cloudVolume.m_multipleScatteringAmount = Mathf.Pow(Mathf.Clamp(m_density / 40000.0f, 0, 1), 0.25f);
        m_cloudVolume.m_multipleScatteringBias = 0.4f - 0.3f * Mathf.Pow(Mathf.Clamp(m_density / 40000.0f, 0, 1), 0.25f);

        // Apply manual adjustments.
        m_cloudVolume.m_multipleScatteringAmount = Mathf.Clamp(m_cloudVolume.m_multipleScatteringAmount + 0.4f - 0.8f * m_shadowing, 0, 1);
        m_cloudVolume.m_multipleScatteringBias = Mathf.Clamp(m_cloudVolume.m_multipleScatteringBias - 0.2f + 0.4f * m_shadowing, 0, 1);

        m_cloudVolume.m_scatteringCoefficients = m_cloudVolume.m_extinctionCoefficients * (1 - 0.9f * m_raininess);

        m_cloudVolume.m_ambientStrengthRange = new Vector2(0.5f + m_ambient, 1 + 2 * m_ambient);

        m_cloudVolume.m_silverIntensity = m_silverLining;
        m_cloudVolume.m_silverSpread = m_silverLiningSpread;

        m_cloudVolume.m_baseWarpIntensity = m_swirl;

        m_cloudVolume.m_baseVelocity = m_wind * new Vector2(0.001f, 0.001f);
        m_cloudVolume.m_structureVelocity = m_wind * new Vector2(0.01f, 0.01f);
        m_cloudVolume.m_detailVelocity = m_wind * new Vector2(0.01f, 0.01f);

        m_cloudVolume.m_noiseTextureQuality = m_quality;
    }
}

}
