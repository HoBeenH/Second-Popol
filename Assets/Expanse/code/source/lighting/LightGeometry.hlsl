#ifndef EXPANSE_LIGHT_GEOMETRY_INCLUDED
#define EXPANSE_LIGHT_GEOMETRY_INCLUDED

#include "../common/Geometry.hlsl"
#include "../common/Utilities.hlsl"
#include "LightingRenderSettings.cs.hlsl"

/* Interface for light geometry object. */
interface ILightGeometry {

  /**
  * @brief: intersects a ray with a light volume specified by the
  * given light struct. Returns:
  *   x: start point
  *   y: end point
  *   z: start bounding point to compute relative to for integration
  *   w: end bounding point to compute relative to for integration
  * 
  * If there's no intersection, tStart will be negative.
  * */
  float4 intersect(float3 p, float3 d);

  /**
   * @brief: integrates attenuation factor along a given ray. Also returns the result of intersect().
   * */
  void integrate(float3 p, float3 d, float maxDistance, out float4 intersection, out float integratedAttenuation);

  /**
   * @brief: raymarches attenuation factor along a given ray. Also returns the result of intersect().
   * */
  void raymarch(float3 p, float3 d, float maxDistance, int samples, out float4 intersection, out float integratedAttenuation);
};

/**
 * @brief: Simple point light with radius.
 */
class PointLightVolume : ILightGeometry {
  float range, cullRadius;
  float3 position;

  float4 intersect(float3 p, float3 d) {
    // All the geometry types require a bound intersection.
    float3 boundIntersection = Geometry::intersectSphere(p - position, d, range * cullRadius);
    if (!(boundIntersection.x > 0 || boundIntersection.y > 0) || boundIntersection.z < 0) {
        return -1;
    }
    float2 bound = float2(min(boundIntersection.x, boundIntersection.y), 
        max(boundIntersection.x, boundIntersection.y));
    return float4(max(0, bound.x), bound.y, bound.x, bound.y);
  }

  void integrate(float3 p, float3 d, float maxDistance, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        float tMaxIntersect = intersection.w;
        float tStart = intersection.x;
        intersection.y = min(intersection.y, maxDistance);
        float tEnd = intersection.y;
        float tMinIntersect = intersection.z;
        float3 startWS = p + d * tStart;
        // This is an analytical integration of the attenuation function 1/((d/r)^2 + 1).
        // However, since the light unit is specified in lumens, we can guarantee that r is 1.
        // Chord across circle
        float T = tMaxIntersect - tMinIntersect;
        float x0 = (tStart - tMinIntersect) - T/2.0;
        float xf = (tEnd - tMinIntersect) - T/2.0;
        float d0 = length(startWS - position);
        float k = d0 * d0 - x0 * x0;
        float rcpSqrtKR = rcp(sqrt(k + 1));
        // Integrate x < 0 and x > 0 separately
        integratedAttenuation = abs(rcpSqrtKR * (atan(xf * rcpSqrtKR) - atan(x0 * rcpSqrtKR))); 
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }

  void raymarch(float3 p, float3 d, float maxDistance, int samples, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        intersection.y = min(intersection.y, maxDistance);
        float tStart = intersection.x;
        float tEnd = intersection.y;
        float3 startWS = p + d * tStart;
        float dt = tEnd - tStart;
        float localDt = dt / samples;
        for (int j = 0; j < samples; j++) {
            float3 samplePoint = startWS + ((j + 0.5) / (float) samples) * dt * d;
            float attenuation = rcp(dot(samplePoint - position, samplePoint - position) + 1);
            integratedAttenuation += attenuation * localDt;
        }
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }

  static PointLightVolume CreatePointLightVolume(float3 position, float range, float cullRadius) {
    PointLightVolume v;
    v.position = position;
    v.range = range;
    v.cullRadius = cullRadius;
    return v;
  }
};

/**
 * @brief: Spot light with cone shape.
 */
class ConeLightVolume : ILightGeometry {
  float range, cullRadius, angle;
  float3 position, axis;
  float4x4 rotation, inverseRotation;

