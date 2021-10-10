using UnityEngine;

namespace Script.Player
{
    public class S_Player_Sliding : State<PlayerController>
    {
        private readonly int m_SlidingHash;
        private Rigidbody m_Rigidbody;
        
        public S_Player_Sliding() : base("Base Layer.Skill.Sliding") => m_SlidingHash = Animator.StringToHash("Sliding");

        protected override void Init()
        {
            m_Rigidbody = owner.GetComponent<Rigidbody>();
        }

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_SlidingHash);
            m_Rigidbody.velocity = owner.transform.forward * 7f;
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }
    }
}