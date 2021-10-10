using System;
using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public class DragonSkill : SkillController
    {
        private void Awake()
        {
            base.Init();
            base.mask = 1 << 10;
            base.damage = () => _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                (_PlayerController.transform.position - transform.position).normalized);


            if (this.gameObject.name.Equals("Fire(Clone)"))
            {
                var choice = Random.Range(0, 2);
                BHasTriggerEffect = true;
                m_TriggerEffect = choice switch
                {
                    0 => EPrefabName.FireEx,
                    1 => EPrefabName.FireEx2,
                    _ => EPrefabName.FireEx
                };
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (_PlayerController.transform.position - transform.position).normalized);

                impulseHandler?.Invoke();
                if (BHasTriggerEffect)
                {
                    _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, new WaitForSeconds(5.0f));
                }

                col.enabled = false;
                StartCoroutine(base.HtiDelay());
            }
            else if (other.CompareTag("Ground"))
            {
                CheckOverlap();
                if (BHasTriggerEffect)
                {
                    _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, new WaitForSeconds(5.0f));
                }

                col.enabled = false;
                StartCoroutine(base.HtiDelay());
            }
        }
    }
}