using System.Collections;
using Script.Player;
using Script.Player.Effect;
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
        private readonly WaitForSeconds m_SmokeReturnTime = new WaitForSeconds(3.0f);
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
            var _targetPos = owner.player.position;
            owner.nav.SetDestination(_targetPos);
            while (currentOffset >= 3f)
            {
                currentOffset = Mathf.Lerp(currentOffset, 0f, 2f * Time.deltaTime);
                owner.nav.baseOffset = currentOffset;
                yield return null;
            }

            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.nav.ResetPath();
            owner.nav.baseOffset = 0;
            var _position = owner.transform.position;
            EffectManager.Instance.GetEffectOrNull(EPrefabName.DragonDownSmoke, _position, null,
                m_SmokeReturnTime);
            EffectManager.Instance.GetEffectOrNull(EPrefabName.DragonDownSmoke2, _position, null,
                m_SmokeReturnTime, null, owner.transform);
            var _result = new Collider[1];
            int _size = Physics.OverlapSphereNonAlloc(_position, 5F, _result, owner.playerMask);
            if (_size == 0)
            {
                yield break;;
            }

            var temp = (owner.player.position - owner.transform.position).normalized;
            PlayerController.Instance.useFallDown.Invoke(temp,5f);
            PlayerController.Instance.TakeDamage(owner.DragonStat.damage);
        }
    }
}