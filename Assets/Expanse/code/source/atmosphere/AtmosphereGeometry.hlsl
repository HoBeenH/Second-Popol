#ifndef EXPANSE_ATMOSPHERE_GEOMETRY_INCLUDED
#define EXPANSE_ATMOSPHERE_GEOMETRY_INCLUDED

#include "../common/Geometry.hlsl"
#include "../common/Utilities.hlsl"

/* Struct containing data for ray intersection queries made against the
 * sky volume. */
struct SkyIntersectionData {
  float startT, endT;
  bool groundHit, atmoHit;
};

/**
 * @brief: static utility class for handling intersections with the atmosphere
 * geometry.
 * */
class AtmosphereGeometry {

  /**
   * @brief: Traces a ray starting at point O in direction d. Returns
   * information about where the ray hit on the ground/on the boundary of the
   * atmosphere. */
  static SkyIntersectionData traceSkyVolume(float3 O, float3 d,
    float planetRadius, float atmosphereRadius) {
    /* Perform raw sphere intersections. */
    float3 t_ground = Geometry::intersectSphere(O, d, planetRadius);
    float3 t_atmo = Geometry::intersectSphere(O, d, atmosphereRadius);

    SkyIntersectionData toRet = {0, 0, false, false};

    /* We have a hit if the intersection was successful and if either point
     * is greater than zero (meaning we are in front of the ray, and not
     * behind it). */
    toRet.groundHit = t_ground.z >= 0.0 && (t_ground.x >= 0.0 || t_ground.y >= 0.0);
    toRet.atmoHit = t_atmo.z >= 0.0 && (t_atmo.x >= 0.0 || t_atmo.y >= 0.0);

    if (Utilities::floatLT(length(O), atmosphereRadius)) {
      /* We are below the atmosphere boundary, and we will start our raymarch
       * at the origin point. */
      toRet.startT = 0;
      if (toRet.groundHit) {
        /* We have hit the ground, and will end our raymarch at the first
         * positive ground hit. */
        toRet.endT = Utilities::minNonNegative(t_ground.x, t_ground.y);
      } else {
        /* We will end our raymarch at the first positive atmosphere hit. */
        toRet.endT = Utilities::minNonNegative(t_atmo.x, t_atmo.y);
      }
    } else {
      /* We are outside the atmosphere, and, if we intersect the atmosphere
       * at all, we will start our raymarch at the first atmosphere
       * intersection point. We don't need to be concerned about negative
       * t values, since it's a geometric impossibility to be outside a sphere
       * and intersect both in front of and behind a ray. */
      if (toRet.atmoHit) {
        toRet.startT = min(t_atmo.x, t_atmo.y);
        if (toRet.groundHit) {
          /* If we hit the ground at all, we'll end our ray at the first ground
           * intersection point. */
          toRet.endT = min(t_ground.x, t_ground.y);
        } else {
          /* Otherwise, we'll end our ray at the second atmosphere
           * intersection point. */
          toRet.endT = max(t_atmo.x, t_atmo.y);
        }
      }
      /* If we haven't hit the atmosphere, we leave everything uninitialized,
       * since this ray just goes out into space. */
    }

    return toRet;
  }

  /**
   * @brief: Traces a ray starting at point O in direction d. Returns
   * information about where the ray hit on the ground/on the boundary of the
   * atmosphere. This is an acceleration for the case when it is known that O
   * is within the sky volume. */
  static SkyIntersectionData traceSkyVolumeValid(float3 O, float3 d,
    float planetRadius, float atmosphereRadius) {
    /* Perform raw sphere intersections. */
    float t_ground = Geometry::traceSphere(O, d, planetRadius);
    float t_atmo = Geometry::traceSphere(O, d, atmosphereRadius);

    SkyIntersectionData toRet = {0, 0, false, false};

    /* We have a hit if either intersection t is greater than or equal to 0. */
    toRet.groundHit = t_ground >= 0.0;
    toRet.atmoHit = t_atmo >= 0.0;

    /* We are below the atmosphere boundary, and we will start our raymarch
     * at the origin point, which is already set. */
    if (toRet.groundHit) {
      /* We have hit the ground, and will end our raymarch at the ground hit. */
      toRet.endT = t_ground;
    } else {
      /* We will end our raymarch at the atmosphere hit. */
      toRet.endT = t_atmo;
    }

    return toRet;
  }

};

#endif // EXPANSE_ATMOSPHERE_GEOMETRY_INCLUDED
