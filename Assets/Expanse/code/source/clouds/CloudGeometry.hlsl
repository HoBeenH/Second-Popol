#ifndef EXPANSE_CLOUD_GEOMETRY_INCLUDED
#define EXPANSE_CLOUD_GEOMETRY_INCLUDED

#include "../common/Utilities.hlsl"
#include "../common/Geometry.hlsl"
#include "../common/Datatypes.cs.hlsl"
#include "../atmosphere/AtmosphereGeometry.hlsl"

/* Interface for cloud geometry object. */
interface ICloudGeometry {
  /**
   * @return: whether planet space point p is in bounds of the cloud geometry.
   * */
  bool inBounds(float3 p);

  /**
   * @return: uv coordinate for planet space point p, with tiling factor tile
   * and uv offset offset. Assumes coordinate is within the bounds of the
   * geometry.
   * */
  float3 mapCoordinate(float3 p, int3 tile, float3 offset);

  /**
   * @return: starting and ending intersection distances, as, correspondingly,
   * (x, y) if this is a 3D volume. Or, just single intersection, if a 2D
   * volume.
   * */
  float2 intersect(float3 p, float3 d);

  /**
   * @return: same as intersect, unless this is a 2D cloud geometry,
   * in which case something is faked.
   * */
  float2 intersect3D(float3 p, float3 d);

  /**
   * @return: dimension of this cloud volume, used for lighting calculations.
   */
  int dimension();

  /**
   * @return: given a point p, density attenuation distance and bias,
   * and density ramp up as (start distance, end distance), computes the 
   * density attenuation factor.
   */
  float densityAttenuation(float3 p, float distance, float bias, float2 rampUp);

  /**
   * @return: 0-1 value to use in height gradient computations.
   */
  float heightGradient(float3 p);
};




// TODO: none of these account for a planet origin offset!



/**
 * @brief: 2D xz-aligned plane.
 */
class CloudPlane : ICloudGeometry {
  float2 xExtent, yExtent, zExtent;
  float height, apparentThickness;

  /* Takes into account apparent thickness to make volumetric shadow queries
   * simpler. */
  bool inBounds(float3 p) {
    return Utilities::boundsCheck(p.x, xExtent)
      && Utilities::boundsCheck(p.y, yExtent)
      && Utilities::boundsCheck(p.z, zExtent);
  }

  /* Disregards y components of p and tile. */
  float3 mapCoordinate(float3 p, int3 tile, float3 offset) {
    float2 minimum = float2(xExtent.x, zExtent.x);
    float2 maximum = float2(xExtent.y, zExtent.y);
    float2 uv = (p.xz - minimum) / (maximum - minimum);
    uv = frac(uv * tile.xz + offset.xz);
    return float3(uv.x, 0, uv.y);
  }

  float2 intersect(float3 p, float3 d) {
    return Geometry::intersectXZAlignedPlane(p, d, xExtent, zExtent, height);
  }

  /* Useful for self-shadowing hacks. */
  float2 intersect3D(float3 p, float3 d) {
    return Geometry::intersectAxisAlignedBoxVolume(p, d, xExtent, yExtent, zExtent);
  }

  int dimension() {
    return NOISEDIMENSION_TWO_DIMENSIONAL;
  }

  float densityAttenuation(float3 p, float distance, float bias, float2 rampUp) {
    float2 origin = float2(dot(xExtent, float2(1, 1))/2, dot(zExtent, float2(1, 1))/2);
    float distFromOrigin = length(origin - p.xz);
    float rampUpAttenuation = saturate((distFromOrigin - rampUp.x) / Utilities::clampAboveZero(rampUp.y - rampUp.x));
    return saturate(rampUpAttenuation * exp(-(distFromOrigin-bias)/distance));
  }

  float heightGradient(float3 p) {
    return 0; // We have no meaningful height gradient.
  }

  static CloudPlane CreateCloudPlane(float2 xExtent, float2 zExtent, float height,
    float apparentThickness, float planetRadius) {
    CloudPlane c;
    c.xExtent = xExtent;
    c.zExtent = zExtent;
    c.height = height + planetRadius;
    c.apparentThickness = apparentThickness;
    c.yExtent = float2(c.height - apparentThickness/2, c.height + apparentThickness/2);
    return c;
  }

};








/**
 * @brief: Subsection of a sphere around the planet.
 */
