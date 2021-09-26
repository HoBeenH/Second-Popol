using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse { //

[GenerateHLSL(needAccessors=false)]
public struct CloudLayerRenderSettings {

    /* For globally tracking layers. */
    private static List<BaseCloudLayerBlock> kLayers = new List<BaseCloudLayerBlock>();
    /* Indices of shadow layers in regular layers list. */
    private static List<int> kShadowLayers = new List<int>();
    
    public static void register(BaseCloudLayerBlock b) {
        if (!kLayers.Contains(b)) {
            kLayers.Add(b);
        }
    }
    public static void deregister(BaseCloudLayerBlock b) {
        kLayers.Remove(b);
    }

    public static int GetLayerCount() {
        return kLayers.Count;
    }
    public static int GetShadowLayerCount() {
        return kShadowLayers.Count;
    }
    /* Maps shadow layer index => absolute index in cloud array (of shadow-casters
     * and non-shadow-casters). */
    public static int GetShadowLayerIndex(int i) {
        return kShadowLayers[i];
    }
    public static UniversalCloudLayer GetLayer(int i) {
        return kLayers[i].ToUniversal();
    }

    /* For setting global buffers. */
    private static ComputeBuffer kLayerComputeBuffer;
    private static ComputeBuffer kNoiseComputeBuffer;
    private static UniversalCloudLayer.UniversalCloudLayerRenderSettings[] kLayerArray = new UniversalCloudLayer.UniversalCloudLayerRenderSettings[1];
    private static UniversalCloudLayer.UniversalCloudNoiseLayer.UniversalCloudNoiseLayerRenderSettings[] kNoiseArray = new UniversalCloudLayer.UniversalCloudNoiseLayer.UniversalCloudNoiseLayerRenderSettings[CloudDatatypes.kNumCloudNoiseLayers];

    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        // If we have no layers, deallocate compute buffer and return.
        if (kLayers.Count == 0) {
            cleanup();
            cmd.SetGlobalInt("_ExpanseNumCloudLayers", 0);
            cmd.SetGlobalBuffer("_ExpanseCloudLayers", IRenderer.kDefaultComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseCloudNoises", IRenderer.kDefaultComputeBuffer);
            return;
        }

        if (kLayerComputeBuffer == null || kLayerComputeBuffer.count != kLayers.Count 
            || kLayerArray.Length != kLayers.Count) {
            cleanup();
            kLayerArray = new UniversalCloudLayer.UniversalCloudLayerRenderSettings[kLayers.Count];
            kNoiseArray = new UniversalCloudLayer.UniversalCloudNoiseLayer.UniversalCloudNoiseLayerRenderSettings[kLayers.Count * CloudDatatypes.kNumCloudNoiseLayers];
            kLayerComputeBuffer = new ComputeBuffer(kLayers.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(UniversalCloudLayer.UniversalCloudLayerRenderSettings)));
            kNoiseComputeBuffer = new ComputeBuffer(kLayers.Count * (int) CloudDatatypes.kNumCloudNoiseLayers, System.Runtime.InteropServices.Marshal.SizeOf(typeof(UniversalCloudLayer.UniversalCloudNoiseLayer.UniversalCloudNoiseLayerRenderSettings)));
        }

        kShadowLayers.Clear();
        for (int i = 0; i < kLayers.Count; i++) {
            UniversalCloudLayer universal = kLayers[i].ToUniversal();
            kLayerArray[i] = universal.renderSettings;
            for (int j = 0; j < CloudDatatypes.kNumCloudNoiseLayers; j++) {
                kNoiseArray[i * CloudDatatypes.kNumCloudNoiseLayers + j] = universal.noiseLayers[j].renderSettings;
            }
            if (kLayerArray[i].castShadows > 0) {
                kShadowLayers.Add(i);
            }
        }

        cmd.SetGlobalInt("_ExpanseNumCloudLayers", kLayerArray.Length);
        kLayerComputeBuffer.SetData(kLayerArray);
        cmd.SetGlobalBuffer("_ExpanseCloudLayers", kLayerComputeBuffer);
        kNoiseComputeBuffer.SetData(kNoiseArray);
        cmd.SetGlobalBuffer("_ExpanseCloudNoises", kNoiseComputeBuffer);
    }

    public static void build() {
        if (kLayerComputeBuffer != null) {
            kLayerComputeBuffer.Release();
        }
        kLayerComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(UniversalCloudLayer.UniversalCloudLayerRenderSettings)));

        if (kNoiseComputeBuffer != null) {
            kNoiseComputeBuffer.Release();
        }
        kNoiseComputeBuffer = new ComputeBuffer(1 * (int) CloudDatatypes.kNumCloudNoiseLayers, System.Runtime.InteropServices.Marshal.SizeOf(typeof(UniversalCloudLayer.UniversalCloudNoiseLayer.UniversalCloudNoiseLayerRenderSettings)));
    }

    public static void cleanup() {
        if (kLayerComputeBuffer != null) {
            kLayerComputeBuffer.Release();
        }
        kLayerComputeBuffer = null;

        if (kNoiseComputeBuffer != null) {
            kNoiseComputeBuffer.Release();
        }
        kNoiseComputeBuffer = null;
    }
}

} // namespace Expanse
