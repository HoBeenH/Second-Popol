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
            SetEffect();
            owner.StartCoroutine(machine.WaitForIdle(animToHash));
        }

        private void SetEffect()
        {
            _EffectManager.GetEffectOrNull(EPrefabName.Shoot, _EffectManager.spawnPosFw.position,
                Quaternion.LookRotation(owner.transform.forward),
                m_EffectDestroyTime, m_EffectDelayTime);

            _EffectManager.GetEffectOrNull(EPrefabName.ShootHand, _EffectManager.leftHand.position, null,
                new WaitForSeconds(6.0f), null, _EffectManager.leftHand);
            _EffectManager.GetEffectOrNull(EPrefabName.ShootHand, _EffectManager.rightHand.position, null,
                new WaitForSeconds(6.0f), null, _EffectManager.rightHand);
        }
    }
}