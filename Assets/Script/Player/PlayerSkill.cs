using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class PlayerSkill : SkillController
    {
        private void Awake()
        {
            base.Init();
            base.mask = 1 << 11;
            base.damage = () =>
                _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
                impulseHandler?.Invoke();
                if (BHasTriggerEffect)
                {
                    _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, new WaitForSeconds(15.0f));
                }

                col.enabled = false;
                StartCoroutine(base.HtiDelay());
            }
            else if (other.CompareTag("Ground"))
            {
                CheckOverlap();
                if (BHasTriggerEffect)
                {
                    _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, new WaitForSeconds(15.0f));
                }

                col.enabled = false;
                StartCoroutine(base.HtiDelay());
            }
        }
    }
}