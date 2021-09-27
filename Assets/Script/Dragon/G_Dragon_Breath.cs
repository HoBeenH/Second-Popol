using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Breath : State<DragonController>
    {
        private readonly int m_BreathAnimHash = Animator.StringToHash("Base Layer.Breath_Idle.Breath_Idle");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private readonly int m_BreathHash = Animator.StringToHash("Breath");
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.5f);
        private readonly WaitForSeconds m_BreathCoolTime = new WaitForSeconds(20f);
        private WaitUntil m_CurrentAnimIsBreath;
        private WaitUntil m_CurrentAnimIsIdle;

        protected override void Init()
        {
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);   
            m_CurrentAnimIsBreath = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_BreathAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry;
            owner.bReadyBreath = false;
            owner.StartCoroutine(WaitForAnim());
            machine.animator.SetTrigger(m_BreathHash);
        }

        private IEnumerator CoolTime()
        {
            yield return m_BreathCoolTime;
            owner.bReadyBreath = true;
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_CurrentAnimIsBreath;
            yield return m_CurrentAnimIsIdle;
            yield return m_Delay;
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
            machine.ChangeState<S_Dragon_Movement>();
        }

    }
}