  float4 intersect(float3 p, float3 d) {
    // All the geometry types require a bound intersection.
    float3 boundIntersection = Geometry::intersectSphere(p - position, d, range * cullRadius);
    if (!(boundIntersection.x > 0 || boundIntersection.y > 0) || boundIntersection.z < 0) {
        return -1;
    }
    float2 bound = float2(min(boundIntersection.x, boundIntersection.y), 
        max(boundIntersection.x, boundIntersection.y));

    float3 intersection = Geometry::intersectCone(p, d, position, inverseRotation, angle);
    if (intersection.z < 0) {
        return -1;
    }
    
    // Default to sphere intersection.
    float tStart = max(0, bound.x);
    float tEnd = bound.y;
    
    // Have a few cases to sort out.
    // First, determine a few things about the intersection.
    float2 cone = float2(min(intersection.x, intersection.y), max(intersection.x, intersection.y));
    bool insideLit = dot(normalize(p - position), axis) > cos(angle/2.0);
    bool insideUnlit = -dot(normalize(p - position), axis) > cos(angle/2.0);
    bool bothPositive = cone.x > 0 && cone.y > 0;
    bool bothNegative = cone.x < 0 && cone.y < 0;
    
    if (insideLit) {
        tStart = 0;
        if (bothPositive) {
            tEnd = cone.x;
        } else if (bothNegative) {
            tEnd = bound.y;
        } else {
            tEnd = cone.y;
        }
    }
    else if (insideUnlit) {
        if (bothPositive) {
            if (cone.y > bound.y) {
                return -1;
            } else {
                tStart = cone.y;
                tEnd = bound.y;
            }
        } else {
            return -1;
        }
    } 
    else {
        if (bothPositive) {
            if (cone.x > bound.y) {
                return -1;
            } else {
                bool wrongSide = dot(normalize(p + cone.x * d - position), axis) < 0;
                if (wrongSide) {
                    return -1;
                } else {
                    tStart = cone.x;
                    tEnd = min(cone.y, bound.y);
                }
            }
        } else if (bothNegative) {
            return -1;
        } else {
            if (cone.y > bound.y) {
                return -1;
            } else {
                bool wrongSide = dot(normalize(p + cone.y * d - position), axis) < 0;
                if (wrongSide) {
                    return -1;
                } else {
                    tStart = cone.y;
                    tEnd = bound.y;
                }
            }
        }
    }

    return float4(tStart, tEnd, bound.x, bound.y);
  }

  void integrate(float3 p, float3 d, float maxDistance, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        float tMaxIntersect = intersection.w;
        float tStart = intersection.x;
        intersection.y = min(intersection.y, maxDistance);
        float tEnd = intersection.y;
        float tMinIntersect = intersection.z;
        float3 startWS = p + d * tStart;
        // This is an analytical integration of the attenuation function 1/((d/r)^2 + 1).
        // However, since the light unit is specified in lumens, we can guarantee that r is 1.
        // Chord across circle
        float T = tMaxIntersect - tMinIntersect;
        float x0 = (tStart - tMinIntersect) - T/2.0;
        float xf = (tEnd - tMinIntersect) - T/2.0;
        float d0 = length(startWS - position);
        float k = d0 * d0 - x0 * x0;
        float rcpSqrtKR = rcp(sqrt(k + 1));
        integratedAttenuation = abs(rcpSqrtKR * (atan(xf * rcpSqrtKR) - atan(x0 * rcpSqrtKR))); 
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }

  void raymarch(float3 p, float3 d, float maxDistance, int samples, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        intersection.y = min(intersection.y, maxDistance);
        float tStart = intersection.x;
        float tEnd = intersection.y;
        float3 startWS = p + d * tStart;
        float dt = tEnd - tStart;
        float localDt = dt / samples;
        for (int j = 0; j < samples; j++) {
            float3 samplePoint = startWS + ((j + 0.5) / (float) samples) * dt * d;
            float attenuation = rcp(dot(samplePoint - position, samplePoint - position) + 1);
            integratedAttenuation += attenuation * localDt;
        }
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }


  static ConeLightVolume CreateConeLightVolume(float3 position, float angle, float4x4 rotation,
    float4x4 inverseRotation, float range, float cullRadius) {
    ConeLightVolume v;
    v.position = position;
    v.angle = angle;
    v.rotation = rotation;
    v.inverseRotation = inverseRotation;
    v.axis = mul(rotation, float4(0, 0, 1, 0)).xyz;
    v.range = range;
    v.cullRadius = cullRadius;
    return v;
  }
};

/**
 * @brief: Spot light with box shape.
 */
class BoxLightVolume : ILightGeometry {
  float range, cullRadius, sizeX, sizeY;
  float3 position, axis;
  float4x4 rotation, inverseRotation;

