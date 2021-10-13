using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_Skill : Skill
    {
        private void Awake()
        {
            base.Init();
            base.layer = 1 << 11;
            base.action = () =>
                _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage);
                if (HasImpulseSource)
                {
                    source.GenerateImpulse();
                }

                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                StartCoroutine(CheckOverlap());
                HitTrigger();
            }
        }
    }
}