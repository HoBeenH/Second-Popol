using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

[GenerateHLSL(needAccessors=false)]
public struct QualityRenderSettings {
    public int samplesT;
    public int samplesAP;
    public int samplesSS;
    public int samplesMS;
    public int samplesMSAcc;
    public int samplesScreenspace;
    public int samplesScreenspaceScattering;
    public int importanceSample;
    public int AP_importanceSample;
    public int screenspaceImportanceSample;
    public float AP_depthSkew;
    public float screenspace_depthSkew;
    public int screenspace_historyFrames;
    public int downsampledDepthMip;
    public int antiAlias;
    public int dither;
    public float cloudShadowMapFilmPlaneScale;

    /* Cache of global state. */
    public static void register(QualitySettingsBlock b) {
        m_quality = b;
    }
    public static void deregister(QualitySettingsBlock b) {
        if (m_quality == b) {
            m_quality = null;
        }
    }
    private static QualitySettingsBlock m_quality;

    /* Get canonical instance of quality settings block. */
    public static QualitySettingsBlock Get() {
        return m_quality;
    }

    /* For setting global buffer. */
    private static ComputeBuffer kComputeBuffer;
    private static QualityRenderSettings[] kArray = new QualityRenderSettings[1];
    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // Make sure we have a compute buffer.
        if (kComputeBuffer == null) {
            build();
        }

        if (m_quality == null) {
            Debug.LogError("Expanse requires a quality settings block to function. Please add one.");
            return;
        }

        kArray[0].samplesT = m_quality.m_transmittanceSamples;
        kArray[0].samplesAP = m_quality.m_aerialPerspectiveSamples;
        kArray[0].samplesSS = m_quality.m_singleScatteringSamples;
        kArray[0].samplesMS = m_quality.m_multipleScatteringSamples;
        kArray[0].samplesMSAcc = m_quality.m_multipleScatteringAccumulationSamples;
        kArray[0].samplesScreenspace = m_quality.m_screenspaceOcclusionSamples;
        kArray[0].samplesScreenspaceScattering = m_quality.m_screenspaceScatteringSamples;
        kArray[0].importanceSample = m_quality.m_importanceSampleAtmosphere ? 1 : 0;
        kArray[0].AP_importanceSample = m_quality.m_importanceSampleAerialPerspective ? 1 : 0;
        kArray[0].screenspaceImportanceSample = m_quality.m_screenspaceImportanceSample ? 1 : 0;
        kArray[0].AP_depthSkew = m_quality.m_aerialPerspectiveDepthSkew;
        kArray[0].screenspace_depthSkew = m_quality.m_screenspaceFogDepthSkew;
        kArray[0].screenspace_historyFrames = m_quality.m_fogDenoisingHistoryFrames;
        kArray[0].downsampledDepthMip = m_quality.m_screenspaceDepthDownscale - 1;
        kArray[0].antiAlias = m_quality.m_antiAlias ? 1 : 0;
        kArray[0].dither = m_quality.m_dither ? 1 : 0;
        kArray[0].cloudShadowMapFilmPlaneScale = m_quality.m_cloudShadowMapFilmPlaneScale / 2.0f;

        /* Bind depth skews globally for fog/AP. */
        cmd.SetGlobalFloat("_Screenspace_depthSkew", m_quality.m_screenspaceFogDepthSkew);
        cmd.SetGlobalFloat("_AP_depthSkew", m_quality.m_aerialPerspectiveDepthSkew);

        kComputeBuffer.SetData(kArray);
        cmd.SetGlobalBuffer("_ExpanseQualitySettings", kComputeBuffer);
    }

    public static void build() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(QualityRenderSettings)));
    }

    public static void cleanup() {
        if (kComputeBuffer != null) {
            kComputeBuffer.Release();
        }
        kComputeBuffer = null;
    }
}

} // namespace Expanse
