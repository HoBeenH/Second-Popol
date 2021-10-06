using System;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class DragonBreath : MonoBehaviour
    {
        private ParticleSystem m_Breath;
        private bool bIsStart = true;

        private void Awake()
        {
            m_Breath = GetComponentInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            bIsStart = true;
            m_Breath.Play();
        }

        private void OnParticleTrigger()
        {
            if (bIsStart)
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.skillDamage,
                    (_PlayerController.transform.position - transform.position).normalized);
                bIsStart = false;
            }
        }
    }
}