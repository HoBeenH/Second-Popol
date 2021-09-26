using UnityEngine;

namespace Script.Player
{
    public class W_Player_Skill : State<PlayerController>
    {
        private readonly int m_WSkillHash;
        public W_Player_Skill() : base("Base Layer.Skill.WSkill") => m_WSkillHash = Animator.StringToHash("WSkill");
        
        public override void OnStateEnter()
        {
            owner.useActionCam();
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_WSkillHash);
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