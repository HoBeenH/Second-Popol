using UnityEngine;
using System.Collections;

namespace Script.Dragon
{
    public class G_Dragon_Tail : State<DragonController>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Tail_Idle.Attack L");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private readonly int m_TailHash = Animator.StringToHash("Tail");
        private readonly WaitForSeconds m_TailCoolTIme = new WaitForSeconds(12.0f);
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.5f);
        private WaitUntil m_CurrentAnimIsAttack;
        private WaitUntil m_CurrentAnimIsIdle;

        protected override void Init()
        {
            m_CurrentAnimIsAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackLAnimHash); 
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_TailHash);
            owner.StopAnim += HitParry;
            owner.bReadyTail = false;
            owner.StartCoroutine(WaitForAnim());
            owner.StartCoroutine(CoolTime());
        }

        private void HitParry()
        {
            owner.StopCoroutine(WaitForAnim());
        }

        private IEnumerator CoolTime()
        {
            yield return m_TailCoolTIme;
            owner.bReadyTail = true;
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsAttack;
            if (owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                machine.animator.SetTrigger(m_TailHash);
            }
            yield return m_CurrentAnimIsIdle;
            yield return m_Delay;
            machine.ChangeState<S_Dragon_Movement>();
            owner.StopAnim -= HitParry;
        }
    }
}