  float4 intersect(float3 p, float3 d) {
    float3 boundIntersection = Geometry::intersectSphere(p - position, d, range * cullRadius);
    if (!(boundIntersection.x > 0 || boundIntersection.y > 0) || boundIntersection.z < 0) {
        return -1;
    }
    float2 bound = float2(min(boundIntersection.x, boundIntersection.y), 
        max(boundIntersection.x, boundIntersection.y));

    float3 pTransformed = p - position;
    pTransformed = mul(inverseRotation, float4(pTransformed, 1)).xyz;
    float3 dTransformed = mul(inverseRotation, float4(d, 0)).xyz;
    float2 intersection = Geometry::intersectAxisAlignedBoxVolume(pTransformed, dTransformed, float2(-sizeX/2, sizeX/2),
    float2(-sizeY/2, sizeY/2), float2(0, range * cullRadius));
    float tStart = min(intersection.x, intersection.y);
    float tEnd = max(intersection.x, intersection.y);
    return float4(tStart, tEnd, bound.x, bound.y);
  }

  void integrate(float3 p, float3 d, float maxDistance, out float4 intersection, out float integratedAttenuation) {
        // TODO: need to redo math here.
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        float tStart = intersection.x;
        intersection.y = min(intersection.y, maxDistance);
        float tEnd = intersection.y;
        float dt = tEnd - tStart;
        float3 startWS = p + d * tStart;
        // This is an analytical integration of the attenuation function 1/((d/r)^2 + 1).
        // However, since the light unit is specified in lumens, we can guarantee that r is 1.
        // Here, d specifies the distance from the plane, not from the position. This is an
        // approximation to actually integrating over the whole area light.
        float a = dot(startWS - position, axis) + 1;
        float c = dot(d, axis);
        float b = 2 * a * c;
        a *= a;
        c *= c;
        // Clamping above 0.0001 seems to give us reasonable numerical stability, but TODO: 
        // still have some slight issues.
        float rcpDisc = rcp(max(0.0001, Utilities::safeSqrt(4 * a * c - b * b)));
        integratedAttenuation = 2 * rcpDisc * (atan((b + 2 * c * dt) * rcpDisc) - atan(b * rcpDisc)); 
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }

  void raymarch(float3 p, float3 d, float maxDistance, int samples, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        intersection.y = min(intersection.y, maxDistance);
        float tStart = intersection.x;
        float tEnd = intersection.y;
        float3 startWS = p + d * tStart;
        float dt = tEnd - tStart;
        float localDt = dt / samples;
        for (int j = 0; j < samples; j++) {
            float3 samplePoint = startWS + ((j + 0.5) / (float) samples) * dt * d;
            // For a box light, the attenuation is a function of distance from the plane.
            float sampleToPlaneDot = dot(samplePoint - position, axis);
            float attenuation = rcp(sampleToPlaneDot * sampleToPlaneDot + 1);
            integratedAttenuation += attenuation * localDt;
        }
        // Multiply attenuation through by light area.
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }


  static BoxLightVolume CreateBoxLightVolume(float3 position, float sizeX, float sizeY, float4x4 rotation,
    float4x4 inverseRotation, float range, float cullRadius) {
    BoxLightVolume v;
    v.position = position;
    v.sizeX = sizeX;
    v.sizeY = sizeY;
    v.rotation = rotation;
    v.inverseRotation = inverseRotation;
    v.axis = mul(rotation, float4(0, 0, 1, 0)).xyz;
    v.range = range;
    v.cullRadius = cullRadius;
    return v;
  }
};

/**
 * @brief: Spot light with pyramid shape.
 */
class PyramidLightVolume : ILightGeometry {
  float range, cullRadius, angle, aspect;
  float3 position, axis;
  float4x4 rotation, inverseRotation;

