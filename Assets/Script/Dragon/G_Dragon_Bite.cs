using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Bite : State<Dragon_Controller>
    {
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");
        private readonly int m_AttackAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");

        public override void OnStateEnter()
        {
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_AttackAnimHash)));
            machine.animator.SetTrigger(m_AttackTriggerHash);

        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
        }
    }
}