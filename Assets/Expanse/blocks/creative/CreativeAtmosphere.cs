using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeAtmosphere : MonoBehaviour
{

    public AtmosphereLayerBlock m_airBlock;
    public AtmosphereLayerBlock m_ozoneBlock;

    [Tooltip("Color of the sky at daytime.")]
    public Color m_daytimeColor = new Color(0.09f, 0.2061667f, 0.5f, 1);
    [Tooltip("Color of the sky at sunset.")]
    public Color m_sunsetColor = new Color(1-0.09f, 1-0.2061667f, 1-0.5f, 1);
    [Range(0, 10), Tooltip("Amount of ozone gas. Increasing will make the sky more purple at sunset.")]
    public float m_ozone = 1;
    [Range(0, 4), Tooltip("How thick and dense the atmosphere is. Good for alien planets.")]
    public float m_thickness = 1;

    private const float kNormalizationConstant = 6.62f * 1e-5f;

    // Update is called once per frame
    void Update()
    {
        m_airBlock.m_scatteringCoefficients = kNormalizationConstant * m_daytimeColor;
        m_airBlock.m_extinctionCoefficients = kNormalizationConstant * ((new Color(1, 1, 1, 0)) - m_sunsetColor);
        m_airBlock.m_density = m_thickness;
        m_airBlock.m_thickness = 8000 * Mathf.Sqrt(m_thickness);
        m_ozoneBlock.m_density = 0.3f * m_ozone;
    }
}

}
