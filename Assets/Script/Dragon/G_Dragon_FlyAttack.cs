using System.Collections;
using Script.Player;
using Script.Player.Effect;
using UnityEngine;
using static Script.Facade;

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
        private readonly Collider[] m_Results = new Collider[1];

        protected override void Init()
        {
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry | EDragonPhaseFlag.Fly;
            owner.bReadyFlyAttack = false;
            owner.StartCoroutine(FlyAttack());
        }

        public override void OnStateExit()
        {
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.Fly;
            machine.animator.SetBool(m_bFlyAttackHash, false);
        }

        private IEnumerator FlyAttack()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            machine.animator.SetBool(m_bFlyAttackHash, true);

            yield return owner.StartCoroutine(Fly());
            yield return owner.StartCoroutine(FallDown());

            yield return owner.StartCoroutine(machine.WaitForAnim(typeof(S_Dragon_Movement)));
            yield return m_FlyAttackCoolTime;
            owner.bReadyFlyAttack = true;
        }

        private IEnumerator Fly()
        {
            yield return m_CurrentAnimIsFly;
            while (owner.nav.baseOffset <= 19f)
            {
                owner.nav.baseOffset = Mathf.Lerp(owner.nav.baseOffset, 20, Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator FallDown()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            var _targetPos = _PlayerController.transform.position;
            while (owner.nav.baseOffset >= 3f)
            {
                owner.nav.SetDestination(_targetPos);
                owner.nav.baseOffset = Mathf.Lerp(owner.nav.baseOffset, 0f, 1.5f * Time.deltaTime);
                yield return null;
            }

            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.nav.ResetPath();
            owner.nav.baseOffset = 0;
            var _position = owner.transform.position;
            _EffectManager.GetEffectOrNull(EPrefabName.DragonDownSmoke, _position, null,
                m_SmokeReturnTime);
            _EffectManager.GetEffectOrNull(EPrefabName.DragonDownSmoke2, _position, null,
                m_SmokeReturnTime, null, owner.transform);
            var _radius = 5f;
            if (owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.HealthUp))
            {
                _radius = 10f;
            }

            var _size = Physics.OverlapSphereNonAlloc(_position, _radius, m_Results, owner.playerMask);
            if (_size == 0)
            {
                yield break;
            }

            _PlayerController.TakeDamage(owner.DragonStat.damage,
                (_PlayerController.transform.position - owner.transform.position).normalized);
            
        }
    }
}