  float4 intersect(float3 p, float3 d) {
    float3 boundIntersection = Geometry::intersectSphere(p - position, d, range * cullRadius);
    if (!(boundIntersection.x > 0 || boundIntersection.y > 0) || boundIntersection.z < 0) {
        return -1;
    }
    float2 bound = float2(min(boundIntersection.x, boundIntersection.y), 
        max(boundIntersection.x, boundIntersection.y));

    float3 pTransformed = p - position;
    pTransformed = mul(inverseRotation, float4(pTransformed, 1)).xyz;
    float3 dTransformed = mul(inverseRotation, float4(d, 0)).xyz;

    // TODO: factor in aspect ratio
    // Build 4 frustum planes and intersect with each one.
    float angleAspect = atan(tan(angle) * aspect);
    float sinHalfAngle = sin(angle);
    float cosHalfAngle = cos(angle);
    float sinHalfAngleAspect = sin(angleAspect);
    float cosHalfAngleAspect = cos(angleAspect);
    // Flip aspect ratio if < 1
    if (aspect < 1) {
        float adjustedAspect = rcp(aspect);
        sinHalfAngleAspect = sinHalfAngle;
        cosHalfAngleAspect = cosHalfAngle;
        angleAspect = atan(tan(angle) * adjustedAspect);
        sinHalfAngle = sin(angleAspect);
        cosHalfAngle = cos(angleAspect);
    }
    // Right + Left planes
    float3 nRight = float3(cosHalfAngleAspect, 0, sinHalfAngleAspect);
    float3 nLeft = float3(-cosHalfAngleAspect, 0, sinHalfAngleAspect);
    // Top + Bottom planes
    float3 nTop = float3(0, -cosHalfAngle, sinHalfAngle);
    float3 nBottom = float3(0, cosHalfAngle, sinHalfAngle);

    // Intersections
    float2 intersectRight = Geometry::intersectPlane(pTransformed, dTransformed, dot(pTransformed, nRight) < 0 ? nRight : -nRight, 0);
    float2 intersectLeft = Geometry::intersectPlane(pTransformed, dTransformed, dot(pTransformed, nLeft) < 0 ? nLeft : -nLeft, 0);
    float2 intersectTop = Geometry::intersectPlane(pTransformed, dTransformed, dot(pTransformed, nTop) < 0 ? nTop : -nTop, 0);
    float2 intersectBottom = Geometry::intersectPlane(pTransformed, dTransformed, dot(pTransformed, nBottom) < 0 ? nBottom : -nBottom, 0);

    float tStart = FLT_MAX;
    float tEnd = -1;

    if (intersectRight.y >= 0 && intersectRight.x >= 0) {
        float3 intersectionPoint = pTransformed + intersectRight.x * dTransformed;
        float3 intersectionPointProjected = intersectionPoint - float3(1, 0, 0) * dot(intersectionPoint, float3(1, 0, 0));
        if (dot(normalize(intersectionPointProjected), float3(0, 0, 1)) > cosHalfAngle) {
            // This intersection is valid and can be considered.
            tStart = min(intersectRight.x, tStart);
            tEnd = max(intersectRight.x, tEnd);
        }
    }

    if (intersectLeft.y >= 0 && intersectLeft.x >= 0) {
        float3 intersectionPoint = pTransformed + intersectLeft.x * dTransformed;
        float3 intersectionPointProjected = intersectionPoint - float3(1, 0, 0) * dot(intersectionPoint, float3(1, 0, 0));
        if (dot(normalize(intersectionPointProjected), float3(0, 0, 1)) > cosHalfAngle) {
            // This intersection is valid and can be considered.
            tStart = min(intersectLeft.x, tStart);
            tEnd = max(intersectLeft.x, tEnd);
        }
    }

    if (intersectTop.y >= 0 && intersectTop.x >= 0) {
        float3 intersectionPoint = pTransformed + intersectTop.x * dTransformed;
        float3 intersectionPointProjected = intersectionPoint - float3(0, 1, 0) * dot(intersectionPoint, float3(0, 1, 0));
        if (dot(normalize(intersectionPointProjected), float3(0, 0, 1)) > cosHalfAngleAspect) {
            // This intersection is valid and can be considered.
            tStart = min(intersectTop.x, tStart);
            tEnd = max(intersectTop.x, tEnd);
        }
    }

    if (intersectBottom.y >= 0 && intersectBottom.x >= 0) {
        float3 intersectionPoint = pTransformed + intersectBottom.x * dTransformed;
        float3 intersectionPointProjected = intersectionPoint - float3(0, 1, 0) * dot(intersectionPoint, float3(0, 1, 0));
        if (dot(normalize(intersectionPointProjected), float3(0, 0, 1)) > cosHalfAngleAspect) {
            // This intersection is valid and can be considered.
            tStart = min(intersectBottom.x, tStart);
            tEnd = max(intersectBottom.x, tEnd);
        }
    }

    bool inside = dot(nRight, pTransformed) > 0 && dot(nLeft, pTransformed) > 0 && dot(nTop, pTransformed) > 0 && dot(nBottom, pTransformed) > 0;
    if (tStart == tEnd) {
        if (inside) {
            tEnd = tStart;
            tStart = 0;
        } else {
            tEnd = max(tStart, range);
            tStart = tStart;
        }
    } else if (inside) {
        tEnd = range;
        tStart = 0;
    }

    return float4(tStart == FLT_MAX ? -1 : tStart, tEnd, bound.x, bound.y);
  }

