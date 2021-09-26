using UnityEngine;

namespace Script.Player
{
    public class M_Player_TopDown : State<PlayerController>
    {
        private readonly int m_TopDownHash;
        public M_Player_TopDown() : base("Base Layer.Skill.Top Down") => m_TopDownHash = Animator.StringToHash("Top Down");

        public override void OnStateEnter()
        {
            owner.useActionCam();
            machine.animator.SetTrigger(m_TopDownHash);
        }

        public override void OnStateChangePoint()
        {
            if (machine.IsEnd())
            {
                machine.ChangeState<S_Player_Movement>();
            }
        }

        public override void OnStateExit()
        {
            owner.useDefaultCam();
        }
    }
}