using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct AerialPerspectiveRenderSettings {
  public float uniformOcclusionSpread;
  public float uniformOcclusionBias;
  public float directionalOcclusionSpread;
  public float directionalOcclusionBias;
  public float nightScatteringMultiplier;

   /* Cache of global state. */
    public static void register(AerialPerspectiveSettingsBlock b) {
        m_settings = b;
    }
    public static void deregister(AerialPerspectiveSettingsBlock b) {
        if (m_settings == b) {
            m_settings = null;
        }
    }
    private static AerialPerspectiveSettingsBlock m_settings;

    /* For setting global buffer. */
    private static ComputeBuffer kComputeBuffer;
    private static AerialPerspectiveRenderSettings[] kArray = new AerialPerspectiveRenderSettings[1];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have a compute buffer.
        if (kComputeBuffer == null) {
            build();
        }

        if (m_settings == null) {
            Debug.LogError("Expanse requires an aerial perspective settings block to function. Please add one.");
            return;
        }

        kArray[0].uniformOcclusionSpread = m_settings.m_uniformOcclusionSpread;
        kArray[0].uniformOcclusionBias = m_settings.m_uniformOcclusionBias;
        kArray[0].directionalOcclusionSpread = m_settings.m_directionalOcclusionSpread;
        kArray[0].directionalOcclusionBias = m_settings.m_directionalOcclusionBias;
        kArray[0].nightScatteringMultiplier = m_settings.m_nightScatteringMultiplier;

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpanseAerialPerspectiveSettings", kComputeBuffer);
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AerialPerspectiveRenderSettings)));
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
