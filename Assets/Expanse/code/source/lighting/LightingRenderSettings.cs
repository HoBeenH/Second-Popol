using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Expanse {

public struct LightingRenderSettings {    
    /* Directional lights. */
    [GenerateHLSL(needAccessors=false)]
    public struct DirectionalLightRenderSettings {
        public Vector3 direction;
        /* Color and intensity. */
        public Vector3 lightColor;
        /* angular radius * penumbra */
        public float penumbraRadius;
        public int useShadowmap;
        public float maxShadowmapDistance;
        public int shadowmapNDCSign;
        public int volumetricGeometryShadows;
        public int volumetricCloudShadows;
    }

    /* Point light types. */
    [GenerateHLSL(needAccessors=false)]
    public enum PointLightGeometryType {
        Point = 0,
        Cone,
        Pyramid,
        Box
    }

    /* Point lights. */
    [GenerateHLSL(needAccessors=false)]
    public struct PointLightRenderSettings {
        public Vector3 position;
        /* Only used for spot lights. NOTE: spot lights have their 
         * axis along positive z. */
        public Matrix4x4 rotation;
        public Matrix4x4 inverseRotation;
        /* Color and intensity. */
        public Vector3 lightColor;
        public float range;
        public int raymarch;
        public float multiplier;
        public int volumetricGeometryShadows;
        public int volumetricCloudShadows;
        public int useShadowmap;
        public float maxShadowmapDistance;
        public int shadowIndex;
        public int geometryType;
        /*
         * Spot lights: inner cos angle
         * Pyramid lights: ...
         * Box lights: size x
         * */
        public float geometryParam1;
        /*
         * Spot lights: outer cos angle
         * Pyramid lights: ...
         * Box lights: size y
         * */
        public float geometryParam2;
    }

    /* Cache of global state. */
    private static List<LightControl> kDirectionalLightControls = new List<LightControl>();
    private static List<LightControl> kPointLightControls = new List<LightControl>();
    public static void register(LightControl b) {
        UnityEngine.Light light = b.GetLight();
        if (light.type == UnityEngine.LightType.Directional) {
            if (!kDirectionalLightControls.Contains(b)) {
                kDirectionalLightControls.Add(b);
            }
        } else if (light.type == UnityEngine.LightType.Point || light.type == UnityEngine.LightType.Spot) {
            if (!kPointLightControls.Contains(b)) {
                kPointLightControls.Add(b);
            }
        }
    }
    public static void deregister(LightControl b) {
        UnityEngine.Light light = b.GetLight();
        if (light.type == UnityEngine.LightType.Directional) {
            kDirectionalLightControls.Remove(b);
        } else if (light.type == UnityEngine.LightType.Point || light.type == UnityEngine.LightType.Spot) {
            kPointLightControls.Remove(b);
        }
    }

    /* For tracking the point light shadow indices that Unity refuses 
     * to give us CPU-side. */
    private static Dictionary<UnityEngine.Light, int> m_lightToShadowIndex = new Dictionary<Light, int>();
    public static void assignShadowIndices() {
        m_lightToShadowIndex.Clear();
        UnityEngine.Light[] lights = (UnityEngine.Light[]) UnityEngine.Object.FindObjectsOfType<UnityEngine.Light>();
        int shadowCount = 0;
        // First do point lights, then spot lights---this seems to be the order that
        // unity uses to assign shadow indices.
        for (int i = 0; i < lights.Length; i++) {
            if ((lights[i].type == UnityEngine.LightType.Point) && lights[i].shadows != LightShadows.None) {
                m_lightToShadowIndex[lights[i]] = shadowCount;
                shadowCount++;
            }
        }
        for (int i = 0; i < lights.Length; i++) {
            if ((lights[i].type == UnityEngine.LightType.Spot) && lights[i].shadows != LightShadows.None) {
                m_lightToShadowIndex[lights[i]] = shadowCount;
                shadowCount++;
            }
        }
    }

    /* For setting global buffers. */
    
    private static PointLightRenderSettings lightSettings = new PointLightRenderSettings();
    private static ComputeBuffer kAtmosphereDirectionalComputeBuffer;
    private static ComputeBuffer kCloudDirectionalComputeBuffer;
    private static ComputeBuffer kCloudShadowDirectionalComputeBuffer;
    private static ComputeBuffer kFogPointComputeBuffer;
    private static ComputeBuffer kCloudPointComputeBuffer;
    private static DirectionalLightRenderSettings[] kAtmosphereDirectionalArray = new DirectionalLightRenderSettings[1];
    private static DirectionalLightRenderSettings[] kCloudDirectionalArray = new DirectionalLightRenderSettings[1];
    private static DirectionalLightRenderSettings[] kCloudShadowDirectionalArray = new DirectionalLightRenderSettings[1];
    private static PointLightRenderSettings[] kFogPointArray = new PointLightRenderSettings[1];
    private static PointLightRenderSettings[] kCloudPointArray = new PointLightRenderSettings[1];
    private static int m_cloudShadowLights = 0;
    private static Dictionary<int, int> m_shadowIndexToAtmosphereIndex = new Dictionary<int, int>();
    public static int GetCloudShadowLights() {
        return m_cloudShadowLights;
    }
    public static int CloudShadowIndexToAtmosphereIndex(int shadowIndex) {
        return m_shadowIndexToAtmosphereIndex[shadowIndex];
    }

    public static void SetShaderGlobals(ExpanseSettings settings, CommandBuffer cmd) {
        SetShaderGlobalsDirectionalLights(settings, cmd);
        SetShaderGlobalsPointLights(settings, cmd);
    }

    public static void SetShaderGlobalsDirectionalLights(ExpanseSettings settings, CommandBuffer cmd) {
        // Clear our frame-local data structures.
        m_shadowIndexToAtmosphereIndex.Clear();

        // Count how many of the directional lights affect the clouds, and register indices for each one.
        int cloudLights = 0;
        int cloudShadowLights = 0;
        for (int i = 0; i < kDirectionalLightControls.Count; i++) {
            kDirectionalLightControls[i].SetAtmosphereIndex(i);
            if (kDirectionalLightControls[i].m_lightClouds) {
                kDirectionalLightControls[i].SetCloudIndex(cloudLights);
                cloudLights++;
                if (kDirectionalLightControls[i].m_castCloudShadows) {
                    kDirectionalLightControls[i].SetCloudShadowIndex(cloudShadowLights);
                    m_shadowIndexToAtmosphereIndex[cloudShadowLights] = i;
                    cloudShadowLights++;
                }
            }
        }

        // Expose a getter for this variable so that the cloud renderer knows how big to make
        // its shadowmap arrays.
        m_cloudShadowLights = cloudShadowLights;

        // Set how many of each we have.
        cmd.SetGlobalInt("_ExpanseNumAtmosphereDirectionalLights", kDirectionalLightControls.Count);
        cmd.SetGlobalInt("_ExpanseNumCloudDirectionalLights", cloudLights);
        cmd.SetGlobalInt("_ExpanseNumCloudShadowDirectionalLights", cloudShadowLights);

        // If we have no kDirectionalLightControls, deallocate compute buffers and return.
        if (kDirectionalLightControls.Count == 0) {
            cleanupAtmoDirectionalLights();
            cleanupCloudDirectionalLights();
            cleanupCloudShadowDirectionalLights();
            cmd.SetGlobalBuffer("_ExpanseAtmosphereDirectionalLights", IRenderer.kDefaultComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseCloudDirectionalLights", IRenderer.kDefaultComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseCloudShadowDirectionalLights", IRenderer.kDefaultComputeBuffer);
            return;
        }

        // Reallocate compute buffers and arrays if necessary.
        if (kCloudDirectionalComputeBuffer == null 
            || kAtmosphereDirectionalComputeBuffer == null
            || kCloudDirectionalComputeBuffer.count != cloudLights 
            || kAtmosphereDirectionalComputeBuffer.count != kDirectionalLightControls.Count 
            || kCloudDirectionalArray.Length != cloudLights
            || kAtmosphereDirectionalArray.Length != kDirectionalLightControls.Count) {
            // TODO: this will reallocate unnecessarily; break these out into their own conditions.
            cleanupAtmoDirectionalLights();
            cleanupCloudDirectionalLights();
            cleanupCloudShadowDirectionalLights();
            kAtmosphereDirectionalArray = new DirectionalLightRenderSettings[kDirectionalLightControls.Count];
            kAtmosphereDirectionalComputeBuffer = new ComputeBuffer(kDirectionalLightControls.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
            if (cloudLights > 0) {
                kCloudDirectionalArray = new DirectionalLightRenderSettings[cloudLights];
                kCloudDirectionalComputeBuffer = new ComputeBuffer(cloudLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
                if (cloudShadowLights > 0) {
                    kCloudShadowDirectionalArray = new DirectionalLightRenderSettings[cloudShadowLights];
                    kCloudShadowDirectionalComputeBuffer = new ComputeBuffer(cloudShadowLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
                }
            }
        }

        // Fill array.
        int cloudLightCount = 0;
        int cloudShadowLightCount = 0;
        for (int i = 0; i < kDirectionalLightControls.Count; i++) {
            kAtmosphereDirectionalArray[i].direction = CelestialBodyUtils.rotationVectorToDirection(kDirectionalLightControls[i].m_direction);
            if (kDirectionalLightControls[i].m_useTemperature) {
                kAtmosphereDirectionalArray[i].lightColor = Vector3.Scale(((Vector4) kDirectionalLightControls[i].m_lightColor).xyz(), CelestialBodyUtils.blackbodyTempToColor(kDirectionalLightControls[i].m_lightTemperature).xyz() * kDirectionalLightControls[i].m_lightIntensity);
            } else {
                kAtmosphereDirectionalArray[i].lightColor = ((Vector4) kDirectionalLightControls[i].m_lightColor).xyz() * kDirectionalLightControls[i].m_lightIntensity;
            }
            kAtmosphereDirectionalArray[i].penumbraRadius = kDirectionalLightControls[i].m_angularRadius * kDirectionalLightControls[i].m_penumbra;
            kAtmosphereDirectionalArray[i].useShadowmap = kDirectionalLightControls[i].m_shadowmapVolumetricShadows ? 1 : 0;
            kAtmosphereDirectionalArray[i].maxShadowmapDistance = kDirectionalLightControls[i].m_maxVolumetricShadowmapDistance;

            bool flipNDC = (kDirectionalLightControls[i].m_direction.x % 360) > 90 && (kDirectionalLightControls[i].m_direction.x % 360) < 270;
            kAtmosphereDirectionalArray[i].shadowmapNDCSign = flipNDC ? -1 : 1;
            
            kAtmosphereDirectionalArray[i].volumetricGeometryShadows = kDirectionalLightControls[i].m_volumetricGeometryShadows ? 1 : 0;
            kAtmosphereDirectionalArray[i].volumetricCloudShadows = kDirectionalLightControls[i].m_volumetricCloudShadows ? 1 : 0;

            if (kDirectionalLightControls[i].m_lightClouds) {
                kCloudDirectionalArray[cloudLightCount] = kAtmosphereDirectionalArray[i];
                cloudLightCount++;
                if (kDirectionalLightControls[i].m_castCloudShadows) {
                    kCloudShadowDirectionalArray[cloudShadowLightCount] = kAtmosphereDirectionalArray[i];
                    cloudShadowLightCount++;
                }
            }

        }

        kAtmosphereDirectionalComputeBuffer.SetData(kAtmosphereDirectionalArray);
        cmd.SetGlobalBuffer("_ExpanseAtmosphereDirectionalLights", kAtmosphereDirectionalComputeBuffer);
        if (cloudLights > 0) {
            kCloudDirectionalComputeBuffer.SetData(kCloudDirectionalArray);
            cmd.SetGlobalBuffer("_ExpanseCloudDirectionalLights", kCloudDirectionalComputeBuffer);
            if (cloudShadowLights > 0) {
                kCloudShadowDirectionalComputeBuffer.SetData(kCloudShadowDirectionalArray);
                cmd.SetGlobalBuffer("_ExpanseCloudShadowDirectionalLights", kCloudShadowDirectionalComputeBuffer);
            } else {
                cmd.SetGlobalBuffer("_ExpanseCloudShadowDirectionalLights", IRenderer.kDefaultComputeBuffer);
            }
        } else {
            cmd.SetGlobalBuffer("_ExpanseCloudDirectionalLights", IRenderer.kDefaultComputeBuffer);
        }
    }

    public static void SetShaderGlobalsPointLights(ExpanseSettings settings, CommandBuffer cmd) {
        // Compute the shadow indices that unity refuses to give us CPU-side.
        assignShadowIndices();

        // Count how many of each type we have.
        int fogLights = 0;
        int cloudLights = 0;


        for (int i = 0; i < kPointLightControls.Count; i++) {
            fogLights += kPointLightControls[i].m_lightFog ? 1 : 0;
            cloudLights += kPointLightControls[i].m_lightClouds ? 1 : 0;
        }
        
        cmd.SetGlobalInt("_ExpanseNumFogPointLights", fogLights);
        cmd.SetGlobalInt("_ExpanseNumCloudPointLights", cloudLights);

        if (fogLights == 0 && cloudLights == 0) {
            // Don't do any setup work, just set the buffers. Can't clean it up because
            // then we'll get a shader error that no buffer has been bound.
            cleanupFogPointLights();
            cleanupCloudPointLights();
            cmd.SetGlobalBuffer("_ExpanseFogPointLights", IRenderer.kDefaultComputeBuffer);
            cmd.SetGlobalBuffer("_ExpanseCloudPointLights", IRenderer.kDefaultComputeBuffer);
            return;
        }
        
        // Resize if necessary.
        if (kFogPointComputeBuffer == null 
            || kFogPointArray.Length != fogLights
            || kFogPointComputeBuffer.count != fogLights) {
            cleanupFogPointLights();
            if (fogLights != 0) {
                kFogPointArray = new PointLightRenderSettings[fogLights];
                kFogPointComputeBuffer = new ComputeBuffer(fogLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightRenderSettings)));
            }
        }

        if (kCloudPointComputeBuffer == null 
            || kCloudPointArray.Length != cloudLights
            || kCloudPointComputeBuffer.count != cloudLights) {
            cleanupCloudPointLights();
            if (cloudLights != 0) {
                kCloudPointArray = new PointLightRenderSettings[cloudLights];
                kCloudPointComputeBuffer = new ComputeBuffer(cloudLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightRenderSettings)));
            }
        }
        
        int fogCount = 0;
        int cloudCount = 0;
        for (int i = 0; i < kPointLightControls.Count; i++) {
            UnityEngine.Light light = kPointLightControls[i].GetLight();
            lightSettings.position = kPointLightControls[i].m_direction; // Direction field is used as position for point lights.
            if (kPointLightControls[i].m_useTemperature) {
                lightSettings.lightColor = Vector3.Scale(((Vector4) kPointLightControls[i].m_lightColor).xyz(), CelestialBodyUtils.blackbodyTempToColor(kPointLightControls[i].m_lightTemperature).xyz() * kPointLightControls[i].m_lightIntensity);
            } else {
                lightSettings.lightColor = ((Vector4) kPointLightControls[i].m_lightColor).xyz() * kPointLightControls[i].m_lightIntensity;
            }
            lightSettings.range = kPointLightControls[i].m_range;
            lightSettings.raymarch = kPointLightControls[i].m_raymarch ? 1 : 0;
            lightSettings.multiplier = kPointLightControls[i].m_fogMultiplier;
            lightSettings.volumetricGeometryShadows = kPointLightControls[i].m_volumetricGeometryShadows ? 1 : 0;
            lightSettings.volumetricCloudShadows = kPointLightControls[i].m_volumetricCloudShadows ? 1 : 0;
            lightSettings.useShadowmap = kPointLightControls[i].m_shadowmapVolumetricShadows ? 1 : 0;
            lightSettings.maxShadowmapDistance = kPointLightControls[i].m_maxVolumetricShadowmapDistance;
            if (light.shadows != LightShadows.None) {
                lightSettings.shadowIndex = m_lightToShadowIndex[light];
            }
            if (light.type == UnityEngine.LightType.Point) {
                HDAdditionalLightData lightData = kPointLightControls[i].GetHDAdditionalLightData();
                lightSettings.geometryType = (int) PointLightGeometryType.Point;
                lightSettings.geometryParam1 = lightData.shapeRadius;
            } else if (light.type == UnityEngine.LightType.Spot) {
                HDAdditionalLightData lightData = kPointLightControls[i].GetHDAdditionalLightData();
                lightSettings.rotation = Matrix4x4.Rotate(light.transform.localRotation);
                lightSettings.inverseRotation = Matrix4x4.Rotate(Quaternion.Inverse(light.transform.localRotation));
                if (lightData.spotLightShape == SpotLightShape.Cone) {
                    lightSettings.geometryType = (int) PointLightGeometryType.Cone;
                    lightSettings.geometryParam1 = Mathf.Deg2Rad * light.innerSpotAngle;
                    lightSettings.geometryParam2 = Mathf.Deg2Rad * light.spotAngle;
                } else if (lightData.spotLightShape == SpotLightShape.Pyramid) {
                    lightSettings.geometryType = (int) PointLightGeometryType.Pyramid;
                    lightSettings.geometryParam1 = Mathf.Deg2Rad * light.spotAngle;
                    lightSettings.geometryParam2 = lightData.aspectRatio;
                    // unhandled
                } else if (lightData.spotLightShape == SpotLightShape.Box) {
                    lightSettings.geometryType = (int) PointLightGeometryType.Box;
                    lightSettings.geometryParam1 = lightData.shapeWidth;
                    lightSettings.geometryParam2 = lightData.shapeHeight;
                }
            }

            if (kPointLightControls[i].m_lightFog) {
                kFogPointArray[fogCount] = lightSettings;
                fogCount++;
            }

            if (kPointLightControls[i].m_lightClouds) {
                kCloudPointArray[cloudCount] = lightSettings;
                cloudCount++;
            }
        }
        
        if (fogLights != 0) {
            kFogPointComputeBuffer.SetData(kFogPointArray);
            cmd.SetGlobalBuffer("_ExpanseFogPointLights", kFogPointComputeBuffer);
        } else {
            cmd.SetGlobalBuffer("_ExpanseFogPointLights", IRenderer.kDefaultComputeBuffer);
        }
        if (cloudLights != 0) {
            kCloudPointComputeBuffer.SetData(kCloudPointArray);
            cmd.SetGlobalBuffer("_ExpanseCloudPointLights", kCloudPointComputeBuffer);
        } else {
            cmd.SetGlobalBuffer("_ExpanseCloudPointLights", IRenderer.kDefaultComputeBuffer);
        }
    }

    public static void build() {
        buildAtmoDirectionalLights();
        buildCloudDirectionalLights();
        buildCloudShadowDirectionalLights();
        buildFogPointLights();
        buildCloudPointLights();
    }

    public static void cleanup() {
        cleanupAtmoDirectionalLights();
        cleanupCloudDirectionalLights();
        cleanupCloudShadowDirectionalLights();
        cleanupFogPointLights();
        cleanupCloudPointLights();
    }

    public static void buildAtmoDirectionalLights() {
        if (kAtmosphereDirectionalComputeBuffer != null) {
            kAtmosphereDirectionalComputeBuffer.Release();
        }
        kAtmosphereDirectionalComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
    }

    public static void buildCloudDirectionalLights() {
        if (kCloudDirectionalComputeBuffer != null) {
            kCloudDirectionalComputeBuffer.Release();
        }
        kCloudDirectionalComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
    }

    public static void buildCloudShadowDirectionalLights() {
        if (kCloudShadowDirectionalComputeBuffer != null) {
            kCloudShadowDirectionalComputeBuffer.Release();
        }
        kCloudShadowDirectionalComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightRenderSettings)));
    }

    public static void buildFogPointLights() {
        if (kFogPointComputeBuffer != null) {
            kFogPointComputeBuffer.Release();
        }
        kFogPointComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightRenderSettings)));
    }

    public static void buildCloudPointLights() {
        if (kCloudPointComputeBuffer != null) {
            kCloudPointComputeBuffer.Release();
        }
        kCloudPointComputeBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightRenderSettings)));
    }

    public static void cleanupAtmoDirectionalLights() {
        if (kAtmosphereDirectionalComputeBuffer != null) {
            kAtmosphereDirectionalComputeBuffer.Release();
        }
        kAtmosphereDirectionalComputeBuffer = null;
    }

    public static void cleanupCloudDirectionalLights() {
        if (kCloudDirectionalComputeBuffer != null) {
            kCloudDirectionalComputeBuffer.Release();
        }
        kCloudDirectionalComputeBuffer = null;
    }

    public static void cleanupCloudShadowDirectionalLights() {
        if (kCloudShadowDirectionalComputeBuffer != null) {
            kCloudShadowDirectionalComputeBuffer.Release();
        }
        kCloudShadowDirectionalComputeBuffer = null;
    }

    public static void cleanupFogPointLights() {
        if (kFogPointComputeBuffer != null) {
            kFogPointComputeBuffer.Release();
        }
        kFogPointComputeBuffer = null;
    }

    public static void cleanupCloudPointLights() {
        if (kCloudPointComputeBuffer != null) {
            kCloudPointComputeBuffer.Release();
        }
        kCloudPointComputeBuffer = null;
    }
}

} // namespace Expanse
