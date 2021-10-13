using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_WeaponTopDown : State<Player_Controller>
    {
        private readonly int m_WTopDownHash;

        public Player_WeaponTopDown() : base("Base Layer.Skill.WTop Down") =>
            m_WTopDownHash = Animator.StringToHash("WTopDown");

        protected override void Init()
        {
            SkillManager.AddSkill(typeof(Player_WeaponTopDown),6f);
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