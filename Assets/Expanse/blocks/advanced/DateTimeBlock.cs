using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace Expanse {

[ExecuteInEditMode, Serializable]
public class DateTimeBlock : MonoBehaviour
{
    [Tooltip("Celestial Body that will have the position of the sun.")]
    public CelestialBodyBlock m_sun;
    [Tooltip("Celestial Body that will have the position of the moon.")]
    public CelestialBodyBlock m_moon;
    [Tooltip("Night sky block that this controls.")]
    public NightSkyBlock m_nightSky;
    [Tooltip("Rotation speed of night sky along each axis.")]
    public Vector3 m_nightSkyRotationSpeed = new Vector3(0.5f, 0.25f, 0.7f);
    [Range(-90, 90), Tooltip("Latitude coordinate of the player.")]
    public float m_latitude = 37.77f;
    [Range(-180, 180), Tooltip("Longitude coordinate of the player.")]
    public float m_longitude = 122.42f;
    [Tooltip("Local time---so UTC time, with the UTC offset applied.")]
    public SunTime m_timeLocal;
    public float m_skyOffset = 0;
    [Range(-13, 13), Tooltip("Offset to UTC---useful for being able to specify time in local time where your latitude/longitude coordinates are. Specify as the number of hours ahead your location is. In Munich, 12:00 local time is 10:00 UTC, so you would set this parameter to 2.")]
    public int m_timeUTCOffset = 7;
    // Update is called once per frame
    void Update()
    {
        DateTime dateTime = new DateTime(
            m_timeLocal.year,
            m_timeLocal.month,
            m_timeLocal.day,
            m_timeLocal.hour,
            m_timeLocal.minute,
            m_timeLocal.second,
            m_timeLocal.millisecond
        );

        // Apply UTC offset to datetime, to make sure that the year/month/day wrap correctly.
        dateTime -= new TimeSpan(0, m_timeUTCOffset, 0, 0);
        float internalHour = dateTime.Hour + (dateTime.Minute * 0.0166667f) + (dateTime.Second * 0.000277778f);

        double totalDaysUTC = daysSinceEpoch(dateTime);
        float d = 367 * dateTime.Year - 7 * (dateTime.Year + (dateTime.Month / 12 + 9) / 12) / 4 + 275 * dateTime.Month / 9 + dateTime.Day - 730530;
        d += internalHour / 24f;
        float ecl = 23.4393f - 3.563E-7f * d;

        // Update sun position.
        double sunAzimuthAngle, sunZenithAngle;
        CalculateSunPositionEnv(internalHour, d, ecl, out sunAzimuthAngle, out sunZenithAngle);
        if (m_sun != null) {
            m_sun.m_direction = new Vector3((float) sunZenithAngle, (float) sunAzimuthAngle + m_skyOffset, 0);
        }
        

        // Update moon position.
        double moonAzimuthAngle, moonZenithAngle;
        CalculateMoonPositionEnv(d, ecl, out moonAzimuthAngle, out moonZenithAngle);
        if (m_moon != null) {
            m_moon.m_direction = new Vector3((float) moonZenithAngle, (float) moonAzimuthAngle + m_skyOffset, 0);
        }

        // Update night sky position, with what's essentially a huge hack.
        if (m_nightSky != null) {
            double rotateX = (totalDaysUTC * (double) m_nightSkyRotationSpeed.x * 360.0) % 360.0;
            double rotateY = (totalDaysUTC * (double) m_nightSkyRotationSpeed.y * 360.0) % 360.0;
            double rotateZ = (totalDaysUTC * (double) m_nightSkyRotationSpeed.z * 360.0) % 360.0;
            m_nightSky.m_rotation = new Vector3((float) rotateX, (float) rotateY - m_skyOffset, (float) rotateZ);
        }
    }

    // Sets time value using the offset.
    public void SetDateTimeLocal(DateTime dateTime) {
        m_timeLocal.SetFromDateTime(dateTime);
    }

    // Sets time value using date time object using UTC.
    public void SetDateTimeUTC(DateTime dateTime) {
        // Make sure we apply the opposite of the offset.
        dateTime += new TimeSpan(0, m_timeUTCOffset, 0, 0);
        m_timeLocal.SetFromDateTime(dateTime);
    }

    private static double daysSinceEpoch(DateTime dateTime) {
        TimeSpan span = dateTime.Subtract(new DateTime(1970,1,1,0,0,0));
        return span.TotalDays;
    }
      
    /*
     * Credit for this implementation goes to Enviro Sky and Fog---adapted by
     * morpheus101.
     */
    private const double Deg2Rad = Math.PI / 180.0;
    private const double Rad2Deg = 180.0 / Math.PI;
    private float LST; // State computed by CalculateSunPositionEnv() that controls moon position.

    public void CalculateSunPositionEnv(float internalHour, float d, float ecl, out double outAzimuth, out double outAltitude)
    {
        /////http://www.stjarnhimlen.se/comp/ppcomp.html#5////
        ///////////////////////// SUN ////////////////////////
        float w = 282.9404f + 4.70935E-5f * d;
        float e = 0.016709f - 1.151E-9f * d;
        float M = 356.0470f + 0.9856002585f * d;

        float E = M + e * Mathf.Rad2Deg * Mathf.Sin(Mathf.Deg2Rad * M) * (1 + e * Mathf.Cos(Mathf.Deg2Rad * M));

        float xv = Mathf.Cos(Mathf.Deg2Rad * E) - e;
        float yv = Mathf.Sin(Mathf.Deg2Rad * E) * Mathf.Sqrt(1 - e * e);

        float v = Mathf.Rad2Deg * Mathf.Atan2(yv, xv);
        float r = Mathf.Sqrt(xv * xv + yv * yv);

        float l = v + w;

        float xs = r * Mathf.Cos(Mathf.Deg2Rad * l);
        float ys = r * Mathf.Sin(Mathf.Deg2Rad * l);

        float xe = xs;
        float ye = ys * Mathf.Cos(Mathf.Deg2Rad * ecl);
        float ze = ys * Mathf.Sin(Mathf.Deg2Rad * ecl);

        float decl_rad = Mathf.Atan2(ze, Mathf.Sqrt(xe * xe + ye * ye));
        float decl_sin = Mathf.Sin(decl_rad);
        float decl_cos = Mathf.Cos(decl_rad);

        float GMST0 = (l + 180);
        float GMST = GMST0 + internalHour * 15;
        LST = GMST + m_longitude;

        float HA_deg = LST - Mathf.Rad2Deg * Mathf.Atan2(ye, xe);
        float HA_rad = Mathf.Deg2Rad * HA_deg;
        float HA_sin = Mathf.Sin(HA_rad);
        float HA_cos = Mathf.Cos(HA_rad);

        float x = HA_cos * decl_cos;
        float y = HA_sin * decl_cos;
        float z = decl_sin;

        float sin_Lat = Mathf.Sin(Mathf.Deg2Rad * m_latitude);
        float cos_Lat = Mathf.Cos(Mathf.Deg2Rad * m_latitude);

        float xhor = x * sin_Lat - z * cos_Lat;
        float yhor = y;
        float zhor = x * cos_Lat + z * sin_Lat;

        float azimuth = Mathf.Atan2(yhor, xhor) + Mathf.Deg2Rad * 180;
        float altitude = Mathf.Atan2(zhor, Mathf.Sqrt(xhor * xhor + yhor * yhor));

        float sunTheta = (90 * Mathf.Deg2Rad) - altitude;
        float sunPhi = azimuth;

        outAltitude = altitude * Rad2Deg;
        outAzimuth = azimuth * Rad2Deg;
    }

    public void CalculateMoonPositionEnv(float d, float ecl, out double outAzimuth, out double outAltitude)
    {

        float N = 125.1228f - 0.0529538083f * d;
        float i = 5.1454f;
        float w = 318.0634f + 0.1643573223f * d;
        float a = 60.2666f;
        float e = 0.054900f;
        float M = 115.3654f + 13.0649929509f * d;

        float rad_M = Mathf.Deg2Rad * M;

        float E = rad_M + e * Mathf.Sin(rad_M) * (1f + e * Mathf.Cos(rad_M));

        float xv = a * (Mathf.Cos(E) - e);
        float yv = a * (Mathf.Sqrt(1f - e * e) * Mathf.Sin(E));

        float v = Mathf.Rad2Deg * Mathf.Atan2(yv, xv);
        float r = Mathf.Sqrt(xv * xv + yv * yv);

        float rad_N = Mathf.Deg2Rad * N;
        float sin_N = Mathf.Sin(rad_N);
        float cos_N = Mathf.Cos(rad_N);

        float l = Mathf.Deg2Rad * (v + w);
        float sin_l = Mathf.Sin(l);
        float cos_l = Mathf.Cos(l);

        float rad_i = Mathf.Deg2Rad * i;
        float cos_i = Mathf.Cos(rad_i);

        float xh = r * (cos_N * cos_l - sin_N * sin_l * cos_i);
        float yh = r * (sin_N * cos_l + cos_N * sin_l * cos_i);
        float zh = r * (sin_l * Mathf.Sin(rad_i));

        float cos_ecl = Mathf.Cos(Mathf.Deg2Rad * ecl);
        float sin_ecl = Mathf.Sin(Mathf.Deg2Rad * ecl);

        float xe = xh;
        float ye = yh * cos_ecl - zh * sin_ecl;
        float ze = yh * sin_ecl + zh * cos_ecl;

        float ra = Mathf.Atan2(ye, xe);
        float decl = Mathf.Atan2(ze, Mathf.Sqrt(xe * xe + ye * ye));

        float HA = Mathf.Deg2Rad * LST - ra;

        float x = Mathf.Cos(HA) * Mathf.Cos(decl);
        float y = Mathf.Sin(HA) * Mathf.Cos(decl);
        float z = Mathf.Sin(decl);

        float latitude = Mathf.Deg2Rad * m_latitude;
        float sin_latitude = Mathf.Sin(latitude);
        float cos_latitude = Mathf.Cos(latitude);

        float xhor = x * sin_latitude - z * cos_latitude;
        float yhor = y;
        float zhor = x * cos_latitude + z * sin_latitude;

        float azimuth = Mathf.Atan2(yhor, xhor) + Mathf.Deg2Rad * 180f;
        float altitude = Mathf.Atan2(zhor, Mathf.Sqrt(xhor * xhor + yhor * yhor));

        float MoonTheta = (90f * Mathf.Deg2Rad) - altitude;
        float MoonPhi = azimuth;

        outAzimuth = azimuth * Rad2Deg;
        outAltitude = altitude * Rad2Deg;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(DateTimeBlock))]
public class DateTimeBlockEditor : Editor
{
override public void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sun"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_moon"));
    SerializedProperty nightSky = serializedObject.FindProperty("m_nightSky");
    EditorGUILayout.PropertyField(nightSky);
    if (nightSky.objectReferenceValue != null) { 
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_nightSkyRotationSpeed"));
    }
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_latitude"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_longitude"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_skyOffset"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_timeUTCOffset"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_timeLocal"));
    serializedObject.ApplyModifiedProperties();
}
}

#endif // UNITY_EDITOR

} // namespace Expanse