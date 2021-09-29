using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Stun : State<DragonController>
    {
        public S_Dragon_Stun() : base("Base Layer.Stun") => m_StunHash = Animator.StringToHash("Stun");
        private readonly int m_StunHash;

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_StunHash);
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement),animToHash));
        }
    }
}