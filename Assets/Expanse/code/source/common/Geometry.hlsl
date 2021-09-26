#ifndef EXPANSE_COMMON_GEOMETRY_INCLUDED
#define EXPANSE_COMMON_GEOMETRY_INCLUDED

#include "Utilities.hlsl"

/**
 * @brief: static utility class for various geometry operations.
 * */
class Geometry {

  /**
   * @brief: solves quadratic equation. Third component indicates if there
   * was a solution---it's negative if there was none.
   * */
  static float3 solveQuadratic(float A, float B, float C) {
    float det = (B * B) - 4.f * A * C;
    if (Utilities::floatGT(det, 0.0)) {
      det = Utilities::safeSqrt(det);
      float rcp2A = rcp(2 * A);
      return float3((-B + det) * rcp2A, (-B - det) * rcp2A, 1.0);
    }
    return -1;
  }

  /**
   * @brief: Intersects a sphere. Third coordinate is positive if there was
   * an intersection, and negative if there was not. First two coordinates
   * are intersection t's.
   * */
  static float3 intersectSphere(float3 p, float3 d, float r) {
    float A = dot(d, d);
    float B = 2.f * dot(d, p);
    float C = dot(p, p) - (r * r);
    return solveQuadratic(A, B, C);
  }

  /**
   * @brief: Intersects a sphere, but only returns the closest positive t
   * value. If negative, there was no positive intersection.
   * */
  static float traceSphere(float3 p, float3 d, float r) {
    float3 intersections = intersectSphere(p, d, r);
    return Utilities::minNonNegative(intersections.x, intersections.y);
  }

  /* Return t value of plane intersection. If negative, there was no
   * intersection at all, or if the intersection was outside the specified
   * bounds. */
  static float intersectXZAlignedPlane(float3 O, float3 d, float2 xExtent,
    float2 zExtent, float height) {
    /* Compute the plane intersection. */
    float t = (height - O.y) / d.y;
    float3 p = O + t * d;
    if (Utilities::boundsCheck(p.x, xExtent) && Utilities::boundsCheck(p.z, zExtent)) {
      return t;
    }
    return -1;
  }

  /* Return t value of plane intersection. If no intersection, return second value is negative. */
  static float2 intersectPlane(float3 p, float3 d, float3 n, float p0) {
    if (dot(d, n) < FLT_EPSILON) {
      return -1;
    }
    return float2(dot(p0 - p, n) / dot(d, n), 1);
  }

  /* Return entry (.x) and exit (.y) of box intersection.
   * If both are negative, there was no intersection.
   * If entry is negative but exit is not, this ray was cast from inside
   * the box. */
  static float2 intersectAxisAlignedBoxVolume(float3 O, float3 d, float2 xExtent,
    float2 yExtent, float2 zExtent) {
    float3 aabbMin = float3(xExtent.x, yExtent.x, zExtent.x);
    float3 aabbMax = float3(xExtent.y, yExtent.y, zExtent.y);

    float3 invD = rcp(d);
  	float3 t0s = (aabbMin - O) * invD;
    float3 t1s = (aabbMax - O) * invD;

    float3 tsmaller = min(t0s, t1s);
    float3 tbigger  = max(t0s, t1s);

    float tmin = -1;
    float tmax = FLT_MAX;

    tmin = max(tmin, max(tsmaller[0], max(tsmaller[1], tsmaller[2])));
    tmax = min(tmax, min(tbigger[0], min(tbigger[1], tbigger[2])));

  	if (tmax < tmin) {
      return float2(-1, -1);
    }

    return float2(max(0, tmin), tmax);
  }

  /**
   * @brief: Intersects a cone with specified angle, position, and inverse rotation.
   * First two coordinates are intersection t's. Last coordinate specifies if intersections
   * were valid (i.e. if quadratic was solvable).
   * @param p: ray start point.
   * @param d: ray direction.
   * @param o: position of the tip of the cone.
   * @param r_I: inverse rotation matrix specifying cone's orientation.
   * @param phi: cone angle.
   * */
  static float3 intersectCone(float3 p, float3 d, float3 o, float4x4 r_I, float phi) {
    // Transform ray to cone's frame.
    p -= o;
    p = mul(r_I, float4(p, 1)).xyz;
    d = mul(r_I, float4(d, 0)).xyz;
    // Intersect the cone. Assumes cone's axis is the z axis.
    float k = tan(phi/2);
    float A = dot(d * float3(-1, -1, k), d * float3(1, 1, k));
    float B = dot(2 * p * float3(-1, -1, k), d * float3(1, 1, k));
    float C = dot(p * float3(-1, -1, k), p * float3(1, 1, k));
    return solveQuadratic(A, B, C);
  }

  /**
   * @brief: samples depth buffer at given uv, but uses view direction
   * to scale that sample so that it's actually the distance from the eye
   * point. Returns -1 if the distance is beyond the far clipping plane.
   * */
   static float sampleCameraDistance(float2 uv, float3 d, float3 viewDir) {
     float linearDepth = Linear01Depth(SampleCameraDepth(uv), _ZBufferParams) * _ProjectionParams.z;
     float cosTheta = dot(viewDir, d);
     float rcpCosTheta = rcp(Utilities::clampAboveZero(cosTheta));
     float distance = linearDepth * rcpCosTheta;
     float farClip = _ProjectionParams.z * rcpCosTheta;
     return (distance < farClip - 0.001) ? distance : -1;
   }

};

#endif // EXPANSE_COMMON_GEOMETRY_INCLUDED
