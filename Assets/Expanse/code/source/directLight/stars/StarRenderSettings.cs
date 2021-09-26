using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct StarRenderSettings {
    public Matrix4x4 rotation;
    public Vector3 tint;
    public int highDensityMode;
    public float density;
    public Vector3 densitySeed;
    public Vector2 sizeRange;
    public float sizeBias;
    public Vector3 sizeSeed;
    public Vector2 intensityRange;
    public float intensityBias;
    public Vector3 intensitySeed;
    public Vector2 temperatureRange;
    public float temperatureBias;
    public Vector3 temperatureSeed;
    public float nebulaFollowAmount;
    public int twinkle;
    public float twinkleThreshold;
    public Vector2 twinkleFrequencyRange;
    public float twinkleBias;
    public float twinkleSmoothAmplitude;
    public float twinkleChaoticAmplitude;

    /* Cache of global state. */
    public static void register(ProceduralStarsBlock b) {
        m_proceduralStars = b;
    }
    public static void deregister(ProceduralStarsBlock b) {
        if (m_proceduralStars == b) {
            m_proceduralStars = null;
        }
    }
    public static void register(TextureStarsBlock b) {
        m_textureStars = b;
    }
    public static void deregister(TextureStarsBlock b) {
        if (m_textureStars == b) {
            m_textureStars = null;
        }
    }

    /* We'll pick one or the other if both of these are registered. */
    private static TextureStarsBlock m_textureStars;
    private static ProceduralStarsBlock m_proceduralStars;
    private static Datatypes.Quality m_quality;

    public static int GetStarHashCode() {
        int hash = 1;
        hash = hash * 23 + (m_proceduralStars == null).GetHashCode();
        if (m_proceduralStars != null) {
            hash = hash * 23 + m_proceduralStars.m_quality.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_highDensityMode.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_density.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_densitySeed.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_sizeRange.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_sizeBias.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_sizeSeed.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_intensityRange.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_intensityBias.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_intensitySeed.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_temperatureRange.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_temperatureBias.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_temperatureSeed.GetHashCode();
            hash = hash * 23 + m_proceduralStars.m_tint.GetHashCode();
        } else if (m_textureStars != null) {
            hash = (m_textureStars.m_starTexture == null) ? hash : hash * 23 + m_textureStars.m_starTexture.GetHashCode();
        }
        return hash;
    }

    public static bool Procedural() {
        return (m_proceduralStars != null);
    }

    public static Datatypes.Quality GetQuality() {
        return m_quality;
    }

    /* For setting global buffer. */
    private static ComputeBuffer kComputeBuffer;
    private static StarRenderSettings[] kArray = new StarRenderSettings[1];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have a compute buffer.
        if (kComputeBuffer == null) {
            build();
        }

        if (m_proceduralStars != null) {
            setShaderGlobalsProcedural(settings, cmd);
        } else if (m_textureStars != null) {
            setShaderGlobalsTexture(settings, cmd);
        } else {
            cmd.SetGlobalBuffer("_ExpanseStars", kComputeBuffer);
            cmd.SetGlobalTexture("_ExpanseTextureStarTexture", IRenderer.kDefaultTextureCube);
            cmd.SetGlobalInt("_ExpanseStarsProcedural", 0);
            cmd.SetGlobalInt("_ExpanseHasStarTexture", 0);
            return;
        }

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpanseStars", kComputeBuffer);
    }

    private static void setShaderGlobalsTexture(ExpanseSettings settings, CommandBuffer cmd) {
        cmd.SetGlobalInt("_ExpanseStarsProcedural", 0);
        kArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_textureStars.m_rotation);
        kArray[0].tint = ((Vector4) m_textureStars.m_tint * m_textureStars.m_intensity).xyz();
        if (m_textureStars.m_starTexture == null) {
            cmd.SetGlobalTexture("_ExpanseTextureStarTexture", IRenderer.kDefaultTextureCube);
            cmd.SetGlobalInt("_ExpanseHasStarTexture", 0);
        } else {
            cmd.SetGlobalTexture("_ExpanseTextureStarTexture", m_textureStars.m_starTexture);
            cmd.SetGlobalInt("_ExpanseHasStarTexture", 1);
        }
    }

    private static void setShaderGlobalsProcedural(ExpanseSettings settings, CommandBuffer cmd) {
        cmd.SetGlobalInt("_ExpanseStarsProcedural", 1);
        cmd.SetGlobalInt("_ExpanseHasStarTexture", 0);
        cmd.SetGlobalTexture("_ExpanseTextureStarTexture", IRenderer.kDefaultTextureCube);
        
        m_quality = m_proceduralStars.m_quality;

        kArray[0].rotation = Utilities.quaternionVectorToRotationMatrix(m_proceduralStars.m_rotation);
        kArray[0].tint = ((Vector4) m_proceduralStars.m_tint * m_proceduralStars.m_intensity).xyz();

        // Procedural generation params.
        kArray[0].highDensityMode = m_proceduralStars.m_highDensityMode ? 1 : 0;
        kArray[0].density = m_proceduralStars.m_density;
        kArray[0].densitySeed = m_proceduralStars.m_densitySeed;
        kArray[0].sizeRange = m_proceduralStars.m_sizeRange;
        kArray[0].sizeBias = m_proceduralStars.m_sizeBias;
        kArray[0].sizeSeed = m_proceduralStars.m_sizeSeed;
        kArray[0].intensityRange = m_proceduralStars.m_intensityRange;
        kArray[0].intensityBias = m_proceduralStars.m_intensityBias;
        kArray[0].intensitySeed = m_proceduralStars.m_intensitySeed;
        kArray[0].temperatureRange = m_proceduralStars.m_temperatureRange;
        kArray[0].temperatureBias = m_proceduralStars.m_temperatureBias;
        kArray[0].temperatureSeed = m_proceduralStars.m_temperatureSeed;
        kArray[0].nebulaFollowAmount = 0;

        // Twinkle params.
        kArray[0].twinkle = m_proceduralStars.m_twinkle ? 1 : 0;
        kArray[0].twinkleThreshold = m_proceduralStars.m_twinkleThreshold;
        kArray[0].twinkleFrequencyRange = m_proceduralStars.m_twinkleFrequencyRange;
        kArray[0].twinkleBias = m_proceduralStars.m_twinkleBias;
        kArray[0].twinkleSmoothAmplitude = m_proceduralStars.m_twinkleSmoothAmplitude;
        kArray[0].twinkleChaoticAmplitude = m_proceduralStars.m_twinkleChaoticAmplitude; //
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(StarRenderSettings)));
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
