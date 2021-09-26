using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;

namespace Expanse {

/**
 * @brief: universal, lerp-able, GPU-mirrored representation that all cloud 
 * layers must comply with.
 * */
[Serializable]
public class UniversalCloudLayer {

    public UniversalCloudLayer() {
        noiseLayers = new UniversalCloudNoiseLayer[CloudDatatypes.kNumCloudNoiseLayers];
    }

    /**
     * @brief: universal, lerp-able, GPU-mirrored representation of a cloud
     * noise component.
     * */
    [Serializable]
    public struct UniversalCloudNoiseLayer {
        // GPU-mirrorable data.
        [GenerateHLSL(needAccessors=false), Serializable]
        public struct UniversalCloudNoiseLayerRenderSettings {
            public Datatypes.NoiseType noiseType;
            public Vector2 scale;
            public int octaves;
            public float octaveScale;
            public float octaveMultiplier;
            public int tile;
        }
        public UniversalCloudNoiseLayerRenderSettings renderSettings;
        public bool procedural;
        public Texture noiseTexture;
        public static UniversalCloudNoiseLayer lerp(UniversalCloudNoiseLayer a, UniversalCloudNoiseLayer b, float x) {
            return a;
        }
        public override int GetHashCode() {
            int hash = 1;
            unchecked {
                hash = hash * 23 + procedural.GetHashCode();
                if (procedural) {
                    hash = hash * 23 + renderSettings.noiseType.GetHashCode();
                    hash = hash * 23 + renderSettings.scale.GetHashCode();
                    hash = hash * 23 + renderSettings.octaves.GetHashCode();
                    hash = hash * 23 + renderSettings.octaveScale.GetHashCode();
                    hash = hash * 23 + renderSettings.octaveMultiplier.GetHashCode();
                } else {
                    hash = (noiseTexture == null) ? hash : hash * 23 + noiseTexture.GetHashCode();
                }
            }
            return hash;
        }
    }

    // GPU-mirrorable data.
    [GenerateHLSL(needAccessors=false), Serializable]
    public struct UniversalCloudLayerRenderSettings {
        /* Geometry. */
        public int geometryType;
        public Vector2 geometryXExtent;
        public Vector2 geometryYExtent;
        public Vector2 geometryZExtent;
        public float geometryHeight;

        /* Modeling. */
        public float coverageIntensity;
        public float structureIntensity;
        public float structureMultiply;
        public float detailIntensity;
        public float detailMultiply;
        public float baseWarpIntensity;
        public float detailWarpIntensity;
        public Vector2 heightGradientBottom;
        public Vector2 heightGradientTop;
        public float rounding;
        public float roundingShape;
        public Vector2 windSkew;

        // So we don't have to access individual noise layers when sampling noise textures.
        public int coverageTile;
        public int baseTile;
        public int structureTile;
        public int detailTile;
        public int baseWarpTile;
        public int detailWarpTile;

        /* Movement. */
        public Vector3 coverageOffset;
        public Vector3 baseOffset;
        public Vector3 structureOffset;
        public Vector3 detailOffset;
        public Vector3 baseWarpOffset;
        public Vector3 detailWarpOffset;

        /* Lighting. */
        public float density;
        public float attenuationDistance;
        public float attenuationBias;
        public Vector2 rampUp;
        public Vector3 extinctionCoefficients;
        public Vector3 scatteringCoefficients;
        public float multipleScatteringAmount;
        public float multipleScatteringBias;
        public float multipleScatteringRampDown;
        public float multipleScatteringRampDownShape;
        public float silverSpread;
        public float silverIntensity;
        public float anisotropy;
        public float ambient;
        public Vector2 ambientHeightRange;
        public Vector2 ambientStrengthRange;
        public int selfShadowing;
        public int highQualityShadows;
        public float maxSelfShadowDistance;
        public float lightPollutionDimmer;
        public int celShade;
        public float celShadeLightingBands;
        public float celShadeTransmittanceBands;
        public int castShadows;
        /* 2D. */
        public float apparentThickness;
        /* 3D. */
        public Vector2 verticalProbabilityHeightRange;
        public float verticalProbabilityStrength;
        public Vector2 depthProbabilityHeightRange;
        public Vector2 depthProbabilityStrengthRange;
        public float depthProbabilityDensityMultiplier;
        public float depthProbabilityBias;
        public int depthProbabilityDetailIndex;
        public float maxShadowIntensity;