class CloudCurvedPlane : ICloudGeometry {
  float2 rExtent, xAngleExtent, zAngleExtent, xExtent, zExtent;
  float radius, apparentThickness;

  /* Takes into account apparent thickness to make volumetric shadow queries
   * simpler. */
  bool inBounds(float3 p) {
    float r = length(p);
    float sinXAngle = p.x/radius;
    float sinZAngle = p.z/radius;
    return Utilities::boundsCheck(r, rExtent)
      && Utilities::boundsCheck(sinXAngle, xAngleExtent)
      && Utilities::boundsCheck(sinZAngle, zAngleExtent);
  }

  /* Disregards y components of p and tile. */
  float3 mapCoordinate(float3 p, int3 tile, float3 offset) {
    float2 minimum = float2(xAngleExtent.x, zAngleExtent.x);
    float2 maximum = float2(xAngleExtent.y, zAngleExtent.y);
    float2 sinAngles = float2(p.x/radius, p.z/radius);
    float2 uv = (sinAngles - minimum) / (maximum - minimum);
    uv = frac(uv * tile.xz + offset.xz);
    return float3(uv.x, 0, uv.y);
  }

  float2 intersect(float3 p, float3 d) {
    float3 intersection = Geometry::intersectSphere(p, d, radius);
    if (intersection.z > 0 && (intersection.x > 0 || intersection.y > 0)) {
      float t = Utilities::minNonNegative(intersection.x, intersection.y);
      float3 o = p + t * d;
      float sinXAngle = o.x/radius;
      float sinZAngle = o.z/radius;
      if (Utilities::boundsCheck(sinXAngle, xAngleExtent) && Utilities::boundsCheck(sinZAngle, zAngleExtent)) {
        return float2(t, t);
      }
      if (intersection.x > 0 && intersection.y > 0) {
        /* If the second intersection was valid, bounds check it as well. */
        t = max(intersection.x, intersection.y);
        o = p + t * d;
        sinXAngle = o.x/radius;
        sinZAngle = o.z/radius;
        if (Utilities::boundsCheck(sinXAngle, xAngleExtent) && Utilities::boundsCheck(sinZAngle, zAngleExtent)) {
          return float2(t, t);
        }
      }
    }
    return float2(-1, -1);
  }

  /* Useful for self-shadowing hacks. */
  float2 intersect3D(float3 p, float3 d) {
    // // We can use our existing sky intersection logic to intersect 2 spheres---
    // // the lower and upper boundaries of the cloud layer---at once.
    // SkyIntersectionData intersection = AtmosphereGeometry::traceSkyVolumeValid(p, d, rExtent.x, rExtent.y);
    // float3 o = p + intersection.endT * d;
    // float sinXAngle = o.x/radius;
    // float sinZAngle = o.z/radius;
    // if (Utilities::boundsCheck(sinXAngle, xAngleExtent) && Utilities::boundsCheck(sinZAngle, zAngleExtent)) {
    //   return float2(intersection.endT, intersection.endT);
    // }
    // return float2(-1, -1);
    // In theory, this ^ is how we should compute this intersection. However, this gives
    // weird results when the sun is near the horizon, so instead we fall back on an AABB
    // intersection, which is good enough.
    return Geometry::intersectAxisAlignedBoxVolume(p, d, xExtent, rExtent, zExtent);
  }

  int dimension() {
    return NOISEDIMENSION_TWO_DIMENSIONAL;
  }

  float densityAttenuation(float3 p, float distance, float bias, float2 rampUp) {
    // HACK: just use x and z extents directly to determine the origin.
    float2 origin = float2(dot(xExtent, float2(1, 1))/2, dot(zExtent, float2(1, 1))/2);
    float distFromOrigin = length(origin - p.xz);
    float rampUpAttenuation = saturate((distFromOrigin - rampUp.x) / Utilities::clampAboveZero(rampUp.y - rampUp.x));
    return saturate(rampUpAttenuation * exp(-(distFromOrigin-bias)/distance));
  }

  float heightGradient(float3 p) {
    return 0; // We have no meaningful height gradient.
  }

