#ifndef EXPANSE_COMMON_UTILITIES_INCLUDED
#define EXPANSE_COMMON_UTILITIES_INCLUDED

// Disable warnings for releases. In general these should be fixed
// properly, but they are annoying for users.
#pragma warning(disable: 3206) // implicit truncation of vector type
#pragma warning(disable: 4000) // use of potentially uninitialized variable

/**
 * @brief: static utility class for (frankly) utility functions that don't
 * have another logical place.
 * */
class Utilities {

  #define FLT_EPSILON 0.000001

  static float clampAboveZero(float a) {
    return max(1e-9, a);
  }
  static float2 clampAboveZero(float2 a) {
    return max(1e-9, a);
  }
  static float3 clampAboveZero(float3 a) {
    return max(1e-9, a);
  }
  static float clampNonZero(float a) {
    return (a == 0) ? 1e-9 : a;
  }

  static float clampCosine(float c) {
    return clamp(c, -1.0, 1.0);
  }

  /* True if a is greater than b within tolerance FLT_EPSILON, false
   * otherwise. */
  static bool floatGT(float a, float b) {
    return a > b - FLT_EPSILON;
  }
  static bool floatGT(float a, float b, float eps) {
    return a > b - eps;
  }

  /* True if a is less than b within tolerance FLT_EPSILON, false
   * otherwise. */
  static bool floatLT(float a, float b) {
    return a < b + FLT_EPSILON;
  }
  static bool floatLT(float a, float b, float eps) {
    return a < b + eps;
  }

  static float safeSqrt(float x) {
    return sqrt(max(0, x));
  }

  static float average(float3 x) {
    return dot(x, 1.0/3.0);
  }

  /* Returns minimum non-negative number. If both numbers are negative,
   * returns a negative number. */
  static float minNonNegative(float a, float b) {
    return (min(a, b) < 0.0) ? max(a, b) : min(a, b);
  }

  /* Returns whether x is within [bounds.x, bounds.y]. */
  static bool boundsCheck(float x, float2 bounds) {
    return floatGT(x, bounds.x) && floatLT(x, bounds.y);
  }
  static bool boundsCheckEpsilon(float x, float2 bounds, float eps) {
    return (x >= bounds.x - eps) && (x <= bounds.y + eps);
  }
  static bool boundsCheckNoEpsilon(float x, float2 bounds) {
    return (x >= bounds.x) && (x <= bounds.y);
  }

  static float erf(float x) {
    float sign_x = sign(x);
    x = abs(x);
    const float p = 0.3275911;
    const float a1 = 0.254829592;
    const float a2 = -0.284496736;
    const float a3 = 1.421413741;
    const float a4 = -1.453152027;
    const float a5 = 1.061405429;
    float t = 1 / (1 + p * x);
    float t2 = t * t;
    float t3 = t * t2;
    float t4 = t2 * t2;
    float t5 = t3 * t2;
    float prefactor = a5 * t5 + a4 * t4 + a3 * t3 + a2 * t2 + a1 * t;
    return sign_x * (1 - prefactor * exp(-(x * x)));
  }

  /* Generates linear location from a sample index.
   * Returns (sample, ds). */
  static float2 generateLinearSampleFromIndex(int i, int numberOfSamples) {
    float rcpNumSamples = rcp(numberOfSamples);
    return float2((i + 0.5) * rcpNumSamples, rcpNumSamples);
  }

  /* Generates cubed "importance sample" location from a sample index.
   * Returns (sample, ds). */
  static float2 generateCubicSampleFromIndex(int i, int numberOfSamples) {
    float rcpNumSamples = rcp(numberOfSamples);
    float t_left = i * rcpNumSamples;
    float t_middle = t_left + 0.5 * rcpNumSamples;
    float t_right = t_left + rcpNumSamples;
    t_left *= t_left * t_left;
    t_middle *= t_middle * t_middle;
    t_right *= t_right * t_right;
    return float2(t_middle, t_right - t_left);
  }

  /* Generates cubed "importance sample" location from a sample index.
   * Returns (sample, ds). */
  static float2 generateNthPowerSampleFromIndex(int i, int numberOfSamples, float n) {
    float rcpNumSamples = rcp(numberOfSamples);
    float t_left = i * rcpNumSamples;
    float t_middle = t_left + 0.5 * rcpNumSamples;
    float t_right = t_left + rcpNumSamples;
    t_left = pow(t_left, n);
    t_middle = pow(t_middle, n);
    t_right = pow(t_right, n);
    return float2(t_middle, t_right - t_left);
  }

  /**
   * @brief: Given an index and total number of points, generates corresponding
   * point on fibonacci hemi-sphere.
   * */
#ifndef EXPANSE_GOLDEN_RATIO
#define EXPANSE_GOLDEN_RATIO 1.6180339887498948482
#endif
#ifndef EXPANSE_GOLDEN_ANGLE
#define EXPANSE_GOLDEN_ANGLE 2 * PI / EXPANSE_GOLDEN_RATIO
#endif
  static float3 fibonacciHemisphere(int i, int n) {
    float i_mid = i + 0.5;
    float cos_phi = 1 - i/float(n);
    float sin_phi = safeSqrt(1 - cos_phi * cos_phi);
    float theta = EXPANSE_GOLDEN_ANGLE * i;
    float cos_theta = cos(theta);
    float sin_theta = safeSqrt(1 - cos_theta * cos_theta);
    return float3(cos_theta * sin_phi, cos_phi, sin_theta * sin_phi);
  }

  /**
   * @brief: Given an index and total number of points, generates corresponding
   * point on fibonacci sphere.
   * */
  static float3 fibonacciSphere(int i, int n) {
    float i_mid = i + 0.5;
    float cos_phi = 1 - 2 * i/float(n);
    float sin_phi = safeSqrt(1 - cos_phi * cos_phi);
    float theta = EXPANSE_GOLDEN_ANGLE * i;
    float cos_theta = cos(theta);
    float sin_theta = safeSqrt(1 - cos_theta * cos_theta);
    return float3(cos_theta * sin_phi, cos_phi, sin_theta * sin_phi);
  }

  /**
   * @brief: converts a temperature in kelvin of a blackbody to the color
   * of light it emits. This approximation is reasonably accurate for
   * the temperature range of (1000K, 20000K).
   * */
  static float3 blackbodyTempToColor(float t) {
    t = t / 100;
    float3 result = float3(0, 0, 0);

    /* Red. */
    if (t <= 66) {
      result.r = 255;
    } else {
      result.r = t - 60;
      result.r = 329.698727446f * (pow(abs(result.r), -0.1332047592f));
    }

    /* Green. */
    if (t <= 66) {
      result.g = t;
      result.g = 99.4708025861f * log(t) - 161.1195681661f;
    } else {
      result.g = 288.1221695283f * (pow(abs(t-60), -0.0755148492f));
    }

    /* Blue. */
    if (t >= 66) {
      result.b = 255;
    } else {
      if (t <= 19) {
        result.b = 0;
      } else {
        result.b = 138.5177312231f * log(t-result.b) - 305.0447927307f;
      }
    }

    return clamp(result, 0, 255) / 255;
  }

};

#endif // EXPANSE_COMMON_UTILITIES_INCLUDED
