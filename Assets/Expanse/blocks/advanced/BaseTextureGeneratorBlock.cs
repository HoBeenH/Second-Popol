using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

/**
 * Base class for blocks that can generate textures.
 * */
namespace Expanse {

[ExecuteInEditMode, Serializable]
public abstract class BaseTextureGeneratorBlock : MonoBehaviour
{
    /**
     * @brief: Get this texture generator's texture.
     * */
    public abstract RTHandle GetTexture();

    /**
     * @brief: Force this generator to update when it next gets
     * the chance (generally the next frame).
     * */
    public abstract void ForceUpdate();
}

} // namespace Expanse