#ifndef EXPANSE_ATMOSPHERE_MAPPING_INCLUDED
#define EXPANSE_ATMOSPHERE_MAPPING_INCLUDED

#include "../common/Utilities.hlsl"
#include "../common/Mapping.hlsl"

/**
 * @brief: static utility class for handling texture mapping for atmosphere.
 * */
class AtmosphereMapping {

  static float fromUnitToSubUVs(float u, float resolution) {
    const float j = 0.5;
    return (j + u * (resolution + 1 - 2 * j)) / (resolution + 1);
  }

  static float fromSubUVsToUnit(float u, float resolution) {
    const float j = 0.5;
    return (u * (resolution + 1) - j) / (resolution + 1 - 2 * j);
  }

  static float map_r_naive(float r, float atmosphereRadius, float planetRadius) {
    return (r - planetRadius) / (atmosphereRadius - planetRadius);
  }

  static float unmap_r_naive(float u_r, float atmosphereRadius, float planetRadius) {
    return planetRadius + u_r * (atmosphereRadius - planetRadius);
  }

  static float map_r(float r, float atmosphereRadius, float planetRadius) {
    return Utilities::safeSqrt((r - planetRadius) / (atmosphereRadius - planetRadius));
  }

  static float unmap_r(float u_r, float atmosphereRadius, float planetRadius) {
    return planetRadius + ((u_r * u_r) * (atmosphereRadius - planetRadius));
  }

  static float map_r_transmittance(float r, float atmosphereRadius,
    float planetRadius) {
    float planetRadiusSq = planetRadius * planetRadius;
    float rSq = r * r;
    float rho = Utilities::safeSqrt(rSq - planetRadiusSq);
    float H = Utilities::safeSqrt(atmosphereRadius * atmosphereRadius - planetRadiusSq);
    return rho / H;
  }

  static float2 map_r_mu_transmittance(float r, float mu, float atmosphereRadius,
    float planetRadius, float d, bool groundHit, float resMu) {
    float planetRadiusSq = planetRadius * planetRadius;
    float rSq = r * r;
    float rho = Utilities::safeSqrt(rSq - planetRadiusSq);
    float H = Utilities::safeSqrt(atmosphereRadius * atmosphereRadius - planetRadiusSq);

    float u_mu = 0.0;
    float muStep = 0.5/resMu;
    float discriminant = rSq * mu * mu - rSq + planetRadiusSq;
    if (groundHit) {
      float d_min = r - planetRadius;
      float d_max = rho;
      /* Use lower half of [0, 1] range. */
      u_mu = 0.5 - 0.5 * (d_max == d_min ? 0.0 : (d - d_min) / (d_max - d_min));
    } else {
      float d_min = atmosphereRadius - r;
      float d_max = rho + H;
      /* Use upper half of [0, 1] range. */
      u_mu = 0.5 + 0.5 * (d_max == d_min ? 0.0 : (d - d_min) / (d_max - d_min));
    }

    float u_r = rho / H;

    return float2(u_r, fromUnitToSubUVs(u_mu, resMu));
  }

  static float2 unmap_r_mu_transmittance(float u_r, float u_mu, float atmosphereRadius,
    float planetRadius, float resMu) {
    u_mu = fromSubUVsToUnit(u_mu, resMu);
    float planetRadiusSq = planetRadius * planetRadius;
    float H = Utilities::safeSqrt(atmosphereRadius * atmosphereRadius - planetRadiusSq);
    float rho = u_r * H;
    float r = Utilities::safeSqrt(rho * rho + planetRadiusSq);

    float mu = 0.0;
    if (u_mu < 0.5 + FLT_EPSILON) {
      float d_min = r - planetRadius;
      float d_max = rho;
      float d = d_min + (((0.5 - u_mu) / 0.5) * (d_max - d_min));
      mu = (d == 0.0) ? -1.0 : Utilities::clampCosine(-(rho * rho + d * d) / (2 * r * d));
    } else {
      float d_min = atmosphereRadius - r;
      float d_max = rho + H;
      float d = d_min + (((u_mu - 0.5) / 0.5) * (d_max - d_min));
      mu = (d == 0.0) ? 1.0 : Utilities::clampCosine((H * H - rho * rho - d * d) / (2 * r * d));
    }

    return float2(r, mu);
  }

  static float map_mu_naive(float mu, float resMu) {
    return fromUnitToSubUVs((mu + 1) / 2, resMu);
  }

  static float unmap_mu_naive(float u_mu, float resMu) {
    return (fromSubUVsToUnit(u_mu, resMu) * 2) - 1;
  }

