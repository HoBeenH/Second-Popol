using UnityEngine;

namespace Script.Player
{
    public class W_Player_Parrying : State<PlayerController>
    {
        private readonly int m_ParryingHash;

        public W_Player_Parrying() : base("Base Layer.Skill.Parrying.Parrying") =>
            m_ParryingHash = Animator.StringToHash("Parrying");

        public override void OnStateEnter()
        {
            owner.currentWeaponFlag |= ECurrentWeaponFlag.Parry;
            Debug.Log(owner.currentWeaponFlag.ToString());
            owner.useActionCam();
            machine.animator.SetTrigger(m_ParryingHash);
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
            owner.currentWeaponFlag &= ~ECurrentWeaponFlag.Parry;
            Debug.Log($"Exit {owner.currentWeaponFlag.ToString()}");
            owner.useDefaultCam();
        }
    }
}