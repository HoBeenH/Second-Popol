using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct CelestialBodyRenderSettings {
    public Vector3 direction;
    public float cosAngularRadius;
    public float distance;
    public int receivesLight;
    public int hasAlbedoTexture;
    public Vector2 albedoTextureResolution;
    public Matrix4x4 albedoTextureRotation;
    public Vector4 albedoTint;
    public int moonMode;
    public float retrodirection;
    public float anisotropy;
    public int emissive;
    public int hasEmissionTexture;
    public Vector2 emissionTextureResolution;
    /* Color and intensity. */
    public Vector4 lightColor;
    public float limbDarkening;
    public Matrix4x4 emissionTextureRotation;
    /* Color and intensity. */
    public Vector4 emissionTint;

    /* For globally tracking bodies. */
    private static List<CelestialBodyBlock> kBodies = new List<CelestialBodyBlock>();
    public static void register(CelestialBodyBlock b) {
        if (!kBodies.Contains(b)) {
            kBodies.Add(b);
        }
    }
    public static void deregister(CelestialBodyBlock b) {
        kBodies.Remove(b);
    }

    /* For setting global buffers. */
    private static ComputeBuffer kComputeBuffer;
    private static CelestialBodyRenderSettings[] kArray = new CelestialBodyRenderSettings[1];
    /* Precomputed to avoid expensive string ops in the update loop. */
    private static int[] kAlbedoTextureNames = new int[CelestialBodyDatatypes.kMaxCelestialBodies];
    private static int[] kEmissionTextureNames = new int[CelestialBodyDatatypes.kMaxCelestialBodies];

    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Set number of bodies we have.
        cmd.SetGlobalInt("_ExpanseNumCelestialBodies", kBodies.Count);

        // If we have no bodies, deallocate compute buffer and return.
        if (kBodies.Count == 0) {
            cleanup();
            return;
        }

        // Reallocate compute buffer and array if necessary.
        if (kComputeBuffer == null || kComputeBuffer.count != kBodies.Count || kArray.Length != kBodies.Count) {
            cleanup();
            kArray = new CelestialBodyRenderSettings[kBodies.Count];
            kComputeBuffer = new ComputeBuffer(kBodies.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(CelestialBodyRenderSettings)));
        }

        // Fill array.
        for (int i = 0; i < kBodies.Count; i++) {
            kArray[i].direction = CelestialBodyUtils.rotationVectorToDirection(kBodies[i].m_direction);
            kArray[i].cosAngularRadius = Mathf.Cos(kBodies[i].m_angularRadius * Mathf.Deg2Rad);
            kArray[i].distance = kBodies[i].m_distance;
            kArray[i].receivesLight = kBodies[i].m_receivesLight ? 1 : 0;
            kArray[i].albedoTextureRotation = Utilities.quaternionVectorToRotationMatrix(kBodies[i].m_albedoTextureRotation);
            kArray[i].albedoTint = kBodies[i].m_albedoTint;
            kArray[i].emissive = kBodies[i].m_emissive ? 1 : 0;

            if (kBodies[i].m_useTemperature) {
                kArray[i].lightColor = Vector3.Scale(((Vector4) kBodies[i].m_lightColor).xyz(), CelestialBodyUtils.blackbodyTempToColor(kBodies[i].m_lightTemperature) * kBodies[i].m_lightIntensity);
            } else {
                kArray[i].lightColor = kBodies[i].m_lightColor * kBodies[i].m_lightIntensity;
            }

            kArray[i].limbDarkening = kBodies[i].m_limbDarkening;
            kArray[i].emissionTextureRotation = Utilities.quaternionVectorToRotationMatrix(kBodies[i].m_emissionTextureRotation);
            kArray[i].emissionTint = kBodies[i].m_emissionTint * kBodies[i].m_emissionMultiplier;

            if (kBodies[i].m_albedoTexture != null) {
                kArray[i].hasAlbedoTexture = 1;
                kArray[i].albedoTextureResolution = new Vector2(kBodies[i].m_albedoTexture.width, kBodies[i].m_albedoTexture.height);
                cmd.SetGlobalTexture(kAlbedoTextureNames[i], kBodies[i].m_albedoTexture);
            } else {
                kArray[i].hasAlbedoTexture = 0;
                cmd.SetGlobalTexture(kAlbedoTextureNames[i], IRenderer.kDefaultTextureCube);
            }

            kArray[i].moonMode = kBodies[i].m_moonMode ? 1 : 0;
            kArray[i].retrodirection = kBodies[i].m_retrodirection;
            kArray[i].anisotropy = kBodies[i].m_anisotropy;

            if (kBodies[i].m_emissionTexture != null) {
                kArray[i].hasEmissionTexture = 1;
                kArray[i].emissionTextureResolution = new Vector2(kBodies[i].m_emissionTexture.width, kBodies[i].m_emissionTexture.height);
                cmd.SetGlobalTexture(kEmissionTextureNames[i], kBodies[i].m_emissionTexture);
            } else {
                kArray[i].hasEmissionTexture = 0;
                cmd.SetGlobalTexture(kEmissionTextureNames[i], IRenderer.kDefaultTextureCube);
            }

            if (kBodies[i].m_tidallyLocked) {
                /* Alter rotation matrices based on direction. */
                Vector3 L = kBodies[i].m_direction;
                Matrix4x4 bodyLightRotation = Matrix4x4.Rotate(Quaternion.Euler(L.x, L.y, L.z));
                kArray[i].albedoTextureRotation = kArray[i].albedoTextureRotation * bodyLightRotation.inverse;
                kArray[i].emissionTextureRotation = kArray[i].emissionTextureRotation * bodyLightRotation.inverse;
            }
        }

        /* Bind default textures for the remaining celestial bodies. */
        for (int i = kBodies.Count; i < CelestialBodyDatatypes.kMaxCelestialBodies; i++) {
            cmd.SetGlobalTexture(kAlbedoTextureNames[i], IRenderer.kDefaultTextureCube);
            cmd.SetGlobalTexture(kEmissionTextureNames[i], IRenderer.kDefaultTextureCube);
        }

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpanseCelestialBodies", kComputeBuffer);
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(CelestialBodyRenderSettings)));

        for (int i = 0; i < kAlbedoTextureNames.Length; i++) {
            kAlbedoTextureNames[i] = Shader.PropertyToID("_ExpanseBodyAlbedoTex" + i);
            kEmissionTextureNames[i] = Shader.PropertyToID("_ExpanseBodyEmissionTex" + i);
        }
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
