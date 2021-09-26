using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct PlanetRenderSettings {
    public float radius;
    public float atmosphereRadius;
    public Vector3 originOffset;
    public float clipFade;
    public Color groundTint;
    public float groundEmissionMultiplier;
    public Matrix4x4 rotation;
    public int hasAlbedoTexture;
    public int hasEmissionTexture;

   /* Cache of global state. */
    public static void register(PlanetBlock b) {
        m_planet = b;
    }
    public static void deregister(PlanetBlock b) {
        if (m_planet == b) {
            m_planet = null;
        }
    }
    private static PlanetBlock m_planet;

    /* For setting global buffer. */
    private static ComputeBuffer kComputeBuffer;
    private static PlanetRenderSettings[] kArray = new PlanetRenderSettings[1];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have a compute buffer.
        if (kComputeBuffer == null) {
            build();
        }

        if (m_planet == null) {
            Debug.LogError("Expanse requires a planet block to function. Please add one.");
            return;
        }

        kArray[0].radius = m_planet.m_radius;
        kArray[0].atmosphereRadius = m_planet.m_radius + m_planet.m_atmosphereThickness;
        kArray[0].originOffset = m_planet.m_originOffset;
        kArray[0].clipFade = m_planet.m_clipFade;
        kArray[0].groundTint = m_planet.m_groundTint;
        kArray[0].groundEmissionMultiplier = m_planet.m_groundEmissionMultiplier;
        kArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_planet.m_rotation);
        kArray[0].hasAlbedoTexture = m_planet.m_groundAlbedoTexture == null ? 0 : 1;
        kArray[0].hasEmissionTexture = m_planet.m_groundEmissionTexture == null ? 0 : 1;

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpansePlanetRenderSettings", kComputeBuffer);

        if (m_planet.m_groundAlbedoTexture == null) {
            cmd.SetGlobalTexture("_ExpansePlanetAlbedoTexture", IRenderer.kDefaultTextureCube);
        } else {
            cmd.SetGlobalTexture("_ExpansePlanetAlbedoTexture", m_planet.m_groundAlbedoTexture);
        }

        if (m_planet.m_groundEmissionTexture == null) {
            cmd.SetGlobalTexture("_ExpansePlanetEmissionTexture", IRenderer.kDefaultTextureCube);
        } else {
            cmd.SetGlobalTexture("_ExpansePlanetEmissionTexture", m_planet.m_groundEmissionTexture);
        }

        // Make clip fade available to transparents.        
        cmd.SetGlobalFloat("_EXPANSE_CLIP_FADE", m_planet.m_clipFade);
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PlanetRenderSettings)));
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
