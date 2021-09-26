#ifndef EXPANSE_COMMON_MAPPING_INCLUDED
#define EXPANSE_COMMON_MAPPING_INCLUDED

/**
 * @brief: static utility class for handling texture coordinate mapping
 * operations that are common across the whole codebase.
 * */
class Mapping {

  /** 
   * Transforms a point from worldspace to planet space, which has the 
   * same orientation as worldspace, but places the planet at the origin.
   * */
  static float3 transformPointToPlanetSpace(float3 p, float3 planetOriginOffset, float planetRadius) {
    float3 transformed = p - planetOriginOffset + float3(0, planetRadius, 0);
    // Clamp to surface of planet.
    return normalize(transformed) * max(length(transformed), planetRadius + 1);
  }

  static float3 getSkyViewDirWS(float2 positionCS, float4x4 pixelCoordToViewDirMatrix, float2 taaJitterStrength) {
    return normalize(mul(float4(positionCS + taaJitterStrength, 1.0f, 1.0f), pixelCoordToViewDirMatrix).xyz);
  }

  /**
   * UV => direction and vice-versa.
   * */
  static float3 unmapPolar(float2 uv) {
    float theta = 2 * PI * uv.x;
    float phi = PI * uv.y;
    return float3(
      sin(phi) * cos(theta),
      cos(phi),
      sin(phi) * sin(theta)
    );
  }
  static float2 mapPolar(float3 d) {
    float theta = atan2(d.z, d.x);
    float phi = atan2(sqrt(d.x*d.x + d.z*d.z), d.y);
    if (phi < 0) {
      theta += PI;
      phi = -phi;
    }
    theta = (theta < 0) ? (theta + (2 * PI)) : theta;
    return float2(theta / (2 * PI), phi / PI);
  }

  /**
   * @brief: converts a direction vector to a texture array coordinate.
   * Used for faking a cubemap with a 2D texture array.
   * */
  static float3 directionToTex2DArrayCubemapUV(float3 xyz) {
    // Find which dimension we're pointing at the most
    float3 absxyz = abs(xyz);
    int xMoreY = absxyz.x > absxyz.y;
    int yMoreZ = absxyz.y > absxyz.z;
    int zMoreX = absxyz.z > absxyz.x;
    int xMost = (xMoreY) && (!zMoreX);
    int yMost = (!xMoreY) && (yMoreZ);
    int zMost = (zMoreX) && (!yMoreZ);

    // Determine which index belongs to each +- dimension
    // 0: +X; 1: -X; 2: +Y; 3: -Y; 4: +Z; 5: -Z;
    float xSideIdx = 0 + (xyz.x < 0);
    float ySideIdx = 2 + (xyz.y < 0);
    float zSideIdx = 4 + (xyz.z < 0);

    // Composite it all together to get our side
    float side = xMost * xSideIdx + yMost * ySideIdx + zMost * zSideIdx;

    // Depending on side, we use different components for UV and project to square
    float3 useComponents = float3(0, 0, 0);
    if (xMost) useComponents = xyz.yzx;
    if (yMost) useComponents = xyz.xzy;
    if (zMost) useComponents = xyz.xyz;

    float2 uv = useComponents.xy / useComponents.z;

    // Transform uv from [-1,1] to [0,1]
    uv = uv * 0.5 + float2(0.5, 0.5);

    return float3(uv, side);
  }

  /**
   * @brief: converts a texture array coordinate to a direction vector.
   * Used for faking a cubemap with a 2D texture array.
   * */
  static float3 tex2DArrayCubemapUVToDirection(float3 uvw) {
    // Use side to decompose primary dimension and negativity
    int side = uvw.z;
    int xMost = side < 2;
    int yMost = side >= 2 && side < 4;
    int zMost = side >= 4;
    int wasNegative = side & 1;

    // Insert a constant plane value for the dominant dimension in here
    uvw.z = 1;

    // Depending on the side we swizzle components back (NOTE: uvw.z is 1)
    float3 useComponents = float3(0, 0, 0);
    if (xMost) useComponents = uvw.zxy;
    if (yMost) useComponents = uvw.xzy;
    if (zMost) useComponents = uvw.xyz;

    // Transform components from [0,1] to [-1,1]
    useComponents = useComponents * 2 - float3(1, 1, 1);
    useComponents *= 1 - 2 * wasNegative;

    return normalize(useComponents);
  }
};

#endif // EXPANSE_COMMON_MAPPING_INCLUDED
