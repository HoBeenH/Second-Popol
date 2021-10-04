using System.Collections;
using Script.Player.Effect;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class M_Player_TopDown : State<PlayerController>
    {
        private readonly int m_TopDownHash;
        private readonly WaitForSeconds m_TopDownCool = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_EffectTopDownHandTimer = new WaitForSeconds(6.0f);
        private readonly WaitForSeconds m_EffectTopDownTimer = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_EffectTopDownSpawnDelay = new WaitForSeconds(0.8f);

        public M_Player_TopDown() : base("Base Layer.Skill.Top Down") =>
            m_TopDownHash = Animator.StringToHash("Top Down");

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_TopDownHash);
            owner.bTopDownCoolTime = false;
            owner.StartCoroutine(CoolTime());
            SetEffect();
            owner.StartCoroutine(machine.WaitForIdle(animToHash));
        }

        private IEnumerator CoolTime()
        {
            yield return m_TopDownCool;
            owner.bTopDownCoolTime = true;
        }

        private void SetEffect()
        {
            _EffectManager.GetEffectOrNull(EPrefabName.TopDownHand, _EffectManager.leftHand.position, null,
                m_EffectTopDownHandTimer, null, _EffectManager.leftHand);

            _EffectManager.GetEffectOrNull(EPrefabName.TopDownHand, _EffectManager.rightHand.position, null,
                m_EffectTopDownHandTimer, null, _EffectManager.rightHand);

            Physics.Raycast(_EffectManager.spawnPosUp.position, -_EffectManager.spawnPosUp.up,
                out var _hit);

            _EffectManager.GetEffectOrNull(EPrefabName.TopDown, _hit.point, null, m_EffectTopDownTimer,
                m_EffectTopDownSpawnDelay);
        }
    }
}