  static float map_mu(float r, float mu, float atmosphereRadius, float planetRadius,
    float d, bool groundHit, float resMu) {
    float u_mu = 0.0;
    float muStep = 1.0/resMu; // HACK: technically this should be 0.5/resMu, but that causes an artifact
    float h = r - planetRadius;
    float cos_h = -Utilities::safeSqrt(h * (2 * planetRadius + h)) / (planetRadius + h);
    if (groundHit) {
      mu = min(mu, cos_h);
      u_mu = 0.5 * pow((cos_h - mu) / (1 + cos_h), 0.5);
      /* Clamping above 2*muStep here avoids bright artifacts on lower quality
       * settings. */
      u_mu = clamp(u_mu, muStep * 2, 0.5 - muStep);
    } else {
      mu = max(mu, cos_h);
      u_mu = 0.5 * pow((mu - cos_h) / (1 - cos_h), 0.5) + 0.5;
      u_mu = max(u_mu, 0.5 + muStep);
    }
    return fromUnitToSubUVs(u_mu, resMu);
  }

  static float unmap_mu(float u_r, float u_mu, float atmosphereRadius,
    float planetRadius, float resMu) {
    u_mu = fromSubUVsToUnit(u_mu, resMu);
    float r = planetRadius + ((u_r * u_r) * (atmosphereRadius - planetRadius));
    float mu = 0.0;
    float h = r - planetRadius;
    float cos_h = -Utilities::safeSqrt(h * (2 * planetRadius + h)) / (planetRadius + h);
    if (u_mu < 0.5) {
      u_mu = min(u_mu, 0.5 - 0.5/resMu);
      mu = Utilities::clampCosine(cos_h - pow(u_mu * 2, 2) * (1 + cos_h));
    } else {
      u_mu = max(u_mu, 0.5 + 0.5/resMu);
      mu = Utilities::clampCosine(pow(2 * (u_mu - 0.5), 2) * (1 - cos_h) + cos_h);
    }
    return mu;
  }

  static float unmap_mu_with_r(float r, float u_mu, float atmosphereRadius,
    float planetRadius, float resMu) {
    u_mu = fromSubUVsToUnit(u_mu, resMu);
    float mu = 0.0;
    float h = r - planetRadius;
    float cos_h = -Utilities::safeSqrt(h * (2 * planetRadius + h)) / (planetRadius + h);
    if (u_mu < 0.5) {
      u_mu = min(u_mu, 0.5 - 0.5/resMu);
      mu = Utilities::clampCosine(cos_h - pow(u_mu * 2, 2) * (1 + cos_h));
    } else {
      u_mu = max(u_mu, 0.5 + 0.5/resMu);
      mu = Utilities::clampCosine(pow(2 * (u_mu - 0.5), 2) * (1 - cos_h) + cos_h);
    }
    return mu;
  }

  static float map_mu_l(float mu_l) {
    return saturate((1.0 - exp(-3 * mu_l - 0.6)) / (1 - exp(-3.6)));
  }

  static float unmap_mu_l(float u_mu_l) {
    return Utilities::clampCosine((log(1.0 - (u_mu_l * (1 - exp(-3.6)))) + 0.6) / -3.0);
  }

  static float map_mu_l_naive(float mu_l) {
    return (mu_l + 1) / 2;
  }

  static float unmap_mu_l_naive(float u_mu_l) {
    return u_mu_l * 2 - 1;
  }

  static float map_nu(float nu) {
    float gamma = acos(nu) / PI;
    if (gamma >= 0.5) {
      return 0.5 + 0.5 * pow(2 * (gamma - 0.5), 1.5);
    } else {
      return 0.5 - 0.5 * pow(2 * (0.5 - gamma), 1.5);
    }
  }

  static float unmap_nu(float u_nu) {
    if (u_nu > 0.5) {
      float gamma = (pow(((u_nu - 0.5) / 0.5), 2/3) / 2) + 0.5;
      return cos(gamma * PI);
    } else {
      float gamma = 0.5 - (pow(((0.5 - u_nu) / 0.5), 2/3) / 2);
      return cos(gamma * PI);
    }
  }

  static float map_theta(float theta, float resTheta) {
    return fromUnitToSubUVs(theta / (2 * PI), resTheta);
  }

  static float unmap_theta(float u_theta, float resTheta) {
    return fromSubUVsToUnit(u_theta, resTheta) * 2 * PI;
  }

  /* Returns u_r, u_mu. */
  static float2 mapTransmittanceCoord(float r, float mu, float atmosphereRadius,
    float planetRadius, float d, bool groundHit, float resMu) {
    // return float2(map_r_naive(r, atmosphereRadius, planetRadius), map_mu_naive(mu, resMu));
    return map_r_mu_transmittance(r, mu, atmosphereRadius, planetRadius, d, groundHit, resMu);
  }

  /* Returns r, mu. */
  static float2 unmapTransmittanceCoord(float u_r, float u_mu,
    float atmosphereRadius, float planetRadius, float resMu) {
    // return float2(unmap_r_naive(u_r, atmosphereRadius, planetRadius), unmap_mu_naive(u_mu, resMu));
    return unmap_r_mu_transmittance(u_r, u_mu, atmosphereRadius, planetRadius, resMu);
  }

  /* Returns u_r, u_mu_l. */
  static float2 mapMSCoordinate(float r, float mu_l,
    float atmosphereRadius, float planetRadius) {
    return float2(map_r(r, atmosphereRadius, planetRadius), map_mu_l(mu_l));
  }

