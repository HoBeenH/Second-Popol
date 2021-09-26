using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct AtmosphereLayerRenderSettings {
    public Vector3 extinctionCoefficients;
    public Vector3 scatteringCoefficients;
    public int densityDistribution;
    public float height;
    public float thickness;
    public int phaseFunction;
    public float anisotropy;
    public float density;
    public Vector3 tint;
    public float multipleScatteringMultiplier;
    public int screenspaceShadows;
    public float maxGeometryOcclusion;
    public float maxCloudOcclusion;
    public int geometryShadows;
    public int cloudShadows;
    public int useCloudArray;
    public int physicalLighting;

    /* For globally tracking layers. */
    private static List<AtmosphereLayerBlock> kLayers = new List<AtmosphereLayerBlock>();
    public static void register(AtmosphereLayerBlock b) {
        if (!kLayers.Contains(b)) {
            kLayers.Add(b);
        }
    }
    public static void deregister(AtmosphereLayerBlock b) {
        kLayers.Remove(b);
    }
    private static int m_fogLayers = 0;
    private static int m_fogLayersUsingDepth = 0;
    public static int GetFogLayers() {
        return m_fogLayers;
    }
    public static int GetFogLayersUsingDepth() {
        return m_fogLayersUsingDepth;
    }

    /* For setting global buffers. */
    private static ComputeBuffer kAtmosphereComputeBuffer;
    private static ComputeBuffer kFogComputeBuffer;
    private static AtmosphereLayerRenderSettings[] kAtmosphereArray = new AtmosphereLayerRenderSettings[1];
    private static AtmosphereLayerRenderSettings[] kFogArray = new AtmosphereLayerRenderSettings[1];

    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // If we have no bodies, deallocate compute buffer and return.
        if (kLayers.Count == 0) {
            cleanup();
            cmd.SetGlobalInt("_ExpanseNumAtmosphereLayers", 0);
            cmd.SetGlobalInt("_ExpanseNumFogLayers", 0);
            cmd.SetGlobalBuffer("_ExpanseAtmosphereLayers", kAtmosphereComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseFogLayers", kFogComputeBuffer);
            return;
        }

        // Count the number of each type of layer.
        int atmosphereLayers = 0;
        m_fogLayers = 0;
        m_fogLayersUsingDepth = 0;
        for (int i = 0; i < kLayers.Count; i++) {
            if (AtmosphereDatatypes.integrateInScreenspace(kLayers[i].m_densityDistribution)) {
                m_fogLayers++;
                if (kLayers[i].m_geometryShadows) {
                    m_fogLayersUsingDepth++;
                }
            } else {
                atmosphereLayers++;
            }
        }

        cmd.SetGlobalInt("_ExpanseNumAtmosphereLayers", atmosphereLayers);
        cmd.SetGlobalInt("_ExpanseNumFogLayers", m_fogLayers);
        
        // Reallocate compute buffer and array if necessary.
        if (kAtmosphereComputeBuffer == null 
            || kAtmosphereComputeBuffer.count != atmosphereLayers 
            || kAtmosphereArray.Length != atmosphereLayers) {
            cleanupAtmo();
            if (atmosphereLayers > 0) {
                kAtmosphereArray = new AtmosphereLayerRenderSettings[atmosphereLayers];
                kAtmosphereComputeBuffer = new ComputeBuffer(atmosphereLayers, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtmosphereLayerRenderSettings)));
            }
        }

        if (kFogComputeBuffer == null 
            || kFogComputeBuffer.count != m_fogLayers 
            || kFogArray.Length != m_fogLayers) {
            cleanupFog();
            if (m_fogLayers > 0) {
                kFogArray = new AtmosphereLayerRenderSettings[m_fogLayers];
                kFogComputeBuffer = new ComputeBuffer(m_fogLayers, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtmosphereLayerRenderSettings)));
            }
        }

        // Fill arrays.
        int atmosphereCount = 0;
        int fogCount = 0;
        for (int i = 0; i < kLayers.Count; i++) {
            if (AtmosphereDatatypes.integrateInScreenspace(kLayers[i].m_densityDistribution)) {
                kFogArray[fogCount].extinctionCoefficients = ((Vector4) kLayers[i].m_extinctionCoefficients).xyz();
                kFogArray[fogCount].scatteringCoefficients = ((Vector4) kLayers[i].m_scatteringCoefficients).xyz();
                kFogArray[fogCount].densityDistribution = (int) kLayers[i].m_densityDistribution;
                kFogArray[fogCount].height = kLayers[i].m_height;
                kFogArray[fogCount].thickness = kLayers[i].m_thickness;
                kFogArray[fogCount].phaseFunction = (int) kLayers[i].m_phaseFunction;
                kFogArray[fogCount].anisotropy = kLayers[i].m_anisotropy;
                kFogArray[fogCount].density = kLayers[i].m_density;
                kFogArray[fogCount].tint = ((Vector4) kLayers[i].m_tint).xyz();
                kFogArray[fogCount].multipleScatteringMultiplier = kLayers[i].m_multipleScatteringMultiplier;
                kFogArray[fogCount].screenspaceShadows = (kLayers[i].m_geometryShadows || kLayers[i].m_cloudShadows) ? 1 : 0;
                kFogArray[fogCount].maxGeometryOcclusion = kLayers[i].m_maxGeometryOcclusion;
                kFogArray[fogCount].maxCloudOcclusion = kLayers[i].m_maxCloudOcclusion;
                kFogArray[fogCount].geometryShadows = kLayers[i].m_geometryShadows ? 1 : 0;
                kFogArray[fogCount].cloudShadows = kLayers[i].m_cloudShadows ? 1 : 0;
                kFogArray[fogCount].useCloudArray = (CloudLayerRenderSettings.GetLayerCount() == 1) ? 1 : 0;
                kFogArray[fogCount].physicalLighting = kLayers[i].m_physicalLighting ? 1 : 0;
                fogCount++;
            } else {
                kAtmosphereArray[atmosphereCount].extinctionCoefficients = ((Vector4) kLayers[i].m_extinctionCoefficients).xyz();
                kAtmosphereArray[atmosphereCount].scatteringCoefficients = ((Vector4) kLayers[i].m_scatteringCoefficients).xyz();
                kAtmosphereArray[atmosphereCount].densityDistribution = (int) kLayers[i].m_densityDistribution;
                kAtmosphereArray[atmosphereCount].height = kLayers[i].m_height;
                kAtmosphereArray[atmosphereCount].thickness = kLayers[i].m_thickness;
                kAtmosphereArray[atmosphereCount].phaseFunction = (int) kLayers[i].m_phaseFunction;
                kAtmosphereArray[atmosphereCount].anisotropy = kLayers[i].m_anisotropy;
                kAtmosphereArray[atmosphereCount].density = kLayers[i].m_density;
                kAtmosphereArray[atmosphereCount].tint = ((Vector4) kLayers[i].m_tint).xyz();
                kAtmosphereArray[atmosphereCount].multipleScatteringMultiplier = kLayers[i].m_multipleScatteringMultiplier;
                kAtmosphereArray[atmosphereCount].screenspaceShadows = (kLayers[i].m_geometryShadows || kLayers[i].m_cloudShadows) ? 1 : 0;
                kAtmosphereArray[atmosphereCount].maxGeometryOcclusion = kLayers[i].m_maxGeometryOcclusion;
                kAtmosphereArray[atmosphereCount].maxCloudOcclusion = kLayers[i].m_maxCloudOcclusion;
                kAtmosphereArray[atmosphereCount].geometryShadows = kLayers[i].m_geometryShadows ? 1 : 0;
                kAtmosphereArray[atmosphereCount].cloudShadows = kLayers[i].m_cloudShadows ? 1 : 0;
                kAtmosphereArray[atmosphereCount].useCloudArray = (CloudLayerRenderSettings.GetLayerCount() == 1) ? 1 : 0;
                kAtmosphereArray[atmosphereCount].physicalLighting = kLayers[i].m_physicalLighting ? 1 : 0;
                atmosphereCount++;
            }
        }

        if (atmosphereLayers > 0) {
            kAtmosphereComputeBuffer.SetData(kAtmosphereArray);
            cmd.SetGlobalBuffer("_ExpanseAtmosphereLayers", kAtmosphereComputeBuffer);
        } else {
            cmd.SetGlobalBuffer("_ExpanseAtmosphereLayers", IRenderer.kDefaultComputeBuffer);
        }
        if (m_fogLayers > 0) {
            kFogComputeBuffer.SetData(kFogArray);
            cmd.SetGlobalBuffer("_ExpanseFogLayers", kFogComputeBuffer);
        } else {
            cmd.SetGlobalBuffer("_ExpanseFogLayers", IRenderer.kDefaultComputeBuffer);
        }
    }

    public static void build() {
        if (kAtmosphereComputeBuffer != null) {
            kAtmosphereComputeBuffer.Release();
        }
        kAtmosphereComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtmosphereLayerRenderSettings)));
        
        if (kFogComputeBuffer != null) {
            kFogComputeBuffer.Release();
        }
        kFogComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AtmosphereLayerRenderSettings)));
    }

    public static void cleanup() {
        cleanupFog();
        cleanupAtmo();
    }

    private static void cleanupAtmo() {
        if (kAtmosphereComputeBuffer != null) {
            kAtmosphereComputeBuffer.Release();
        }
        kAtmosphereComputeBuffer = null;
    }

    private static void cleanupFog() {
        if (kFogComputeBuffer != null) {
            kFogComputeBuffer.Release();
        }
        kFogComputeBuffer = null;
    }
}

} // namespace Expanse