  static CloudCurvedPlane CreateCloudCurvedPlane(float2 xExtent, float2 zExtent,
    float height, float apparentThickness, float planetRadius) {
    CloudCurvedPlane c;
    c.radius = height + planetRadius;
    c.apparentThickness = apparentThickness;
    c.rExtent = float2(c.radius - apparentThickness/2, c.radius + apparentThickness/2);
    /* We assume that the bounds have been provided as an arc length, from which
     * we can readily extract the subtended angle. */
    c.xExtent = xExtent;
    c.zExtent = zExtent;
    c.xAngleExtent = sin(xExtent/c.radius);
    c.zAngleExtent = sin(zExtent/c.radius);
    return c;
  }

};








/**
 * @brief: 3D axis-aligned box.
 */
class CloudBoxVolume : ICloudGeometry {
  float2 xExtent, yExtent, zExtent;
  float3 origin;

  bool inBounds(float3 p) {
    return Utilities::boundsCheck(p.x, xExtent)
      && Utilities::boundsCheck(p.y, yExtent)
      && Utilities::boundsCheck(p.z, zExtent);
  }

  float3 mapCoordinate(float3 p, int3 tile, float3 offset) {
    // Here we use the x extent as the distance the y coordinate spans,
    // so that the aspect ratio of the noises doesn't get wonky and we
    // don't have to deal with sub-1 grid sizes for our noise.
    float3 minimum = float3(xExtent.x, yExtent.x, zExtent.x);
    float3 maximum = float3(xExtent.y, yExtent.y + (xExtent.y - xExtent.x), zExtent.y);
    float3 uv = (p - minimum) / (maximum - minimum);
    return frac(uv * tile + offset);
  }

  float2 intersect(float3 p, float3 d) {
    return Geometry::intersectAxisAlignedBoxVolume(p, d, xExtent, yExtent, zExtent);
  }

  float2 intersect3D(float3 p, float3 d) {
    return intersect(p, d);
  }

  int dimension() {
    return NOISEDIMENSION_THREE_DIMENSIONAL;
  }

  float densityAttenuation(float3 p, float distance, float bias, float2 rampUp) {
    // Only attenuate based on x-z distance.
    float distFromOrigin = length(origin.xz - p.xz);
    float rampUpAttenuation = saturate((distFromOrigin - rampUp.x) / Utilities::clampAboveZero(rampUp.y - rampUp.x));
    return saturate(rampUpAttenuation * exp(-(distFromOrigin-bias)/distance));
  }

  float heightGradient(float3 p) {
    return (p.y - yExtent.x) / (yExtent.y - yExtent.x);
  }

  static CloudBoxVolume CreateCloudBoxVolume(float2 xExtent, float2 yExtent,
    float2 zExtent, float planetRadius) {
    CloudBoxVolume c;
    c.xExtent = xExtent;
    c.yExtent = yExtent + planetRadius;
    c.zExtent = zExtent;
    c.origin = float3(dot(c.xExtent, float2(0.5, 0.5)),
      dot(c.yExtent, float2(0.5, 0.5)),
      dot(c.zExtent, float2(0.5, 0.5)));
    return c;
  }

};








/**
 * @brief: 3D axis-aligned box.
 */
class CloudCurvedBoxVolume : ICloudGeometry {
  float2 xExtent, rExtent, zExtent, xAngleExtent, zAngleExtent;
  float3 origin;
  float radius, planetRadius;

  bool inBounds(float3 p) {
    float r = length(p);
    float rcpR = rcp(r);
    float sinXAngle = p.x * rcpR;
    float sinZAngle = p.z * rcpR;
    return Utilities::boundsCheck(r, rExtent)
      && Utilities::boundsCheck(sinXAngle, xAngleExtent)
      && Utilities::boundsCheck(sinZAngle, zAngleExtent);
  }

  bool inBoundsXZ(float3 p) {
    float r = length(p);
    float rcpR = rcp(r);
    float sinXAngle = p.x * rcpR;
    float sinZAngle = p.z * rcpR;
    return Utilities::boundsCheck(sinXAngle, xAngleExtent)
      && Utilities::boundsCheck(sinZAngle, zAngleExtent);
  }

  float3 mapCoordinate(float3 p, int3 tile, float3 offset) {
    float3 minimum = float3(xAngleExtent.x, rExtent.x, zAngleExtent.x);
    float3 maximum = float3(xAngleExtent.y, rExtent.y + (xExtent.y - xExtent.x), zAngleExtent.y);
    float r = length(p);
    float rcpR = rcp(r);
    float3 sinAnglesAndR = float3(p.x * rcpR, r, p.z * rcpR);
    float3 uv = (sinAnglesAndR - minimum) * rcp(maximum - minimum);
    return frac(uv * tile + offset);
  }

