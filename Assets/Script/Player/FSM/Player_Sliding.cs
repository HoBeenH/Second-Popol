using UnityEngine;

namespace Script.Player.FSM
{
    public class Player_Sliding : State<Player_Controller>
    {
        private readonly int m_SlidingHash;
        private Rigidbody m_Rigidbody;
        
        public Player_Sliding() : base("Base Layer.Skill.Sliding") => m_SlidingHash = Animator.StringToHash("Sliding");

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