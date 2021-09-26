using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct NebulaRenderSettings {

    // Rendering/compositing.
    public Matrix4x4 rotation;
    public float definition;
    public float intensity;
    public Vector4 tint;

    // Generation.
    public float coverageScale;
    public Vector3 coverageSeed;
    public float transmittanceScale;
    public Vector2 transmittanceRange;
    public Vector3 transmittanceSeedX;
    public Vector3 transmittanceSeedY;
    public Vector3 transmittanceSeedZ;

    // Noise layers.
    [GenerateHLSL(needAccessors=false)]
    private struct NebulaGeneratorLayerSettings {
        public float intensity;
        public Vector4 color;
        public int noise;
        public float scale;
        public int octaves;
        public float octaveScale;
        public float octaveMultiplier;
        public float coverage;
        public float spread;
        public float bias;
        public float definition;
        public float strength;
        public float warpScale;
        public float warpIntensity;
        public Vector3 baseSeedX;
        public Vector3 baseSeedY;
        public Vector3 baseSeedZ;
        public Vector3 warpSeedX;
        public Vector3 warpSeedY;
        public Vector3 warpSeedZ;

        public override int GetHashCode() {
            int hash = base.GetHashCode();
            unchecked {
                hash = hash * 23 + intensity.GetHashCode();
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + noise.GetHashCode();
                hash = hash * 23 + scale.GetHashCode();
                hash = hash * 23 + octaves.GetHashCode();
                hash = hash * 23 + octaveScale.GetHashCode();
                hash = hash * 23 + octaveMultiplier.GetHashCode();
                hash = hash * 23 + coverage.GetHashCode();
                hash = hash * 23 + spread.GetHashCode();
                hash = hash * 23 + bias.GetHashCode();
                hash = hash * 23 + definition.GetHashCode();
                hash = hash * 23 + strength.GetHashCode();
                hash = hash * 23 + warpScale.GetHashCode();
                hash = hash * 23 + warpIntensity.GetHashCode();
                hash = hash * 23 + baseSeedX.GetHashCode();
                hash = hash * 23 + baseSeedY.GetHashCode();
                hash = hash * 23 + baseSeedZ.GetHashCode();
                hash = hash * 23 + warpSeedX.GetHashCode();
                hash = hash * 23 + warpSeedX.GetHashCode();
                hash = hash * 23 + warpSeedY.GetHashCode();
                hash = hash * 23 + warpSeedZ.GetHashCode();
            }
            return hash;
        }
    }

    /* Cache of global state. */
    public static void register(ProceduralNebulaeBlock b) {
        m_proceduralNebulae = b;
    }
    public static void deregister(ProceduralNebulaeBlock b) {
        if (m_proceduralNebulae == b) {
            m_proceduralNebulae = null;
        }
    }
    public static void register(TextureNebulaeBlock b) {
        m_textureNebulae = b;
    }
    public static void deregister(TextureNebulaeBlock b) {
        if (m_textureNebulae == b) {
            m_textureNebulae = null;
        }
    }

    /* We'll pick one or the other if both of these are registered. */
    private static TextureNebulaeBlock m_textureNebulae;
    private static ProceduralNebulaeBlock m_proceduralNebulae;
    private static Datatypes.Quality m_quality;

    public static int GetNebulaeHashCode() {
        int hash = 1;
        hash = hash * 23 + (m_proceduralNebulae == null).GetHashCode();
        if (m_proceduralNebulae != null) {
            hash = hash * 23 + m_quality.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].definition.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].intensity.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].coverageScale.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].coverageSeed.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].transmittanceScale.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].transmittanceRange.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].transmittanceSeedX.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].transmittanceSeedY.GetHashCode();
            hash = hash * 23 + kRenderSettingsArray[0].transmittanceSeedZ.GetHashCode();
            for (int i = 0; i < kLayersArray.Length; i++) {
                hash = hash * 23 + kLayersArray[i].GetHashCode();
            }
        } else if (m_textureNebulae != null) {
            hash = (m_textureNebulae.m_nebulaeTexture == null) ? hash : hash * 23 + m_textureNebulae.m_nebulaeTexture.GetHashCode();
        }
        return hash;
    }

    public static bool Procedural() {
        return (m_proceduralNebulae != null);
    }

    public static Datatypes.Quality GetQuality() {
        return m_quality;
    }

    /* For setting global buffer. */
    private static ComputeBuffer kRenderSettingsComputeBuffer;
    private static ComputeBuffer kLayersComputeBuffer;
    private static NebulaRenderSettings[] kRenderSettingsArray = new NebulaRenderSettings[1];
    private static NebulaGeneratorLayerSettings[] kLayersArray = new NebulaGeneratorLayerSettings[NebulaDatatypes.kMaxNebulaLayers];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have valid compute buffers.
        if (kRenderSettingsComputeBuffer == null || kLayersComputeBuffer == null) {
            build();
        }

        if (m_proceduralNebulae != null) {
            setShaderGlobalsProcedural(settings, cmd);
        } else if (m_textureNebulae != null) {
            setShaderGlobalsTexture(settings, cmd);
        } else {
            cmd.SetGlobalBuffer("_ExpanseNebula", kRenderSettingsComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseNebula", kLayersComputeBuffer);
            cmd.SetGlobalTexture("_ExpanseTextureNebulaTexture", IRenderer.kDefaultTextureCube);
            cmd.SetGlobalInt("_ExpanseNebulaProcedural", 0);
            cmd.SetGlobalInt("_ExpanseHasNebulaTexture", 0);
            return;
        }

        kRenderSettingsComputeBuffer.SetData(kRenderSettingsArray);
        cmd.SetGlobalBuffer("_ExpanseNebula", kRenderSettingsComputeBuffer);
        kLayersComputeBuffer.SetData(kLayersArray);
        cmd.SetGlobalBuffer("_ExpanseNebulaLayers", kLayersComputeBuffer);
    }

    private static void setShaderGlobalsTexture(ExpanseSettings settings, CommandBuffer cmd) {
        cmd.SetGlobalInt("_ExpanseNebulaProcedural", 0);
        kRenderSettingsArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_textureNebulae.m_rotation);
        kRenderSettingsArray[0].intensity = m_textureNebulae.m_intensity;
        kRenderSettingsArray[0].tint = ((Vector4) m_textureNebulae.m_tint).xyz();
        if (m_textureNebulae.m_nebulaeTexture == null) {
            cmd.SetGlobalTexture("_ExpanseTextureNebulaTexture", IRenderer.kDefaultTextureCube);
            cmd.SetGlobalInt("_ExpanseHasNebulaTexture", 0);
        } else {
            cmd.SetGlobalTexture("_ExpanseTextureNebulaTexture", m_textureNebulae.m_nebulaeTexture);
            cmd.SetGlobalInt("_ExpanseHasStarTexture", 1);
        }
    }

    private static void setShaderGlobalsProcedural(ExpanseSettings settings, CommandBuffer cmd) {
        cmd.SetGlobalInt("_ExpanseNebulaProcedural", 1);
        cmd.SetGlobalInt("_ExpanseHasNebulaTexture", 0);
        cmd.SetGlobalTexture("_ExpanseTextureNebulaTexture", IRenderer.kDefaultTextureCube);
        
        m_quality = m_proceduralNebulae.m_quality;
        
        kRenderSettingsArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_proceduralNebulae.m_rotation);
        kRenderSettingsArray[0].intensity = m_proceduralNebulae.m_overallIntensity;
        kRenderSettingsArray[0].definition = m_proceduralNebulae.m_overallDefinition;
        kRenderSettingsArray[0].tint = ((Vector4) m_proceduralNebulae.m_overallTint).xyz();

        kRenderSettingsArray[0].coverageScale = m_proceduralNebulae.m_coverageScale;
        kRenderSettingsArray[0].coverageSeed = m_proceduralNebulae.m_coverageSeed;
        kRenderSettingsArray[0].transmittanceScale = m_proceduralNebulae.m_transmittanceScale;
        kRenderSettingsArray[0].transmittanceRange = m_proceduralNebulae.m_transmittanceRange;
        kRenderSettingsArray[0].transmittanceSeedX = m_proceduralNebulae.m_transmittanceSeedX;
        kRenderSettingsArray[0].transmittanceSeedY = m_proceduralNebulae.m_transmittanceSeedY;
        kRenderSettingsArray[0].transmittanceSeedZ = m_proceduralNebulae.m_transmittanceSeedZ;

        for (int i = 0; i < NebulaDatatypes.kMaxNebulaLayers; i++) {
            kLayersArray[i].intensity = m_proceduralNebulae.m_layers[i].intensity;
            kLayersArray[i].color = m_proceduralNebulae.m_layers[i].color;
            kLayersArray[i].noise = (int) m_proceduralNebulae.m_layers[i].noise;
            kLayersArray[i].scale = m_proceduralNebulae.m_layers[i].scale;
            kLayersArray[i].octaves = m_proceduralNebulae.m_layers[i].octaves;
            kLayersArray[i].octaveScale = m_proceduralNebulae.m_layers[i].octaveScale;
            kLayersArray[i].octaveMultiplier = m_proceduralNebulae.m_layers[i].octaveMultiplier;
            kLayersArray[i].coverage = 1 - m_proceduralNebulae.m_layers[i].coverage;
            kLayersArray[i].spread = m_proceduralNebulae.m_layers[i].spread;
            kLayersArray[i].bias = m_proceduralNebulae.m_layers[i].bias;
            kLayersArray[i].definition = m_proceduralNebulae.m_layers[i].definition;
            kLayersArray[i].strength = m_proceduralNebulae.m_layers[i].strength;
            kLayersArray[i].warpScale = m_proceduralNebulae.m_layers[i].warpScale;
            kLayersArray[i].warpIntensity = m_proceduralNebulae.m_layers[i].warpIntensity;
            kLayersArray[i].baseSeedX = m_proceduralNebulae.m_layers[i].baseSeedX;
            kLayersArray[i].baseSeedY = m_proceduralNebulae.m_layers[i].baseSeedY;
            kLayersArray[i].baseSeedZ = m_proceduralNebulae.m_layers[i].baseSeedZ;
            kLayersArray[i].warpSeedX = m_proceduralNebulae.m_layers[i].warpSeedX;
            kLayersArray[i].warpSeedY = m_proceduralNebulae.m_layers[i].warpSeedY;
            kLayersArray[i].warpSeedZ = m_proceduralNebulae.m_layers[i].warpSeedZ;
        }
    }

    public static void build() {
        if (kRenderSettingsComputeBuffer != null) {
            kRenderSettingsComputeBuffer.Release();
        }
        kRenderSettingsComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NebulaRenderSettings)));
        if (kLayersComputeBuffer != null) {
            kLayersComputeBuffer.Release();
        }
        kLayersComputeBuffer = new ComputeBuffer((int) NebulaDatatypes.kMaxNebulaLayers, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NebulaGeneratorLayerSettings)));
    }

    public static void cleanup() {
        if (kRenderSettingsComputeBuffer != null) {
            kRenderSettingsComputeBuffer.Release();
        }
        kRenderSettingsComputeBuffer = null;
        if (kLayersComputeBuffer != null) {
            kLayersComputeBuffer.Release();
        }
        kLayersComputeBuffer = null;
    }
}

} // namespace Expanse
