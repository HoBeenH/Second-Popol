using UnityEngine;

namespace Script.Player
{
    public class S_Player_FallDown : State<PlayerController>
    {
        private readonly int m_FallDownHash;
        private Rigidbody m_Rig;

        public S_Player_FallDown() : base("Base Layer.FallDown") => m_FallDownHash = Animator.StringToHash("FallDown");

        protected override void Init()
        {
            m_Rig = owner.GetComponent<Rigidbody>();
        }

        public override void OnStateEnter()
        {
            m_Rig.AddForce(owner.transform.forward * -5, ForceMode.Impulse);
            machine.animator.SetTrigger(m_FallDownHash);
        }

        public override void OnStateChangePoint()
        {
            if (machine.IsEnd())
            {
                machine.ChangeState<S_Player_Movement>();
            }
        }
    }
}