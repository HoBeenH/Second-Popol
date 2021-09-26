#ifndef EXPANSE_NEBULA_GLOBALS_INCLUDED
#define EXPANSE_NEBULA_GLOBALS_INCLUDED

#include "NebulaRenderSettings.cs.hlsl"

StructuredBuffer<NebulaRenderSettings> _ExpanseNebula;
StructuredBuffer<NebulaGeneratorLayerSettings> _ExpanseNebulaLayers;
TEXTURECUBE(_ExpanseTextureNebulaTexture);
int _ExpanseHasNebulaTexture;
int _ExpanseNebulaProcedural;

#endif // EXPANSE_NEBULA_GLOBALS_INCLUDED