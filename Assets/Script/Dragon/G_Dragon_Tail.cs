using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Tail : State<Dragon_Controller>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Tail_Idle.Attack L");
        private readonly int m_TailHash = Animator.StringToHash("Tail");

        public override void OnStateEnter()
        {
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_AttackLAnimHash)));
            machine.animator.SetTrigger(m_TailHash);
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
        }
    }
}