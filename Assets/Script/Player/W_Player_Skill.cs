using Script.Dragon;
using Script.Player.Effect;
using UnityEngine;

namespace Script.Player
{
    public class W_Player_Skill : State<PlayerController>
    {
        private readonly int m_WSkillHash;

        public W_Player_Skill() : base("Base Layer.Skill.Parrying.WSkill") =>
            m_WSkillHash = Animator.StringToHash("WSkill");

        public override void OnStateEnter()
        {
            Time.timeScale = 0.8f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            owner.useActionCam();
            EffectManager.Instance.EffectPlayerWeapon(true);
            DragonController.Instance.TakeDamage(owner.PlayerStat.damage,owner.currentWeaponFlag);
            machine.animator.SetTrigger(m_WSkillHash);
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Player_Movement),animToHash));
        }

        public override void OnStateExit()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            owner.useDefaultCam();
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}