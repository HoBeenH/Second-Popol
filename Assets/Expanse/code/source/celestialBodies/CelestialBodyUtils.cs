using System;
using UnityEngine;

namespace Expanse {

/**
 * @brief: static class containing common functions used when dealing with
 * celestial bodies.
 * */
public class CelestialBodyUtils {

  public static Vector3 rotationVectorToDirection(Vector3 v) {
    Quaternion bodyLightRotation = Quaternion.Euler(v.x, v.y, v.z);
    return bodyLightRotation * (new Vector3(0, 0, -1));
  }

  public static Vector4 blackbodyTempToColor(float t) {
  t = t / 100;
  float r = 0;
  float g = 0;
  float b = 0;

  /* Red. */
  if (t <= 66) {
    r = 255;
  } else {
    r = t - 60;
    r = 329.698727446f * (Mathf.Pow(r, -0.1332047592f));
  }

  /* Green. */
  if (t <= 66) {
    g = t;
    g = 99.4708025861f * Mathf.Log(t) - 161.1195681661f;
  } else {
    g = 288.1221695283f * (Mathf.Pow((t-60), -0.0755148492f));
  }

  /* Blue. */
  if (t >= 66) {
    b = 255;
  } else {
    if (t <= 19) {
      b = 0;
    } else {
      b = 138.5177312231f * Mathf.Log(t-b) - 305.0447927307f;
    }
  }

  r = Mathf.Clamp(r, 0, 255) / 255;
  g = Mathf.Clamp(g, 0, 255) / 255;
  b = Mathf.Clamp(b, 0, 255) / 255;
  return new Vector4(r, g, b, 0);
}

}

} // namespace Expanse
