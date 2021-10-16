using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Bite : State<Dragon_Controller>
    {
        private readonly int m_AttackHash;

        public Dragon_Bite() : base("Base Layer.Attack_Idle.Attack 1") =>
            m_AttackHash = Animator.StringToHash("Attack");

        public override void OnStateEnter()
        {
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
            machine.anim.SetTrigger(m_AttackHash);
        }
    }
}