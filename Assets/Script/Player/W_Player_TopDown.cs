using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class W_Player_TopDown : State<PlayerController>
    {
        private readonly int m_WTopDownHash;

        public W_Player_TopDown() : base("Base Layer.Skill.WTop Down") =>
            m_WTopDownHash = Animator.StringToHash("WTopDown");

        protected override void Init()
        {
            _SkillManager.AddSkill(typeof(W_Player_TopDown),6f);
        }

        public override void OnStateEnter()
        {
            _EffectManager.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_WTopDownHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        public override void OnStateExit()
        {
            _EffectManager.EffectPlayerWeapon(false);
        }
    }
}