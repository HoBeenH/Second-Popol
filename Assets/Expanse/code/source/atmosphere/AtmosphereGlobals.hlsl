#ifndef EXPANSE_ATMOSPHERE_GLOBALS_INCLUDED
#define EXPANSE_ATMOSPHERE_GLOBALS_INCLUDED

#include "AtmosphereLayerRenderSettings.cs.hlsl"

StructuredBuffer<AtmosphereLayerRenderSettings> _ExpanseAtmosphereLayers;
StructuredBuffer<AtmosphereLayerRenderSettings> _ExpanseFogLayers;
int _ExpanseNumAtmosphereLayers;
int _ExpanseNumFogLayers;

#endif // EXPANSE_ATMOSPHERE_GLOBALS_INCLUDED