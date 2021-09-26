#ifndef EXPANSE_STAR_GLOBALS_INCLUDED
#define EXPANSE_STAR_GLOBALS_INCLUDED

#include "StarRenderSettings.cs.hlsl"

StructuredBuffer<StarRenderSettings> _ExpanseStars;
TEXTURECUBE(_ExpanseTextureStarTexture);
int _ExpanseHasStarTexture;
int _ExpanseStarsProcedural;

#endif // EXPANSE_STAR_GLOBALS_INCLUDED