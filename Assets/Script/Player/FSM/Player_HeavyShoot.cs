using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_HeavyShoot : State<Player_Controller>
    {
        private readonly int m_HeavyShootHash;
        private readonly WaitForSeconds m_Return = new WaitForSeconds(15.0f);
        private readonly WaitForSeconds m_First = new WaitForSeconds(1.7f);
        private readonly WaitForSeconds m_Second = new WaitForSeconds(0.6f);
        private readonly WaitForSeconds m_Last = new WaitForSeconds(1.7f);

        public Player_HeavyShoot() : base("Base Layer.Skill.Heavy Shoot") =>
            m_HeavyShootHash = Animator.StringToHash("Heavy Shoot");

        protected override void Init()
        {
            _SkillManager.AddSkill(typeof(Player_HeavyShoot),15f);
        }

        public override void OnStateEnter()
        {
            machine.anim.SetTrigger(m_HeavyShootHash);
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
            machine.cancel.Add(owner.StartCoroutine(SetEffect()));
        }

        private IEnumerator SetEffect()
        {
            var _rot = Quaternion.LookRotation(owner.transform.forward);
            var _pos = _EffectManager.playerSpawnPosFW.position;
            var _leftHand = _EffectManager.playerLeftHand;
            var _rightHand = _EffectManager.playerRightHand;
            _EffectManager.GetEffect(EPrefabName.HeavyShootHand, _leftHand.position, null, m_Return, null, _leftHand);
            _EffectManager.GetEffect(EPrefabName.HeavyShootHand, _rightHand.position, null, m_Return, null, _rightHand);
            yield return m_First;
            _EffectManager.GetEffect(EPrefabName.FirstHeavyShoot, _pos, _rot, m_Return);
            yield return m_Second;
            _EffectManager.GetEffect(EPrefabName.SecondHeavyShoot, _pos, _rot, m_Return);
            yield return m_Last;
            _EffectManager.GetEffect(EPrefabName.LastHeavyShoot, _pos, _rot, m_Return);
        }
    }
}