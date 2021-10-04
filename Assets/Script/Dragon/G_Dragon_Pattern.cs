using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Pattern : State<DragonController>
    {
        private readonly WaitForSeconds m_PatternCoolTime = new WaitForSeconds(15.0f);
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly int m_TakeOffHash = Animator.StringToHash("Base Layer.Phase2Start.Takeoff");
        private readonly int m_FlyHash = Animator.StringToHash("Base Layer.Phase2Start.Fly");

        public override void OnStateEnter()
        {
            owner.StartCoroutine(PatternStart());
        }

        public override void OnStateUpdate()
        {
            base.OnStateUpdate();
        }

        public override void OnStateExit()
        {
            base.OnStateExit();
        }

        private IEnumerator PatternStart()
        {
            machine.animator.SetTrigger(m_PatternHash);
            yield return new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_TakeOffHash);
            owner.nav.SetDestination(_PlayerController.transform.position);
            yield return new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.3f);
            while (owner.nav.baseOffset <= 9f)
            {
                owner.nav.baseOffset = Mathf.Lerp(owner.nav.baseOffset, 10f, Time.deltaTime);
                yield return null;
            }
            
            yield return m_PatternCoolTime;
            owner.bReadyPattern = true;
        }
    }
}