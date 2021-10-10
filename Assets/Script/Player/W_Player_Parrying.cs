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
            owner.playerFlag |= EPlayerFlag.Parry;
            machine.animator.SetTrigger(m_ParryingHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        public override void OnStateExit()
        {
            owner.playerFlag &= ~EPlayerFlag.Parry;
        }
    }
}