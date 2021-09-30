using System.Collections;
using Script.Player.Effect;
using UnityEngine;

namespace Script.Player
{
    public class W_Player_Attack : State<PlayerController>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Attack.First Attack");
        private readonly int m_Attack = Animator.StringToHash("Attack");

        public override void OnStateEnter()
        {
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_Attack);
            owner.StartCoroutine(machine.WaitForAnim(typeof(S_Player_Movement),true,m_AttackLAnimHash));
        }

        public override void OnStateChangePoint()
        {
            if (Input.GetMouseButtonDown(0))
            {
                machine.animator.SetTrigger(m_Attack);
            }
        }

        public override void OnStateExit()
        {
            machine.animator.ResetTrigger(m_Attack);
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}