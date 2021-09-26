//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef QUALITYRENDERSETTINGS_CS_HLSL
#define QUALITYRENDERSETTINGS_CS_HLSL
// Generated from Expanse.QualityRenderSettings
// PackingRules = Exact
struct QualityRenderSettings
{
    int samplesT;
    int samplesAP;
    int samplesSS;
    int samplesMS;
    int samplesMSAcc;
    int samplesScreenspace;
    int samplesScreenspaceScattering;
    int importanceSample;
    int AP_importanceSample;
    int screenspaceImportanceSample;
    float AP_depthSkew;
    float screenspace_depthSkew;
    int screenspace_historyFrames;
    int downsampledDepthMip;
    int antiAlias;
    int dither;
    float cloudShadowMapFilmPlaneScale;
};


#endif
