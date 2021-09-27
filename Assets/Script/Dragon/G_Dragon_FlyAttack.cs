using System.Collections;
using Sirenix.Utilities;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_FlyAttack : State<DragonController>
    {
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyAttack.Fly");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private readonly int m_FlyAttackHash = Animator.StringToHash("FlyAttack");
        private readonly int m_bFlyAttackHash = Animator.StringToHash("NowFly");
        private readonly WaitForSeconds m_FlyAttackCoolTime = new WaitForSeconds(20.0f);
        private readonly WaitForSeconds m_Delay = new WaitForSeconds(0.5f);
        private WaitUntil m_CurrentAnimIsFly;
        private WaitUntil m_CurrentAnimIsIdle;

        protected override void Init()
        {
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
            m_CurrentAnimIsIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry;
            owner.bReadyFlyAttack = false;
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(FlyAttack());
        }

        public override void OnStateExit()
        {
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
        }

        private IEnumerator CoolTime()
        {
            yield return m_FlyAttackCoolTime;
            owner.bReadyFlyAttack = true;
        }

        private IEnumerator FlyAttack()
        {
            var currentOffset = owner.nav.baseOffset;
            machine.animator.SetTrigger(m_FlyAttackHash);
            machine.animator.SetBool(m_bFlyAttackHash, true);
            while (currentOffset <= 1)
            {
                currentOffset = Mathf.Lerp(currentOffset, 2, Time.deltaTime);
                owner.nav.baseOffset = currentOffset;
                yield return null;
            }

            yield return m_CurrentAnimIsFly;
            while (currentOffset <= 19f)
            {
                currentOffset = Mathf.Lerp(currentOffset, 20, Time.deltaTime);
                owner.nav.baseOffset = currentOffset;
                yield return null;
            }

            machine.animator.SetTrigger(m_FlyAttackHash);
            var temp = (owner.player.GetComponent<Rigidbody>().velocity + owner.player.position);
            owner.nav.SetDestination(temp);
            while (currentOffset >= 2f)
            {
                Physics.Raycast(owner.transform.position, owner.transform.forward, 1f);
                currentOffset = Mathf.Lerp(currentOffset, 0f, 2f * Time.deltaTime);
                owner.nav.baseOffset = currentOffset;

                yield return null;
            }

            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.nav.ResetPath();

            owner.nav.baseOffset = 0;

            yield return m_CurrentAnimIsIdle;
            machine.animator.SetBool(m_bFlyAttackHash, false);
            yield return m_Delay;

            machine.ChangeState<S_Dragon_Movement>();
        }
    }
}