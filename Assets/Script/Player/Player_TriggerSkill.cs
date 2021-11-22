using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_TriggerSkill : TriggerSkill
    {
        private void Awake() => base.Init();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                _DragonController.TakeDamage(_PlayerController.Stat.damage);
                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                HitTrigger();
            }
        }
    }
}