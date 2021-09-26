using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Expanse {

[ExecuteInEditMode, Serializable]
public class CameraSettingsBlock : MonoBehaviour
{
    [Tooltip("Camera that Expanse's ambient probe is rendered for. Defaults to the scene's main camera.")]
    public Camera m_ambientProbeCamera;
    [Tooltip("Prefer to render the ambient probe relative to the editor camera if both the editor and main camera are rendering.")]
    public bool m_preferEditorCamera = true;

    void Update() {
        if (m_ambientProbeCamera == null) {
            m_ambientProbeCamera = Camera.main;
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(CameraSettingsBlock))]
public class CameraSettingsBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ambientProbeCamera"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_preferEditorCamera"));
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse