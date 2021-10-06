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
            owner.playerCurrentFlag |= EPlayerFlag.Parry;
            machine.animator.SetTrigger(m_ParryingHash);
            owner.StartCoroutine(machine.WaitForIdle(animToHash));
        }

        public override void OnStateExit()
        {
            owner.playerCurrentFlag &= ~EPlayerFlag.Parry;
        }
    }
}