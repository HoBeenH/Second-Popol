using Script.Dragon;
using Script.Player.Effect;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class W_Player_Skill : State<PlayerController>
    {
        private readonly int m_WSkillHash;

        public W_Player_Skill() : base("Base Layer.Skill.Parrying.WSkill") =>
            m_WSkillHash = Animator.StringToHash("WSkill");

        public override void OnStateEnter()
        {
            _CamManager.PlayerWeapon.GenerateImpulse();
            Time.timeScale = 0.8f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _EffectManager.EffectPlayerWeapon(true);
            _DragonController.TakeDamage(owner.PlayerStat.damage,owner.playerCurrentFlag);
            machine.animator.SetTrigger(m_WSkillHash);
            owner.StartCoroutine(machine.WaitForIdle(animToHash));
        }

        public override void OnStateExit()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _EffectManager.EffectPlayerWeapon(false);
        }
    }
}