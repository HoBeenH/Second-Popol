using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Attack : State<DragonController>
    {
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");
        private WaitUntil m_CurrentAnimIsAttack;
        private WaitUntil m_CurrentAnimIsIdle;
        private readonly int m_AttackAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");

        public override void Init()
        {
            m_CurrentAnimIsAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackAnimHash);
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.transform.LookAt(owner.player);
            owner.AttackWaitCoru += HitParry;
            machine.animator.SetTrigger(m_AttackTriggerHash);
            owner.StartCoroutine(WaitForAnim());
        }

        private void HitParry()
        {
            owner.StopCoroutine(WaitForAnim());
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsAttack;
            yield return m_CurrentAnimIsIdle;
            owner.AttackWaitCoru -= HitParry;
            machine.ChangeState<S_Dragon_Movement>();
        }
    }
}