  /* Returns r, mu_l. */
  static float2 unmapMSCoordinate(float u_r, float u_mu_l, float atmosphereRadius,
    float planetRadius) {
    return float2(unmap_r(u_r, atmosphereRadius, planetRadius),
      unmap_mu_l(u_mu_l));
  }

  /* Returns u_mu, u_theta. */
  static float2 mapSkyRenderCoordinate(float r, float mu, float theta, float atmosphereRadius,
    float planetRadius, float d, bool groundHit, float resMu, float resTheta) {
    return float2(map_mu(r, mu, atmosphereRadius, planetRadius, d, groundHit, resMu),
      map_theta(theta, resTheta));
  }

  /* Returns mu, theta. */
  static float2 unmapSkyRenderCoordinate(float r, float u_mu, float u_theta,
    float atmosphereRadius, float planetRadius, float resMu, float resTheta) {
    return float2(unmap_mu_with_r(r, u_mu, atmosphereRadius, planetRadius, resMu),
      unmap_theta(u_theta, resTheta));
  }

  /* Returns d from mu and theta angles and view point. */
  static float3 mu_theta_to_d(float cos_mu, float theta, float3 O) {
    /* Construct local frame. */
    float3 y = normalize(O);
    float3 k = float3(1, 0, 0);
    float3 z = normalize(cross(y, k));
    float3 x = normalize(cross(z, y));
    /* Recover d via projection onto local axes. */
    float3 dy = cos_mu * y;
    float sin_mu = Utilities::safeSqrt(1 - cos_mu * cos_mu);
    float3 dx = sin_mu * cos(theta) * x;
    float3 dz = sin_mu * sin(theta) * z;
    /* The normalize shouldn't be necessary, but it is a good sanity check. */
    return normalize(dx + dy + dz);
  }

  /* Returns theta from view direction and view point. */
  static float d_to_theta(float3 d, float3 O) {
    /* Clamping d.x and d.z to be non-zero ensures numerical robustness,
     * and does away with the black fireflies we can sometimes otherwise
     * observe on the 2PI->0 boundary. */
    if (abs(d.x) < 0.0005) {
      d.x = 0.0005;
    }
    if (abs(d.z) < 0.0005) {
      d.z = 0.0005;
    }

    /* Construct local frame. */
    float3 y = normalize(O);
    float3 k = float3(1, 0, 0);
    float3 z = normalize(cross(y, k));
    float3 x = normalize(cross(z, y));
    /* Get cosine and sine of theta from projection onto local axes. */
    float3 dProj = normalize(d - dot(y, d) * y);
    float cosTheta = dot(dProj, x);
    float sinTheta = dot(dProj, z);
    float theta = acos(cosTheta);
    if (Utilities::floatLT(sinTheta, 0)) {
      theta = 2 * PI - theta;
    }
    /* This ensures that we don't sample directly on the theta boundary, which
     * fixes the small artifacts that can occur there. */
    if (theta < PI) {
      theta = clamp(theta, 0.0001, 0.9999 * PI);
    } else {
      theta = clamp(theta, 1.0001 * PI, 1.9999 * PI);
    }

    return theta;
  }

  /* Returns xyzw, where
   *  -xyz: world space direction.
   *  -z: depth, scaled according to clip space to ensure that we have a
   *  view-aligned plane.
   * */
  static float4 unmapFrustumCoordinate(float3 uvw, float2 screenSize,
    float farClip, float depthSkew, float4x4 pixelCoordToViewDirMatrix) {
    /* Clip space xy coordinate. */
    float2 xy = uvw.xy * screenSize;
    float3 clipSpaceD = -Mapping::getSkyViewDirWS(xy, pixelCoordToViewDirMatrix, _TaaJitterStrength.xy);

    /* Depth, or truly, ray length to the view plane. */
    float depth = pow(abs(uvw.z), depthSkew) * farClip;
    float3 cameraCenterD = -Mapping::getSkyViewDirWS((screenSize / 2), pixelCoordToViewDirMatrix, _TaaJitterStrength.xy);
    float cosTheta = dot(cameraCenterD, clipSpaceD);
    depth /= max(cosTheta, 0.00001);

    return float4(clipSpaceD, depth);
  }

  /* Far clip here has to be the ray distance to the far clipping plane---so it's a function 
   * of the pixel coordinate, it's larger for values further away from the camera center. */
  static float unmapFrustumW(float w, float farClip, float depthSkew) {
    return pow(abs(w), depthSkew) * farClip;
  }

  /* Maps linear depth to frustum coordinate. */
  static float3 mapFrustumCoordinate(float2 screenSpaceUV, float linear01Depth,
    float depthSkew) {
    return float3(screenSpaceUV, pow(saturate(linear01Depth), 1.0/depthSkew));
  }

};

#endif // EXPANSE_ATMOSPHERE_MAPPING_INCLUDED
