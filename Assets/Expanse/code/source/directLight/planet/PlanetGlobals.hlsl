#ifndef EXPANSE_PLANET_GLOBALS_INCLUDED
#define EXPANSE_PLANET_GLOBALS_INCLUDED

#include "PlanetRenderSettings.cs.hlsl"

StructuredBuffer<PlanetRenderSettings> _ExpansePlanetRenderSettings;
TEXTURECUBE(_ExpansePlanetAlbedoTexture);
TEXTURECUBE(_ExpansePlanetEmissionTexture);

#endif