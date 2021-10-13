using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Stun : State<Dragon_Controller>
    {
        public Dragon_Stun() : base("Base Layer.Stun") => m_StunHash = Animator.StringToHash("Stun");
        private readonly int m_StunHash;

        public override void OnStateEnter()
        {
            foreach (var pattern in machine.cancel)
            {
                owner.StopCoroutine(pattern);
            }
            
            machine.animator.SetTrigger(m_StunHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }
    }
}