using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class TextureStarsBlock : MonoBehaviour
{
    /* User-exposed controls. */
    [Tooltip("The stars as a cubemap texture.")]
    public Cubemap m_starTexture = null;
    [Tooltip("The intensity of the stars.")]
    public float m_intensity = 50;
    [Tooltip("Tint to the stars.")]
    public Color m_tint = Color.white;
    [Tooltip("The rotation of the stars.")]
    public Vector3 m_rotation = new Vector3(0, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        OnEnable();
    }

    void OnEnable() 
    {
        StarRenderSettings.register(this);
    }

    void OnDisable() 
    {
        StarRenderSettings.deregister(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(TextureStarsBlock))]
public class TextureStarsBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    SerializedProperty tex = serializedObject.FindProperty("m_starTexture");
    EditorGUILayout.PropertyField(tex);
    if (tex.objectReferenceValue != null) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_intensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_tint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotation"));
    }
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse