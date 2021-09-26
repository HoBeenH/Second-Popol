using UnityEngine;

namespace Script.Player
{
    public class W_Player_TopDown : State<PlayerController>
    {
        private readonly int m_WTopDownHash;


        public W_Player_TopDown() : base("Base Layer.Skill.WTop Down") =>
            m_WTopDownHash = Animator.StringToHash("WTopDown");

        public override void OnStateEnter()
        {
            owner.useActionCam();
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_WTopDownHash);
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
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}