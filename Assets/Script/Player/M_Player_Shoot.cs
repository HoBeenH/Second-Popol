using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class M_Player_Shoot : State<PlayerController>
    {
        private readonly WaitForSeconds m_ShootReturn = new WaitForSeconds(20.0f);
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.6f);
        private readonly WaitForSeconds m_HandReturn = new WaitForSeconds(6f);
        private readonly int m_ShootHash;

        public M_Player_Shoot() : base("Base Layer.Skill.Shoot") => m_ShootHash = Animator.StringToHash("Shoot");

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_ShootHash);
            machine.cancel.Add(owner.StartCoroutine(SetEffect()));
            machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(animToHash)));
        }

        private IEnumerator SetEffect()
        {
            var leftHand = _EffectManager.leftHand;
            var rightHand = _EffectManager.rightHand;
            _EffectManager.GetEffect(EPrefabName.ShootHand, leftHand.position, null, m_HandReturn, null, leftHand);
            _EffectManager.GetEffect(EPrefabName.ShootHand, rightHand.position, null, m_HandReturn, null, rightHand);
            yield return m_Delay;
          
            _EffectManager.GetEffect(EPrefabName.Shoot, _EffectManager.spawnPosFw.position,
                Quaternion.LookRotation(owner.transform.forward), m_ShootReturn);
        }
    }
}