using UnityEngine;

namespace Script.Player
{
    public class S_Player_Sliding : State<PlayerController>
    {
        private Rigidbody m_PlayerRigidbody;
        private readonly int m_SlidingHash;
        
        public S_Player_Sliding() : base("Base Layer.Skill.Sliding") => m_SlidingHash = Animator.StringToHash("Sliding");

        protected override void Init()
        {
            m_PlayerRigidbody = owner.GetComponent<Rigidbody>();
        }

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_SlidingHash);
            m_PlayerRigidbody.velocity = owner.transform.forward * 7f;
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Player_Movement),animToHash));
        }
    }
}