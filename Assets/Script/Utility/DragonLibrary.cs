using System;
using UnityEngine;
using System.Collections;

namespace Script
{
    public static class DragonLibrary
    {
        public static Vector3 SetOffsetY(this Vector3 pos, float offSet)
        {
            var _pos = pos;
            _pos.y += offSet;
            return _pos;
        }

        public static IEnumerator CheckDis(this Transform owner, Vector3 endPos, float dis, Action action)
        {
            var _endDis = Mathf.Pow(dis, 2);
            while ((endPos - owner.position).sqrMagnitude >= _endDis)
            {
                action.Invoke();
                yield return null;
            }
        }

        public static IEnumerator CheckTime(float maxTime, Action action)
        {
            var _timer = 0f;
            while (_timer <= maxTime)
            {
                action.Invoke();
                _timer += Time.deltaTime;
                yield return null;
            }
        }

        public static void SinMove(this Transform owner, float speed, float length, ref float runningTime)
        {
            runningTime += Time.deltaTime * speed;
            var yPos = Mathf.Sin(runningTime) * length;
            owner.transform.position += new Vector3(0, yPos);
        }
    }
}