using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_MagicTopDown : State<Player_Controller>
    {
        private readonly WaitForSeconds m_Return = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.8f);
        private readonly int m_TopDownHash;

        public Player_MagicTopDown() : base("Base Layer.Skill.Top Down") =>
            m_TopDownHash = Animator.StringToHash("Top Down");

        protected override void Init() => _SkillManager.AddSkill(typeof(Player_MagicTopDown), 10f);

        public override void OnStateEnter()
        {
            machine.anim.SetTrigger(m_TopDownHash);
            machine.cancel.Add(owner.StartCoroutine(SetEffect()));
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        private IEnumerator SetEffect()
        {
            yield return null;
            var leftHand = _EffectManager.playerLeftHand;
            var rightHand = _EffectManager.playerRightHand;
            var pos = _EffectManager.playerSpawnPosUp;
            _EffectManager.GetEffect(EPrefabName.TopDownHand, leftHand.position, null, m_Return, null, leftHand);
            _EffectManager.GetEffect(EPrefabName.TopDownHand, rightHand.position, null, m_Return, null, rightHand);
            Physics.Raycast(pos.position, pos.up * -1, out var _hit);
            _EffectManager.GetEffect(EPrefabName.TopDown, _hit.point, null, m_Return, m_Delay);
        }
    }
}