        /* Sampling And Quality. */
        public int reprojectionFrames;
        public int useTemporalDenoising;
        public float temporalDenoisingRatio;
        public float coarseStepSize;
        public float detailStepSize;
        public Vector2 coarseStepRange;
        public Vector2 detailStepRange;
        public Vector2 stepDistanceRange;
        public Vector2 flythroughStepRange;
        public Vector2 flythroughStepDistanceRange;
        public float mediaZeroThreshold;
        public float transmittanceZeroThreshold;
        public int maxConsecutiveZeroSamples;
    }
    
    /**
     * Order should be:
     *  1) Coverage
     *  2) Base
     *  3) Structure
     *  4) Detail
     *  5) Base Warp
     *  6) Detail Warp
     * */
    public Datatypes.Quality noiseTextureQuality;
    public UniversalCloudNoiseLayer[] noiseLayers;
    public UniversalCloudLayerRenderSettings renderSettings;

    /**
     * @brief: Interpolates between two universal cloud layers. x is in [0, 1].
     * */
    public static UniversalCloudLayer lerp(UniversalCloudLayer a, UniversalCloudLayer b, float x) {
        // Render settings.
        UniversalCloudLayer c = new UniversalCloudLayer();
        /* Geometry. */
        c.renderSettings.geometryType = x < 0.5 ? a.renderSettings.geometryType : b.renderSettings.geometryType;
        c.renderSettings.geometryXExtent = Vector2.Lerp(a.renderSettings.geometryXExtent, b.renderSettings.geometryXExtent, x);
        c.renderSettings.geometryYExtent = Vector2.Lerp(a.renderSettings.geometryYExtent, b.renderSettings.geometryYExtent, x);
        c.renderSettings.geometryZExtent = Vector2.Lerp(a.renderSettings.geometryZExtent, b.renderSettings.geometryZExtent, x);
        c.renderSettings.geometryHeight = Mathf.Lerp(a.renderSettings.geometryHeight, b.renderSettings.geometryHeight, x);

        /* Modeling. */
        c.renderSettings.coverageIntensity = Mathf.Lerp(a.renderSettings.coverageIntensity, b.renderSettings.coverageIntensity, x);
        c.renderSettings.structureIntensity = Mathf.Lerp(a.renderSettings.structureIntensity, b.renderSettings.structureIntensity, x);
        c.renderSettings.structureMultiply = Mathf.Lerp(a.renderSettings.structureMultiply, b.renderSettings.structureMultiply, x);
        c.renderSettings.detailIntensity = Mathf.Lerp(a.renderSettings.detailIntensity, b.renderSettings.detailIntensity, x);
        c.renderSettings.detailMultiply = Mathf.Lerp(a.renderSettings.detailMultiply, b.renderSettings.detailMultiply, x);
        c.renderSettings.baseWarpIntensity = Mathf.Lerp(a.renderSettings.baseWarpIntensity, b.renderSettings.baseWarpIntensity, x);
        c.renderSettings.detailWarpIntensity = Mathf.Lerp(a.renderSettings.detailWarpIntensity, b.renderSettings.detailWarpIntensity, x);
        c.renderSettings.heightGradientBottom = Vector2.Lerp(a.renderSettings.heightGradientBottom, b.renderSettings.heightGradientBottom, x);
        c.renderSettings.heightGradientTop = Vector2.Lerp(a.renderSettings.heightGradientTop, b.renderSettings.heightGradientTop, x);
        c.renderSettings.rounding = Mathf.Lerp(a.renderSettings.rounding, b.renderSettings.rounding, x);
        c.renderSettings.roundingShape = Mathf.Lerp(a.renderSettings.roundingShape, b.renderSettings.roundingShape, x);
        c.renderSettings.windSkew = Vector2.Lerp(a.renderSettings.windSkew, b.renderSettings.windSkew, x);
        
        c.renderSettings.coverageTile = (int) Mathf.Lerp(a.renderSettings.coverageTile, b.renderSettings.coverageTile, x);
        c.renderSettings.baseTile = (int) Mathf.Lerp(a.renderSettings.baseTile, b.renderSettings.baseTile, x);
        c.renderSettings.structureTile = (int) Mathf.Lerp(a.renderSettings.structureTile, b.renderSettings.structureTile, x);
        c.renderSettings.detailTile = (int) Mathf.Lerp(a.renderSettings.detailTile, b.renderSettings.detailTile, x);
        c.renderSettings.baseWarpTile = (int) Mathf.Lerp(a.renderSettings.baseWarpTile, b.renderSettings.baseWarpTile, x);
        c.renderSettings.detailWarpTile = (int) Mathf.Lerp(a.renderSettings.detailWarpTile, b.renderSettings.detailWarpTile, x);

        c.renderSettings.coverageOffset = Vector3.Lerp(a.renderSettings.coverageOffset, b.renderSettings.coverageOffset, x);
        c.renderSettings.baseOffset = Vector3.Lerp(a.renderSettings.baseOffset, b.renderSettings.baseOffset, x);
        c.renderSettings.structureOffset = Vector3.Lerp(a.renderSettings.structureOffset, b.renderSettings.structureOffset, x);
        c.renderSettings.detailOffset = Vector3.Lerp(a.renderSettings.detailOffset, b.renderSettings.detailOffset, x);
        c.renderSettings.baseWarpOffset = Vector3.Lerp(a.renderSettings.baseWarpOffset, b.renderSettings.baseWarpOffset, x);
        c.renderSettings.detailWarpOffset = Vector3.Lerp(a.renderSettings.detailWarpOffset, b.renderSettings.detailWarpOffset, x);

        /* Lighting. */
        c.renderSettings.density = Mathf.Lerp(a.renderSettings.density, b.renderSettings.density, x);
        c.renderSettings.attenuationDistance = Mathf.Lerp(a.renderSettings.attenuationDistance, b.renderSettings.attenuationDistance, x);
        c.renderSettings.attenuationBias = Mathf.Lerp(a.renderSettings.attenuationBias, b.renderSettings.attenuationBias, x);
        c.renderSettings.rampUp = Vector2.Lerp(a.renderSettings.rampUp, b.renderSettings.rampUp, x);
        c.renderSettings.extinctionCoefficients = Vector3.Lerp(a.renderSettings.extinctionCoefficients, b.renderSettings.extinctionCoefficients, x);
        c.renderSettings.scatteringCoefficients = Vector3.Lerp(a.renderSettings.scatteringCoefficients, b.renderSettings.scatteringCoefficients, x);
        c.renderSettings.multipleScatteringAmount = Mathf.Lerp(a.renderSettings.multipleScatteringAmount, b.renderSettings.multipleScatteringAmount, x);
        c.renderSettings.multipleScatteringBias = Mathf.Lerp(a.renderSettings.multipleScatteringBias, b.renderSettings.multipleScatteringBias, x);
        c.renderSettings.multipleScatteringRampDown = Mathf.Lerp(a.renderSettings.multipleScatteringRampDown, b.renderSettings.multipleScatteringRampDown, x);
        c.renderSettings.multipleScatteringRampDownShape = Mathf.Lerp(a.renderSettings.multipleScatteringRampDownShape, b.renderSettings.multipleScatteringRampDownShape, x);
        c.renderSettings.silverSpread = Mathf.Lerp(a.renderSettings.silverSpread, b.renderSettings.silverSpread, x);
        c.renderSettings.silverIntensity = Mathf.Lerp(a.renderSettings.silverIntensity, b.renderSettings.silverIntensity, x);
        c.renderSettings.anisotropy = Mathf.Lerp(a.renderSettings.anisotropy, b.renderSettings.anisotropy, x);
        c.renderSettings.ambient = Mathf.Lerp(a.renderSettings.ambient, b.renderSettings.ambient, x);
        c.renderSettings.ambientHeightRange = Vector2.Lerp(a.renderSettings.ambientHeightRange, b.renderSettings.ambientHeightRange, x);
        c.renderSettings.ambientStrengthRange = Vector2.Lerp(a.renderSettings.ambientStrengthRange, b.renderSettings.ambientStrengthRange, x);
        c.renderSettings.selfShadowing = x < 0.5 ? a.renderSettings.selfShadowing : b.renderSettings.selfShadowing;
        c.renderSettings.highQualityShadows = x < 0.5 ? a.renderSettings.highQualityShadows : b.renderSettings.highQualityShadows;
        c.renderSettings.maxSelfShadowDistance = Mathf.Lerp(a.renderSettings.maxSelfShadowDistance, b.renderSettings.maxSelfShadowDistance, x);
        c.renderSettings.lightPollutionDimmer = Mathf.Lerp(a.renderSettings.lightPollutionDimmer, b.renderSettings.lightPollutionDimmer, x);
        c.renderSettings.celShade = x < 0.5 ? a.renderSettings.celShade : b.renderSettings.celShade;
        c.renderSettings.celShadeLightingBands = Mathf.Lerp(a.renderSettings.celShadeLightingBands, b.renderSettings.celShadeLightingBands, x);
        c.renderSettings.celShadeTransmittanceBands = Mathf.Lerp(a.renderSettings.celShadeTransmittanceBands, b.renderSettings.celShadeTransmittanceBands, x);
        c.renderSettings.castShadows = x < 0.5 ? a.renderSettings.castShadows : b.renderSettings.castShadows;
        /* 2D. */
        c.renderSettings.apparentThickness = Mathf.Lerp(a.renderSettings.apparentThickness, b.renderSettings.apparentThickness, x);
        /* 3D. */
        c.renderSettings.verticalProbabilityHeightRange = Vector2.Lerp(a.renderSettings.verticalProbabilityHeightRange, b.renderSettings.verticalProbabilityHeightRange, x);
        c.renderSettings.verticalProbabilityStrength = Mathf.Lerp(a.renderSettings.verticalProbabilityStrength, b.renderSettings.verticalProbabilityStrength, x);
        c.renderSettings.depthProbabilityHeightRange = Vector2.Lerp(a.renderSettings.depthProbabilityHeightRange, b.renderSettings.depthProbabilityHeightRange, x);
        c.renderSettings.depthProbabilityStrengthRange = Vector2.Lerp(a.renderSettings.depthProbabilityStrengthRange, b.renderSettings.depthProbabilityStrengthRange, x);
        c.renderSettings.depthProbabilityDensityMultiplier = Mathf.Lerp(a.renderSettings.depthProbabilityDensityMultiplier, b.renderSettings.depthProbabilityDensityMultiplier, x);
        c.renderSettings.depthProbabilityBias = Mathf.Lerp(a.renderSettings.depthProbabilityBias, b.renderSettings.depthProbabilityBias, x);
        c.renderSettings.depthProbabilityDetailIndex = x < 0.5 ? a.renderSettings.depthProbabilityDetailIndex : b.renderSettings.depthProbabilityDetailIndex;
        c.renderSettings.maxShadowIntensity = Mathf.Lerp(a.renderSettings.maxShadowIntensity, b.renderSettings.maxShadowIntensity, x);

        /* Sampling And Quality. */
        c.renderSettings.reprojectionFrames = x < 0.5 ? a.renderSettings.reprojectionFrames : b.renderSettings.reprojectionFrames;
        c.renderSettings.useTemporalDenoising = x < 0.5 ? a.renderSettings.useTemporalDenoising : b.renderSettings.useTemporalDenoising;
        c.renderSettings.temporalDenoisingRatio = Mathf.Lerp(a.renderSettings.temporalDenoisingRatio, b.renderSettings.temporalDenoisingRatio, x);
        c.renderSettings.coarseStepSize = Mathf.Lerp(a.renderSettings.coarseStepSize, b.renderSettings.coarseStepSize, x);
        c.renderSettings.detailStepSize = Mathf.Lerp(a.renderSettings.detailStepSize, b.renderSettings.detailStepSize, x);
        c.renderSettings.coarseStepRange = Vector2.Lerp(a.renderSettings.coarseStepRange, b.renderSettings.coarseStepRange, x);
        c.renderSettings.detailStepRange = Vector2.Lerp(a.renderSettings.detailStepRange, b.renderSettings.detailStepRange, x);
        c.renderSettings.stepDistanceRange = Vector2.Lerp(a.renderSettings.stepDistanceRange, b.renderSettings.stepDistanceRange, x);
        c.renderSettings.flythroughStepRange = Vector2.Lerp(a.renderSettings.flythroughStepRange, b.renderSettings.flythroughStepRange, x);
        c.renderSettings.flythroughStepDistanceRange = Vector2.Lerp(a.renderSettings.flythroughStepDistanceRange, b.renderSettings.flythroughStepDistanceRange, x);
        c.renderSettings.mediaZeroThreshold = Mathf.Lerp(a.renderSettings.mediaZeroThreshold, b.renderSettings.mediaZeroThreshold, x);
        c.renderSettings.transmittanceZeroThreshold = Mathf.Lerp(a.renderSettings.transmittanceZeroThreshold, b.renderSettings.transmittanceZeroThreshold, x);
        c.renderSettings.maxConsecutiveZeroSamples = x < 0.5 ? a.renderSettings.maxConsecutiveZeroSamples : b.renderSettings.maxConsecutiveZeroSamples;


        // Noise + texture quality
        c.noiseTextureQuality = x < 0.5 ? a.noiseTextureQuality : b.noiseTextureQuality;
        for (int i = 0; i < c.noiseLayers.Length; i++) {
            c.noiseLayers[i] = x < 0.5 ? a.noiseLayers[i] : b.noiseLayers[i];
            c.noiseLayers[i].renderSettings.tile = (int) Mathf.Lerp(a.noiseLayers[i].renderSettings.tile, b.noiseLayers[i].renderSettings.tile, x);
        }

        return c;
    }

    /**
     * @brief: loads a universal cloud layer from a file.
     * */
    public static UniversalCloudLayer load(string filepath) {
        // Read in the json representation
        StreamReader reader = new StreamReader(filepath);
        string json = reader.ReadToEnd();
        // Convert it to a universal cloud layer
        return JsonUtility.FromJson<UniversalCloudLayer>(json);
    }

    /**
     * @brief: saves a universal cloud layer to a file.
     * */
    public static void save(UniversalCloudLayer universal, string filepath) {
        // Serialize to json with pretty printing
        string json = JsonUtility.ToJson(universal, true);

        // Write to file path
        File.WriteAllText(filepath, json);

        // Reimport to make sure it shows up in the editor---if we are in
        // the editor and not in a build.
#if UNITY_EDITOR
        AssetDatabase.ImportAsset(filepath, ImportAssetOptions.ImportRecursive);
#endif
    }

    public override int GetHashCode() {
        int hash = 1;
        unchecked {
            hash = hash * 23 + noiseTextureQuality.GetHashCode();
        }
        return hash;
    }
}

} // namespace Expanse
