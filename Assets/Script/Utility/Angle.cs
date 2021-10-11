using UnityEngine;

namespace Script
{
    public static class Angle
    {
        public static Vector2 RotateVector(this Vector3 v, float angle)
        {
            var _radian = angle * Mathf.Deg2Rad;
            var _x = v.x * Mathf.Cos(_radian) - v.y * Mathf.Sin(_radian);
            var _y = v.x * Mathf.Sin(_radian) + v.y * Mathf.Cos(_radian);
            return new Vector2(_x, _y);
        }
    }
}
