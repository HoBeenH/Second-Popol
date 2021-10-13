using Cinemachine;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_Counter : State<Player_Controller>
    {
        private CinemachineImpulseSource m_Source;
        private readonly int m_WSkillHash;

        public Player_Counter() : base("Base Layer.Skill.Parrying.WSkill") =>
            m_WSkillHash = Animator.StringToHash("WSkill");

        protected override void Init()
        {
            m_Source = Camera.main.gameObject.GetComponent<CinemachineImpulseSource>();
        }

        public override void OnStateEnter()
        {
            m_Source.GenerateImpulse();
            _EffectManager.EffectPlayerWeapon(true);
            _DragonController.TakeDamage(owner.PlayerStat.damage);
            machine.animator.SetTrigger(m_WSkillHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        public override void OnStateExit()
        {
            _EffectManager.EffectPlayerWeapon(false);
        }
    }
}