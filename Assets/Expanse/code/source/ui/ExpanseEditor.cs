#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.AnimatedValues;

namespace Expanse {

[VolumeComponentEditor(typeof(ExpanseSettings))]
class ExpanseEditor : SkySettingsEditor {

/******************************************************************************/
/******************************** CONSTRUCTOR *********************************/
/******************************************************************************/

public override void OnEnable() {
  /* Boilerplate. */
  base.OnEnable();
  m_CommonUIElementsMask = (uint) SkySettingsUIElement.UpdateMode;

  /* Get the serialized properties from the Expanse class to
   * attach to the editor. */
  var properties = new PropertyFetcher<ExpanseSettings>(serializedObject);

  /* Unpack all the serialized properties into our variables. */
  unpackSerializedProperties(properties);
}

/******************************************************************************/
/****************************** END CONSTRUCTOR *******************************/
/******************************************************************************/




/******************************************************************************/
/************************************ GUI *************************************/
/******************************************************************************/

/**
 * @brief: main draw function.
 * */
public override void OnInspectorGUI() {

}

/******************************************************************************/
/********************************* END GUI ************************************/
/******************************************************************************/




/******************************************************************************/
/*************************** SERIALIZED PROPERTIES ****************************/
/******************************************************************************/

/**
 * @brief: given a property fetcher for an ExpanseSettings object, unpacks
 * all the serialized properties into member variables.
 * */
void unpackSerializedProperties(PropertyFetcher<ExpanseSettings> properties) {

}

/******************************************************************************/
/************************* END SERIALIZED PROPERTIES **************************/
/******************************************************************************/


}

} // namespace Expanse

#endif // #if UNITY_EDITOR
