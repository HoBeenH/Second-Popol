using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class PlanetBlock : MonoBehaviour
{
    /* User-exposed controls. */

    [Min(10), Tooltip("The total thickness of the atmosphere, in world units.")]
    public float m_atmosphereThickness = 40000;
    [Min(10), Tooltip("The radius of the planet, in world units.")]
    public float m_radius = 6360000;
    [Tooltip("The planet origin, in world units, but specified as an offset from the position (0, -radius, 0), since that origin is much more convenient.")]
    public Vector3 m_originOffset = new Vector3(0, 0, 0);
    [Range(0, 1), Tooltip("Distance to fade geometry to sky, as a function of the clip plane distance. Useful for flight sims with flat terrain to smoothly fade out to the curved planet.")]
    public float m_clipFade = 1;
    [Tooltip("The ground albedo as a cubemap texture. The ground is modeled as a Lambertian (completely diffuse) reflector. If no texture is specified, the color of the ground will just be the ground tint.")]
    public Cubemap m_groundAlbedoTexture = null;
    [Tooltip("A color tint to the ground texture. Perfect grey, (128, 128, 128), specifies no tint. If there is no ground texture specified, this is just the color of the ground. If the tint is black, this can give a performance boost, since the ground will not be lit.")]
    public Color m_groundTint = Color.black;
    [Tooltip("The ground emission as a cubemap texture. Useful for modeling things like city lights. Has no effect on the sky. See \"Light Pollution\" for a way of modeling an emissive ground's effect on the atmosphere.")]
    public Cubemap m_groundEmissionTexture = null;
    [Min(0), Tooltip("An intensity multiplier on the ground emission texture.")]
    public float m_groundEmissionMultiplier = 1;
    [Tooltip("The rotation of the planet textures as euler angles. This won't do anything to light directions, star rotations, etc. It is purely for rotating the planet's albedo and emissive textures.")]
    public Vector3 m_rotation = new Vector3(0.0f, 0.0f, 0.0f);
    
    void OnEnable() {
        PlanetRenderSettings.register(this);
    }

    void OnDisable() {
        PlanetRenderSettings.deregister(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(PlanetBlock))]
public class PlanetBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_atmosphereThickness"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_radius"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_originOffset"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_clipFade"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_groundTint"));

    SerializedProperty albTex = serializedObject.FindProperty("m_groundAlbedoTexture");
    SerializedProperty emissiveTex = serializedObject.FindProperty("m_groundEmissionTexture");
    EditorGUILayout.PropertyField(albTex);
    EditorGUILayout.PropertyField(emissiveTex);

    if (emissiveTex.objectReferenceValue != null) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_groundEmissionMultiplier"));
    }

    if (emissiveTex.objectReferenceValue != null || albTex.objectReferenceValue != null) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
    }

    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse