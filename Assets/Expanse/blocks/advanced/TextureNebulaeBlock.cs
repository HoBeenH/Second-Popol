using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class TextureNebulaeBlock : MonoBehaviour
{
    /* User-exposed controls. */
    [Tooltip("The nebulae as a cubemap texture.")]
    public Cubemap m_nebulaeTexture = null;
    [Min(0), Tooltip("The intensity of the nebulae.")]
    public float m_intensity = 50;
    [Tooltip("Tint to the nebulae texture.")]
    public Color m_tint = Color.white;
    [Tooltip("The rotation of the nebulae texture.")]
    public Vector3 m_rotation = new Vector3(0, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        OnEnable();
    }

    void OnEnable() 
    {
        NebulaRenderSettings.register(this);
    }

    void OnDisable() 
    {
        NebulaRenderSettings.deregister(this);
    }

    // Update is called once per frame
    void Update()
    {
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(TextureNebulaeBlock))]
public class TextureNebulaeBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    
    SerializedProperty tex = serializedObject.FindProperty("m_nebulaeTexture");
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