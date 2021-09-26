using System.Collections.Generic;
using UnityEngine;

namespace Expanse {

/**
 * @brief: global class for controlling UI blocks.
 * */
[ExecuteInEditMode]
public class BlockControl : MonoBehaviour
{
    /* Arrays for keeping track of what cloud/atmo/body indices are free.
     * If true, index is occupied. */
    private static bool[] m_cloudIndices = new bool[CloudDatatypes.kMaxCloudLayers];
    private static bool[] m_atmosphereIndices = new bool[AtmosphereDatatypes.kMaxAtmosphereLayers];
    private static bool[] m_celestialBodyIndices = new bool[CelestialBodyDatatypes.kMaxCelestialBodies];

    static BlockControl() {
        /* Guarantee everything is initialized to false. */
        for (int i = 0; i < m_cloudIndices.Length; i++) {
            m_cloudIndices[i] = false;
        }
        for (int i = 0; i < m_atmosphereIndices.Length; i++) {
            m_atmosphereIndices[i] = false;
        }
        for (int i = 0; i < m_celestialBodyIndices.Length; i++) {
            m_celestialBodyIndices[i] = false;
        }
    }

    /** 
     * @return: cloud layer index, upon successful registration. -1 upon failure;
     * */
    public static int registerCloudLayer() {
        return registerIndex(m_cloudIndices);
    }

    /** 
     * @return: atmosphere layer index, upon successful registration. -1 upon failure;
     * */
    public static int registerAtmosphereLayer() {
        return registerIndex(m_atmosphereIndices);
    }

    /** 
     * @return: celestial body index, upon successful registration. -1 upon failure;
     * */
    public static int registerCelestialBody() {
        return registerIndex(m_celestialBodyIndices);
    }

    /** 
     * @brief: frees cloud layer index.
     * */
    public static void deregisterCloudLayer(int index) {
        deregisterIndex(index, m_cloudIndices);
    }

    /** 
     * @return: frees atmosphere layer index.
     * */
    public static void deregisterAtmosphereLayer(int index) {
        deregisterIndex(index, m_atmosphereIndices);
    }

    /** 
     * @return: frees celestial body index.
     * */
    public static void deregisterCelestialBody(int index) {
        deregisterIndex(index, m_celestialBodyIndices);
    }

    private static int registerIndex(bool[] layerIndices, HashSet<int> reserved=null) {
        for (int i = 0; i < layerIndices.Length; i++) {
            if (!layerIndices[i] && (reserved == null || !reserved.Contains(i))) {
                layerIndices[i] = true;
                return i;
            }
        }
        return -1;
    }

    private static void deregisterIndex(int i, bool[] layerIndices) {
        if (i >= 0 && i < layerIndices.Length) {
            layerIndices[i] = false;
        }
    }

}

} // namespace Expanse