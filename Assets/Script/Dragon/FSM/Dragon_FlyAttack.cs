using System.Collections;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_FlyAttack : State<Dragon_Controller>
    {
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyAttack.Fly");
        private readonly int m_FlyAttackHash = Animator.StringToHash("FlyAttack");
        private readonly WaitForSeconds m_SmokeReturn = new WaitForSeconds(5.0f);
        private readonly Collider[] m_Results = new Collider[1];
        private WaitUntil m_CurrentAnimIsFly;
        private Transform m_DragonTr;
        private const float RADIUS = 5f;

        protected override void Init()
        {
            m_DragonTr = owner.GetComponent<Transform>();
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.nav.enabled = false;
            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.currentStateFlag |= EDragonFlag.Fly;
            owner.currentStateFlag |= EDragonFlag.CantParry;
            machine.cancel.Add(owner.StartCoroutine(FlyAttack()));
            _EffectManager.ActiveDragonsMesh(EPrefabName.SpeedUp);
        }

        public override void OnStateExit()
        {
            _EffectManager.DeActiveDragonMesh();
            owner.currentStateFlag &= ~EDragonFlag.CantParry;
            owner.currentStateFlag &= ~EDragonFlag.Fly;
        }

        private IEnumerator FlyAttack()
        {
            yield return owner.StartCoroutine(Fly());
            yield return owner.StartCoroutine(FallDown());
            yield return owner.StartCoroutine(machine.WaitForState());
        }

        private IEnumerator Fly()
        {
            yield return m_CurrentAnimIsFly;
            var endPos = m_DragonTr.position.SetOffsetY(30f);
            yield return owner.StartCoroutine(m_DragonTr.CheckDis(endPos, 2f,() => 
                    owner.transform.position = Vector3.Lerp(owner.transform.position,endPos,Time.deltaTime)));
        }

        private IEnumerator FallDown()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            var endPos = _PlayerController.transform.position;
            yield return owner.StartCoroutine(m_DragonTr.CheckDis(endPos, 4f,() => 
                owner.transform.position = Vector3.Lerp(owner.transform.position,endPos, 2 * Time.deltaTime)));
            Damage();
            machine.animator.SetTrigger(m_FlyAttackHash);
            owner.nav.enabled = true;
        }

        private void Damage()
        {
            Physics.Raycast(m_DragonTr.position, Vector3.down, out var hit);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, hit.point, null, m_SmokeReturn);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, hit.point, null, m_SmokeReturn, null,
                owner.transform);
            
            if (Physics.OverlapSphereNonAlloc(hit.point,RADIUS,m_Results,owner.playerMask) != 0)
            {
                _PlayerController.TakeDamage(owner.DragonStat.damage,
                    (_PlayerController.transform.position - m_DragonTr.position).normalized);
            }
        }
    }
}