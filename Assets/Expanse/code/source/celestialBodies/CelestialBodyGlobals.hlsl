#ifndef EXPANSE_CELESTIAL_BODY_GLOBALS_INCLUDED
#define EXPANSE_CELESTIAL_BODY_GLOBALS_INCLUDED

#include "CelestialBodyRenderSettings.cs.hlsl"

StructuredBuffer<CelestialBodyRenderSettings> _ExpanseCelestialBodies;
int _ExpanseNumCelestialBodies;

/* Sadly we have to have individual textures for each celestial body, because
 * they may all have different resolutions and because they are cubemaps. */
TEXTURECUBE(_ExpanseBodyAlbedoTex0);
TEXTURECUBE(_ExpanseBodyAlbedoTex1);
TEXTURECUBE(_ExpanseBodyAlbedoTex2);
TEXTURECUBE(_ExpanseBodyAlbedoTex3);
TEXTURECUBE(_ExpanseBodyAlbedoTex4);
TEXTURECUBE(_ExpanseBodyAlbedoTex5);
TEXTURECUBE(_ExpanseBodyAlbedoTex6);
TEXTURECUBE(_ExpanseBodyAlbedoTex7);
TEXTURECUBE(_ExpanseBodyEmissionTex0);
TEXTURECUBE(_ExpanseBodyEmissionTex1);
TEXTURECUBE(_ExpanseBodyEmissionTex2);
TEXTURECUBE(_ExpanseBodyEmissionTex3);
TEXTURECUBE(_ExpanseBodyEmissionTex4);
TEXTURECUBE(_ExpanseBodyEmissionTex5);
TEXTURECUBE(_ExpanseBodyEmissionTex6);
TEXTURECUBE(_ExpanseBodyEmissionTex7);

#endif // EXPANSE_CELESTIAL_BODY_GLOBALS_INCLUDED