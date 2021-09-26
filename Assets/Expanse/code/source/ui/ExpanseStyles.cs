#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace Expanse {

/**
 * @brief: class containing ui styles used in Expanse.
 * */
public class ExpanseStyles {

  const int kIndentUnit = 30;

  /**
   * @brief: constructs a foldout header with specified indentation.
   * */
  public static GUIStyle indentedFoldoutStyle(int indentLevel) {
    GUIStyle s = new GUIStyle(EditorStyles.foldoutHeader);
    s.margin = new RectOffset(kIndentUnit * indentLevel, 0, 0, 0);
    return s;
  }

  /**
   * @brief: constructs a dropdown menu with specified indentation.
   * */
  public static GUIStyle indentedDropdownStyle(float indentLevel) {
    GUIStyle s = new GUIStyle(EditorStyles.miniPullDown);
    s.margin = new RectOffset((int) (kIndentUnit * indentLevel), 0, 0, 0);
    return s;
  }
};

} // namespace Expanse

#endif // #if UNITY_EDITOR
