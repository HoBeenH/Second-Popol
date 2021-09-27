using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Attack : State<DragonController>
    {
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");
        private readonly int m_AttackAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private readonly WaitForSeconds m_AttackCoolTime = new WaitForSeconds(12.0f);
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.5f);
        private WaitUntil m_CurrentAnimIsAttack;
        private WaitUntil m_CurrentAnimIsIdle;

        protected override void Init()
        {
            m_CurrentAnimIsAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackAnimHash);
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.bReadyAttack = false;
            owner.StopAnim += HitParry;
            machine.animator.SetTrigger(m_AttackTriggerHash);
            owner.StartCoroutine(WaitForAnim());
            owner.StartCoroutine(CoolTime());
        }

        private void HitParry()
        {
            owner.StopCoroutine(WaitForAnim());
        }

        private IEnumerator CoolTime()
        {
            yield return m_AttackCoolTime;
            owner.bReadyAttack = true;
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsAttack;
            yield return m_CurrentAnimIsIdle;
            yield return m_Delay;
            owner.StopAnim -= HitParry;
            machine.ChangeState<S_Dragon_Movement>();
        }
    }
}