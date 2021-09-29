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
            owner.useActionCam();
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_WTopDownHash);
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Player_Movement),animToHash));
        }

        public override void OnStateExit()
        {
            owner.useDefaultCam();
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}