using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct NightSkyRenderSettings {

    public Matrix4x4 rotation;
    public Vector3 tint; // tint and intensity
    public Vector3 scatterTint; // tint and intensity
    public Vector3 lightPollution; // tint and intensity
    public float ambientMultiplier;

    /* Cache of global state. */
    public static void register(NightSkyBlock b) {
        m_nightSky = b;
    }
    public static void deregister(NightSkyBlock b) {
        if (m_nightSky == b) {
            m_nightSky = null;
        }
    }
    private static NightSkyBlock m_nightSky;

    /* For setting global buffer. */
    private static ComputeBuffer kComputeBuffer;
    private static NightSkyRenderSettings[] kArray = new NightSkyRenderSettings[1];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have a compute buffer.
        if (kComputeBuffer == null) {
            build();
        }

        if (m_nightSky == null) {
            Debug.LogError("Expanse requires a night sky block to function. Please add one.");
            return;
        }

        kArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_nightSky.m_rotation);
        kArray[0].scatterTint = ((Vector4) m_nightSky.m_scatterTint).xyz() * m_nightSky.m_scatterIntensity;
        kArray[0].tint = ((Vector4) m_nightSky.m_tint).xyz() * m_nightSky.m_intensity;
        kArray[0].lightPollution = ((Vector4) m_nightSky.m_lightPollutionTint).xyz() * m_nightSky.m_lightPollutionIntensity;
        kArray[0].ambientMultiplier = m_nightSky.m_ambientMultiplier;

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpanseNightSky", kComputeBuffer);
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NightSkyRenderSettings)));
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
