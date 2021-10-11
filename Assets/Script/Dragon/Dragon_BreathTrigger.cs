using System;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_BreathTrigger : MonoBehaviour
    {
        public ParticleSystem m_Breath1;
        public ParticleSystem m_Breath2;
        private bool bIsStart = true;

        public void SetEnable(bool isActive, int index)
        {
            if (isActive)
            {
                bIsStart = true;
                switch (index)
                {
                    case 1:
                        m_Breath1.gameObject.SetActive(true);
                        m_Breath1.Play();
                        break;
                    case 2:
                        m_Breath2.gameObject.SetActive(true);
                        m_Breath2.Play();
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 1:
                        m_Breath1.gameObject.SetActive(false);
                        break;
                    case 2:
                        m_Breath2.gameObject.SetActive(false);
                        break;
                }
            }
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