using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Bite : State<Dragon_Controller>
    {
        private readonly int m_AttackAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");

        public override void OnStateEnter()
        {
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_AttackAnimHash)));
            machine.animator.SetTrigger(m_AttackTriggerHash);
        }
    }
}