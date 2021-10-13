using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Tail : State<Dragon_Controller>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Tail_Idle.Attack L");
        private readonly int m_TailHash = Animator.StringToHash("Tail");

        public override void OnStateEnter()
        {
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_AttackLAnimHash)));
            machine.animator.SetTrigger(m_TailHash);
        }
    }
}