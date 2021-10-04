using Script.Player.Effect;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class M_Player_Shoot : State<PlayerController>
    {
        private readonly WaitForSeconds m_EffectDestroyTime = new WaitForSeconds(5.0f);
        private readonly WaitForSeconds m_EffectDelayTime = new WaitForSeconds(0.6f);
        private readonly int m_ShootHash;

        public M_Player_Shoot() : base("Base Layer.Skill.Shoot") => m_ShootHash = Animator.StringToHash("Shoot");

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_ShootHash);
            _EffectManager.GetEffectOrNull(EPrefabName.IceShoot, _EffectManager.spawnPosFw.position,
                Quaternion.LookRotation(owner.transform.forward),
            m_EffectDestroyTime, m_EffectDelayTime);
            owner.StartCoroutine(machine.WaitForIdle(animToHash));
        }
    }
}