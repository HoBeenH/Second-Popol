using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Pattern : State<DragonController>
    {
        private readonly WaitForSeconds m_PatternCoolTime = new WaitForSeconds(15.0f);
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly int m_TakeOffHash = Animator.StringToHash("Base Layer.Phase2Start.Takeoff");
        private readonly int m_FlyOffHash = Animator.StringToHash("Base Layer.Phase2Start.Fly");

        protected override void Init()
        {
            base.Init();
        }

        public override void OnStateEnter()
        {
            owner.StartCoroutine(CoolTime());
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


        private IEnumerator CoolTime()
        {
            yield return m_PatternCoolTime;
            owner.bReadyPattern = true;
        }

        private IEnumerator PatternStart()
        {
            machine.animator.SetTrigger(m_PatternHash);
            yield return new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_TakeOffHash);
            owner.nav.SetDestination(owner.transform.TransformDirection(owner.transform.forward));
            yield return new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.3f);
            while (owner.nav.baseOffset <= 7f)
            {
                owner.nav.baseOffset = Mathf.Lerp(owner.nav.baseOffset, 3f, Time.deltaTime);
                yield return null;
            }
        }
    }
}