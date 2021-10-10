using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Stun : State<DragonController>
    {
        public S_Dragon_Stun() : base("Base Layer.Stun") => m_StunHash = Animator.StringToHash("Stun");
        private readonly int m_StunHash;

        public override void OnStateEnter()
        {
            foreach (var pattern in machine.cancel)
            {
                owner.StopCoroutine(pattern);
            }
            
            machine.animator.SetTrigger(m_StunHash);
            owner.StartCoroutine(machine.WaitForState(animToHash));
        }
    }
}