  float2 intersect(float3 p, float3 d) {
    // HACK: we have no bounds checking here.
    // Intersect both spheres.
    float3 inner = Geometry::intersectSphere(p, d, rExtent.x);
    float3 outer = Geometry::intersectSphere(p, d, rExtent.y);

    bool innerHit = (inner.x > 0) || (inner.y > 0);
    bool outerHit = (outer.x > 0) || (outer.y > 0);

    float2 result = -1;

    if (innerHit) {
      if (outerHit) {
        // We had both an inner hit and an outer hit.
        bool bothInnerHit = inner.x > 0 && inner.y > 0;
        bool bothOuterHit = outer.x > 0 && outer.y > 0;
        if (bothOuterHit) {
          if  (bothInnerHit) {
            // We had all hits. This means we're outside the volume looking down.
            // Return the outer layer's min hit follows by the inner layer's min
            // hit.
            result = float2(min(outer.x, outer.y), min(inner.x, inner.y));
          }
          // It's not possible to have two outer hits and one inner hit.
        } else if (bothInnerHit) {
          // We had one outer hit and two inner hits. We are looking
          // down through the clouds from inside the cloud layer.
          // We have to check if we intersect with the planet to decide
          // which end point to take.
          float3 planetHit = Geometry::intersectSphere(p, d, planetRadius);
          if (planetHit.x > 0 || planetHit.y > 0) {
            // We hit the planet, use the inner hit as the end point.
            result = float2(0, Utilities::minNonNegative(inner.x, inner.y));
          } else {
            // We didn't hit the planet---we have 3 intersection points.
            // Choose the outer point.
            result = float2(0, max(outer.x, outer.y));
          }
        } else {
          // We are in the usual place of looking up through the clouds.
          result = float2(Utilities::minNonNegative(inner.x, inner.y), Utilities::minNonNegative(outer.x, outer.y));
        }
      }
      // It is not possible to have an inner hit without an outer hit.
    } else if (outerHit) {
      // We had only an outer hit.
      if (outer.x < 0 || outer.y < 0) {
        // We've only got one valid outer hit. Select the non-negative one.
        result = float2(0, max(outer.x, outer.y));
      } else {
        // .x and .y were both hits.
        result = float2(min(outer.x, outer.y), max(outer.x, outer.y));
      }
    }
    
    if (result.x >= 0 && !inBoundsXZ(p + result.x * d)) {
      result = -1;
    }

    return result;
  }

  float2 intersect3D(float3 p, float3 d) {
    return intersect(p, d);
  }

  int dimension() {
    return NOISEDIMENSION_THREE_DIMENSIONAL;
  }

  float densityAttenuation(float3 p, float distance, float bias, float2 rampUp) {
    // HACK: just use x and z extents directly to determine the origin.
    float distFromOrigin = length(origin.xz - p.xz);
    float rampUpAttenuation = saturate((distFromOrigin - rampUp.x) / Utilities::clampAboveZero(rampUp.y - rampUp.x));
    return saturate(rampUpAttenuation * exp(-(distFromOrigin-bias)/distance));
  }

  float heightGradient(float3 p) {
    return (length(p) - rExtent.x) * rcp(rExtent.y - rExtent.x);
  }

  static CloudCurvedBoxVolume CreateCloudCurvedBoxVolume(float2 xExtent, float2 yExtent,
    float2 zExtent, float planetRadius) {
    CloudCurvedBoxVolume c;
    c.rExtent = yExtent + planetRadius;
    c.radius = (c.rExtent.x + c.rExtent.y) / 2.0;
    /* We assume that the bounds have been provided as an arc length, from which
     * we can readily extract the subtended angle. */
    c.xExtent = xExtent;
    c.zExtent = zExtent;
    c.xAngleExtent = sin(xExtent/c.radius);
    c.zAngleExtent = sin(zExtent/c.radius);
    c.origin = float3(dot(c.xExtent, float2(0.5, 0.5)),
      dot(c.rExtent, float2(0.5, 0.5)),
      dot(c.zExtent, float2(0.5, 0.5)));
    c.planetRadius = planetRadius;
    return c;
  }

};

#endif // EXPANSE_CLOUD_GEOMETRY_INCLUDED
