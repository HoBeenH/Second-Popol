using System;
using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_FlyAttack : State<Dragon_Controller>
    {
        private readonly int m_FlyAttackHash = Animator.StringToHash("FlyAttack");
        private readonly WaitForSeconds m_SmokeReturn = new WaitForSeconds(5.0f);
        private readonly Collider[] m_Results = new Collider[1];
        private const float RADIUS = 5f;
        private Transform m_DragonTr;
        private WaitUntil m_WaitFly;

        protected override void Init()
        {
            m_DragonTr = owner.GetComponent<Transform>();
            m_WaitFly = new WaitUntil(() =>
                machine.anim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.FlyAttack.Fly"));
        }

        public override void OnStateEnter()
        {
            owner.nav.enabled = false;
            machine.anim.SetTrigger(m_FlyAttackHash);
            owner.stateFlag |= EDragonFlag.Fly;
            owner.stateFlag |= EDragonFlag.CantParry;
            owner.StartCoroutine(FlyAttack());
            _EffectManager.ActiveDragonMeshEffect(EPrefabName.FlyAttack);
        }

        public override void OnStateExit()
        {
            _EffectManager.DeActiveDragonMeshEffect();
            owner.stateFlag &= ~EDragonFlag.CantParry;
            owner.stateFlag &= ~EDragonFlag.Fly;
        }

        private IEnumerator FlyAttack()
        {
            yield return m_WaitFly;

            var _startPos = m_DragonTr.position;
            _startPos.y += 30f;

            yield return owner.StartCoroutine(CheckDis(2f, _startPos, Time.deltaTime));
            
            machine.anim.SetTrigger(m_FlyAttackHash);
            var _endPos = _PlayerController.transform.position;
            
            yield return owner.StartCoroutine(CheckDis(4f, _endPos, 2 * Time.deltaTime));

            SetEffect();
            machine.anim.SetTrigger(m_FlyAttackHash);
            owner.nav.enabled = true;
            yield return owner.StartCoroutine(machine.WaitForState());
        }

        private void SetEffect()
        {
            Physics.Raycast(m_DragonTr.position, Vector3.down, out var hit);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, hit.point, null, m_SmokeReturn);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, hit.point, null, m_SmokeReturn, null, m_DragonTr);

            if (Physics.OverlapSphereNonAlloc(hit.point, RADIUS, m_Results, owner.playerMask) != 0)
            {
                _PlayerController.TakeDamage(owner.Stat.damage,
                    (_PlayerController.transform.position - m_DragonTr.position).normalized);
                Array.Clear(m_Results, 0, 1);
            }
        }

        private IEnumerator CheckDis(float dis, Vector3 pos, float speed)
        {
            var _endDis = Mathf.Pow(dis, 2);
            while ((pos - m_DragonTr.position).sqrMagnitude >= _endDis)
            {
                m_DragonTr.position = Vector3.Lerp(m_DragonTr.position, pos, speed);
                yield return null;
            }
        }
    }
    
}