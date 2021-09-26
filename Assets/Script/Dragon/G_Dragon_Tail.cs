using UnityEngine;
using System.Collections;

namespace Script.Dragon
{
    public class G_Dragon_Tail : State<DragonController>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Tail_Idle.Attack L");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private WaitUntil m_CurrentAnimIsAttack;
        private WaitUntil m_CurrentAnimIsIdle;
        private readonly int m_TailHash = Animator.StringToHash("Tail");

        public override void Init()
        {
            m_CurrentAnimIsAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackLAnimHash); 
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_TailHash);
            owner.StartCoroutine(WaitForAnim());
            owner.AttackWaitCoru += HitParry;
        }

        private void HitParry()
        {
            owner.StopCoroutine(WaitForAnim());
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsAttack;
            yield return m_CurrentAnimIsIdle;
            machine.ChangeState<S_Dragon_Movement>();
            owner.AttackWaitCoru -= HitParry;
        }
    }
}