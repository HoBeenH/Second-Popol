using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Stun : State<Dragon_Controller>
    {
        private readonly int m_StunHash;
        
        public Dragon_Stun() : base("Base Layer.Stun") => m_StunHash = Animator.StringToHash("Stun");

        public override void OnStateEnter()
        {
            foreach (var pattern in machine.cancel)
            {
                owner.StopCoroutine(pattern);
            }
            
            machine.anim.SetTrigger(m_StunHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }
    }
}