using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

/**
 * Base class for blocks that need to access Expanse's settings.
 * */ //
namespace Expanse {

[ExecuteInEditMode, Serializable]
public abstract class BaseCloudLayerBlock : MonoBehaviour
{
    protected void OnEnable() 
    {
        CloudLayerRenderSettings.register(this);
    }

    protected void OnDisable() 
    {
        CloudLayerRenderSettings.deregister(this);
    }

    /**
     * @return: Universal representation of this cloud layer.
     * */
    public abstract UniversalCloudLayer ToUniversal();

    /**
     * @brief: Set this cloud layer's values from a universal representation.
     * Is allowed to not handle certain attributes.
     * */
    public abstract void FromUniversal(UniversalCloudLayer from, bool bypassOffset=false);

    /**
     * @brief: Set the texture for a particular noise layer. This will automatically 
     * make this noise layer non-procedural, if such a concept exists on the implementation
     * block. This may also fail depending on the type of cloud block it is called on---the
     * return value will indicate success or failure, if that is important.
     * */
    public abstract bool SetTexture(CloudDatatypes.CloudNoiseLayer noiseLayer, Texture texture, int tile);

    /**
     * @brief: saves current universal representation of cloud layer to disk.
     * */
    public void SaveUniversal(string filepath) {
        UniversalCloudLayer.save(ToUniversal(), filepath);
    }

    /**
     * @brief: restores universal representation of cloud layer from a saved file 
     * on disk.
     * */
    public void LoadUniversal(string filepath) {
        // Set this layer's values from the loaded universal layer
        FromUniversal(UniversalCloudLayer.load(filepath));
    }

}

} // namespace Expanse