using System;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SunTime {
    public int year = 2021;
    public int month = 4;
    public int day = 8;
    public int hour = 12;
    public int minute = 0;
    public int second = 0;
    public int millisecond = 0;

    public void SetFromDateTime(DateTime dateTime) {
        year = dateTime.Year;
        month = dateTime.Month;
        day = dateTime.Day;
        hour = dateTime.Hour;
        minute = dateTime.Minute;
        second = dateTime.Second;
        millisecond = dateTime.Millisecond;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SunTime))]
public class SunTimeDrawer : PropertyDrawer
{

// Utility function to make negative numbers mod correctly.
int mod(int x, int m) {
    int r = x % m;
    return (r < 0) ? r + m : r;
}

static int[] month_days = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};

override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    // Look up all the properties.
    SerializedProperty yearProp = property.FindPropertyRelative("year");
    SerializedProperty monthProp = property.FindPropertyRelative("month");
    SerializedProperty dayProp = property.FindPropertyRelative("day");
    SerializedProperty hourProp = property.FindPropertyRelative("hour");
    SerializedProperty minuteProp = property.FindPropertyRelative("minute");
    SerializedProperty secondProp = property.FindPropertyRelative("second");
    SerializedProperty millisecondProp = property.FindPropertyRelative("millisecond");
    
    // Draw them all.
    EditorGUILayout.LabelField("Time (Local)");
    yearProp.intValue = EditorGUILayout.IntField("Year", yearProp.intValue);
    monthProp.intValue = EditorGUILayout.IntField("Month", monthProp.intValue);
    dayProp.intValue = EditorGUILayout.IntField("Day", dayProp.intValue);
    hourProp.intValue = EditorGUILayout.IntField("Hour", hourProp.intValue);
    minuteProp.intValue = EditorGUILayout.IntField("Minute", minuteProp.intValue);
    secondProp.intValue = EditorGUILayout.IntField("Second", secondProp.intValue);
    millisecondProp.intValue = EditorGUILayout.IntField("Millisecond", millisecondProp.intValue);

    // Resolve carrying issues.

    // Milliseconds.
    int carrySeconds = (int) Mathf.Floor((float) millisecondProp.intValue / 1000.0f);
    millisecondProp.intValue = mod(millisecondProp.intValue, 1000);
    secondProp.intValue += carrySeconds;

    // Seconds.
    int carryMinutes = (int) Mathf.Floor((float) secondProp.intValue / 60.0f);
    secondProp.intValue = mod(secondProp.intValue, 60);
    minuteProp.intValue += carryMinutes;

    // Minutes.
    int carryHours = (int) Mathf.Floor((float) minuteProp.intValue / 60.0f);
    minuteProp.intValue = mod(minuteProp.intValue, 60);
    hourProp.intValue += carryHours;

    // Hours.
    int carryDays = (int) Mathf.Floor((float) hourProp.intValue / 24.0f);
    hourProp.intValue = mod(hourProp.intValue, 24);
    dayProp.intValue += carryDays;

    // Months. TODO: not working fully!!
    int prevMonth = monthProp.intValue;
    int carryMonths = (int) Mathf.Floor((float) (dayProp.intValue - 1) / (float) month_days[monthProp.intValue - 1]);
    monthProp.intValue += carryMonths;

    // Years.
    int carryYears = (int) Mathf.Floor((float) (monthProp.intValue - 1) / 12.0f);
    monthProp.intValue = 1 + mod(monthProp.intValue - 1, 12);
    yearProp.intValue += carryYears;

    // Wait until we finish computing month to look up final day count.
    if (dayProp.intValue > month_days[prevMonth - 1]) {
        dayProp.intValue = 1 + mod(dayProp.intValue - 1, month_days[prevMonth - 1]);
    } else {
        dayProp.intValue = 1 + mod(dayProp.intValue - 1, month_days[monthProp.intValue - 1]);
    }
}

}
#endif // UNITY_EDITOR
