using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CreativeStars : MonoBehaviour
{

    public ProceduralStarsBlock m_starsBlock;

    [Min(0)]
    public float m_brightness = 50;
    [Range(0, 1)]
    public float m_density = 0.25f;
    [Range(0, 1)]
    public float m_size = 0;

    void Update()
    {
        m_starsBlock.m_intensity = m_brightness;
        m_starsBlock.m_density = m_density;
        m_starsBlock.m_sizeBias = 0.35f + m_size * 0.35f;
    }
}

}