  void integrate(float3 p, float3 d, float maxDistance, out float4 intersection, out float integratedAttenuation) {
        // TODO: need to redo math here.
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        float tStart = intersection.x;
        intersection.y = min(intersection.y, maxDistance);
        float tEnd = intersection.y;
        float dt = tEnd - tStart;
        float3 startWS = p + d * tStart;
        // This is an analytical integration of the attenuation function 1/((d/r)^2 + 1).
        // However, since the light unit is specified in lumens, we can guarantee that r is 1.
        // Here, d specifies the distance from the plane, not from the position. This is an
        // approximation to actually integrating over the whole area light.
        float a = dot(startWS - position, axis) + 1;
        float c = dot(d, axis);
        float b = 2 * a * c;
        a *= a;
        c *= c;
        // Clamping above 0.0001 seems to give us reasonable numerical stability, but TODO: 
        // still have some slight issues.
        float rcpDisc = rcp(max(0.0001, Utilities::safeSqrt(4 * a * c - b * b)));
        integratedAttenuation = 2 * rcpDisc * (atan((b + 2 * c * dt) * rcpDisc) - atan(b * rcpDisc)); 
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }

  void raymarch(float3 p, float3 d, float maxDistance, int samples, out float4 intersection, out float integratedAttenuation) {
        intersection = intersect(p, d);
        integratedAttenuation = 0;
        if (intersection.x < 0 || intersection.x > maxDistance) {
            intersection = -1;
            integratedAttenuation = 0;
            return;
        }
        // Clamp end point to froxel cell.
        intersection.y = min(intersection.y, maxDistance);
        float tStart = intersection.x;
        float tEnd = intersection.y;
        float3 startWS = p + d * tStart;
        float dt = tEnd - tStart;
        float localDt = dt / samples;
        for (int j = 0; j < samples; j++) {
            float3 samplePoint = startWS + ((j + 0.5) / (float) samples) * dt * d;
            // For a box light, the attenuation is a function of distance from the plane.
            float sampleToPlaneDot = dot(samplePoint - position, axis);
            float attenuation = rcp(sampleToPlaneDot * sampleToPlaneDot + 1);
            integratedAttenuation += attenuation * localDt;
        }
        // Multiply attenuation through by light area.
        integratedAttenuation = max(0, rcp(0.99) * (integratedAttenuation - 0.01));
  }


  static PyramidLightVolume CreatePyramidLightVolume(float3 position, float angle, float aspect, float4x4 rotation,
    float4x4 inverseRotation, float range, float cullRadius) {
    PyramidLightVolume v;
    v.position = position;
    v.angle = angle / 2.0;
    v.aspect = aspect;
    v.rotation = rotation;
    v.inverseRotation = inverseRotation;
    v.axis = mul(rotation, float4(0, 0, 1, 0)).xyz;
    v.range = range;
    v.cullRadius = cullRadius;
    return v;
  }
};

class LightGeometry {

static ILightGeometry CreateLightGeometry(PointLightRenderSettings light, float cullRadius) {
    switch (light.geometryType) {
        case POINTLIGHTGEOMETRYTYPE_POINT: {
            return PointLightVolume::CreatePointLightVolume(light.position, light.range, cullRadius);
        }
        case POINTLIGHTGEOMETRYTYPE_CONE: {
            return ConeLightVolume::CreateConeLightVolume(light.position, light.geometryParam2, light.rotation, 
                light.inverseRotation, light.range, cullRadius);
        }
        case POINTLIGHTGEOMETRYTYPE_BOX: {
            return BoxLightVolume::CreateBoxLightVolume(light.position, light.geometryParam1, light.geometryParam2, light.rotation, 
                light.inverseRotation, light.range, cullRadius);
        }
        case POINTLIGHTGEOMETRYTYPE_PYRAMID: {
            return PyramidLightVolume::CreatePyramidLightVolume(light.position, light.geometryParam1, light.geometryParam2, light.rotation, 
                light.inverseRotation, light.range, cullRadius);
        }
        default: {
            // Undefined behavior, return a point light.
            return PointLightVolume::CreatePointLightVolume(light.position, light.range, cullRadius);
        }
    }
}

};

#endif // EXPANSE_LIGHT_GEOMETRY_INCLUDED