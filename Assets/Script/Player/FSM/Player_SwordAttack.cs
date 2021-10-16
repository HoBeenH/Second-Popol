using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_SwordAttack : State<Player_Controller>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Attack.First Attack");
        private readonly int m_AttackHash = Animator.StringToHash("Attack");

        public override void OnStateEnter()
        {
            _EffectManager.EffectPlayerWeapon(true);
            machine.anim.SetTrigger(m_AttackHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_AttackLAnimHash)));
        }

        public override void OnStateChangePoint()
        {
            if (Input.GetMouseButtonDown(0))
            {
                machine.anim.SetTrigger(m_AttackHash);
            }
            
        }

        public override void OnStateExit()
        {
            machine.anim.ResetTrigger(m_AttackHash);
            _EffectManager.EffectPlayerWeapon(false);
        }
    }
}