using System;
using Cinemachine;
using UnityEngine;

namespace Script
{
    public class OverlapSkill : MonoBehaviour
    {
        [SerializeField] protected float radius;
        [SerializeField] protected bool hasImpulse;
        [SerializeField] protected CinemachineImpulseSource source;
        [SerializeField] protected float delayTime;
        protected readonly Collider[] result = new Collider[1];
        protected WaitForSeconds time;
        protected LayerMask layer;

        protected void Init(LayerMask mask)
        {
            if (hasImpulse)
            {
                source = GetComponent<CinemachineImpulseSource>();
            }

            time = delayTime == 0 ? new WaitForSeconds(0.1f) : new WaitForSeconds(delayTime);
            layer = mask;
        }

        private void OnDisable() => Array.Clear(result, 0, 1);
    }
}