using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

[VolumeComponentMenu("Sky/Expanse")]
[SkyUniqueID(EXPANSE_UNIQUE_ID)]
public class ExpanseSettings : SkySettings {

  const int EXPANSE_UNIQUE_ID = 20382532;
  /* We need to use a fake hash code that changes every time GetHashCode()
   * is called to ensure that the sky reflection cubemap is re-rendered
   * every frame. */
  private static int m_fakeHashCode = 0;

  /**
   * @brief: returns the associated renderer for this sky type.
   * */
  public override Type GetSkyRendererType() {
    return typeof(ExpanseRenderer);
  }

  /**
   * @brief: used for determining when to re-render the sky cubemap.
   * We return a different value frame-to-frame, so as to make sure
   * the sky cubemap is rendered every frame.
   * */
  public override int GetHashCode() {
    m_fakeHashCode = (m_fakeHashCode + 1) % 2;
    return m_fakeHashCode;
  }

  /**
   * Helper for getting a member via reflection.
   * @throws: NullReferenceException if field does not exist.
   */
   public T getMemberVariable<T>(string name) {
     return (T) this.GetType().GetField(name).GetValue(this);
   }

  /**
   * Helper for setting a member via reflection.
   */
   public void setMemberVariable<T>(string name, T value) {
     this.GetType().GetField(name).SetValue(this, value);
   }

}

} // namespace Expanse
