using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Attack : State<DragonController>
    {
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");
        private WaitUntil m_CurrentAnimIsAttack;
        private WaitUntil m_CurrentAnimIsIdle;
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");

        public override void Init()
        {
            m_CurrentAnimIsAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackLAnimHash); 
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_AttackTriggerHash);
            owner.StartCoroutine(WaitForAnim());
        }

        public override void OnStateUpdate()
        {
            base.OnStateUpdate();
        }

        public override void OnStateFixedUpdate()
        {
            base.OnStateFixedUpdate();
        }

        public override void OnStateExit()
        {
            base.OnStateExit();
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsAttack;
            yield return m_CurrentAnimIsIdle;
            machine.ChangeState<S_Dragon_Movement>();
        }
    }
}