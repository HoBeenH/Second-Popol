﻿using System.Collections;
using Sirenix.Utilities;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_FlyAttack : State<DragonController>
    {
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyAttack.Fly");
        private readonly int m_FlyAttackHash = Animator.StringToHash("FlyAttack");
        private readonly int m_bFlyAttackHash = Animator.StringToHash("NowFly");
        private readonly WaitForSeconds m_FlyAttackCoolTime = new WaitForSeconds(20.0f);
        private WaitUntil m_CurrentAnimIsFly;

        protected override void Init()
        {
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry | EDragonPhaseFlag.Fly;
            owner.bReadyFlyAttack = false;
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(FlyAttack());
        }

        public override void OnStateExit()
        {
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.Fly;
            machine.animator.SetBool(m_bFlyAttackHash, false);
        }

        private IEnumerator CoolTime()
        {
            yield return m_FlyAttackCoolTime;
            owner.bReadyFlyAttack = true;
        }

        private IEnumerator FlyAttack()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            machine.animator.SetBool(m_bFlyAttackHash, true);

            yield return owner.StartCoroutine(Fly(owner.nav.baseOffset));
            yield return owner.StartCoroutine(FallDown(owner.nav.baseOffset));

            yield return owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement)));
        }

        private IEnumerator Fly(float currentOffset)
        {
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
        }

        private IEnumerator FallDown(float currentOffset)
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            var playerPos = owner.player.position;
            owner.nav.SetDestination(playerPos);
            while (currentOffset >= 3f)
            {
                currentOffset = Mathf.Lerp(currentOffset, 0f, 2f * Time.deltaTime);
                owner.nav.baseOffset = currentOffset;

                yield return null;
            }

            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.nav.ResetPath();
            owner.nav.baseOffset = 0;
        }
    }
}