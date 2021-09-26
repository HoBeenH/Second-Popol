using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeFog : MonoBehaviour
{

    public AtmosphereLayerBlock m_fogBlock;
    [Min(0), Tooltip("Color of the fog. Realistic fog is pure grey.")]
    public Color m_color = new Color(0.5f, 0.5f, 0.5f, 1);
    [Min(0), Tooltip("How dense the fog is.")]
    public float m_density = 20;
    [Min(0), Tooltip("How far away from the player the fog extends.")]
    public float m_radius = 5000;
    [Min(0), Tooltip("How high off the ground the fog extends.")]
    public float m_thickness = 200;
    [Range(0, 1), Tooltip("How smoggy the fog looks.")]
    public float m_smog = 0;
    [Range(0, 1), Tooltip("How intense the sun glare from the fog is.")]
    public float m_glare = 0;
    private float kNormalizationConstant = 8e-6f;

    // Update is called once per frame
    void Update()
    {
        m_fogBlock.m_density = m_density;
        m_fogBlock.m_height = m_radius;
        m_fogBlock.m_thickness = m_thickness;
        m_fogBlock.m_extinctionCoefficients = kNormalizationConstant * m_color;
        m_fogBlock.m_scatteringCoefficients = kNormalizationConstant * (1 - m_smog) * m_color;
        m_fogBlock.m_anisotropy = m_glare * 0.95f;
    }
}

}
