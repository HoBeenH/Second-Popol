using Cinemachine;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class W_Player_Skill : State<PlayerController>
    {
        private readonly int m_WSkillHash;
        private CinemachineImpulseSource m_Source;

        public W_Player_Skill() : base("Base Layer.Skill.Parrying.WSkill") =>
            m_WSkillHash = Animator.StringToHash("WSkill");

        protected override void Init()
        {
            m_Source = Camera.main.gameObject.GetComponent<CinemachineImpulseSource>();
        }

        public override void OnStateEnter()
        {
            m_Source.GenerateImpulse();
            _EffectManager.EffectPlayerWeapon(true);
            _DragonController.TakeDamage(owner.PlayerStat.damage,owner.playerFlag);
            machine.animator.SetTrigger(m_WSkillHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        public override void OnStateExit()
        {
            _EffectManager.EffectPlayerWeapon(false);
        }
    }
}