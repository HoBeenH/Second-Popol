using System.Collections;
using Script.Player.Effect;
using UnityEngine;

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
            owner.useActionCam();
            owner.StartCoroutine(CoolTime());
            SetEffect();
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Player_Movement),animToHash));
        }

        public override void OnStateExit()
        {
            owner.useDefaultCam();
        }

        private IEnumerator CoolTime()
        {
            yield return m_TopDownCool;
            owner.bTopDownCoolTime = true;
        }

        private void SetEffect()
        {
            EffectManager.Instance.GetEffectOrNull(EPrefabName.TopDownHand,
                EffectManager.Instance.leftHand.transform.position, null,
                m_EffectTopDownHandTimer, null, EffectManager.Instance.leftHand);
            
            EffectManager.Instance.GetEffectOrNull(EPrefabName.TopDownHand,
                EffectManager.Instance.rightHand.transform.position, null,
                m_EffectTopDownHandTimer, null, EffectManager.Instance.rightHand);
            
            Physics.Raycast(EffectManager.Instance.spawnPosUp.position, -EffectManager.Instance.spawnPosUp.up,
                out var _hit);
            
            EffectManager.Instance.GetEffectOrNull(EPrefabName.TopDown, _hit.point, null, m_EffectTopDownTimer,
                m_EffectTopDownSpawnDelay);
        }
    }
}