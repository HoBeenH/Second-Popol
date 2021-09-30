using Script.Player.Effect;
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
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_WTopDownHash);
            owner.StartCoroutine(machine.WaitForAnim(typeof(S_Player_Movement), true, animToHash));
        }

        public override void OnStateExit()
        {
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}