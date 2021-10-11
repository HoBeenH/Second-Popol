using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class PlayerSkill : SkillController
    {
        private readonly WaitForSeconds m_Return = new WaitForSeconds(15.0f);

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
                if (BHasImpulse)
                {
                    source.GenerateImpulse();
                }

                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                CheckOverlap();
                HitTrigger();
            }
        }
    }
}