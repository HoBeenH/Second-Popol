using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_Ultimate : State<Dragon_Controller>
    {
        private readonly int m_TakeOffHash = Animator.StringToHash("Base Layer.Phase2Start.Takeoff");
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly WaitForSeconds m_Smoke1Return = new WaitForSeconds(3f);
        private readonly WaitForSeconds m_Smoke2Return = new WaitForSeconds(5f);
        private readonly WaitForSeconds m_Pattern = new WaitForSeconds(40f);
        private WaitUntil m_WaitTakeOff;
        private Vector3 m_UltimatePos;
        private Transform m_DragonTr;

        protected override void Init()
        {
            m_DragonTr = owner.GetComponent<Transform>();
            m_WaitTakeOff = new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_TakeOffHash &&
                      machine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.35f);
            m_UltimatePos = GameObject.FindGameObjectWithTag("UltimatePos").transform.position;
        }

        public override void OnStateEnter()
        {
            owner.nav.enabled = false;
            owner.currentStateFlag |= EDragonFlag.CantParry;
            owner.currentStateFlag |= EDragonFlag.Fly;
            machine.cancel.Add(owner.StartCoroutine(Ultimate()));
            _EffectManager.ActiveDragonsMesh(EPrefabName.DamageUp);
        }

        public override void OnStateExit()
        {
            _EffectManager.DeActiveDragonMesh();
            owner.currentStateFlag &= ~EDragonFlag.CantParry;
            owner.currentStateFlag &= ~EDragonFlag.Fly;
        }

        private IEnumerator Ultimate()
        {
            machine.animator.SetTrigger(m_PatternHash);
            yield return m_WaitTakeOff;
            yield return owner.StartCoroutine(MovePos());
            yield return owner.StartCoroutine(UltimateStart());
            yield return owner.StartCoroutine(UltimateEnd());
            yield return owner.StartCoroutine(machine.WaitForState());
        }

        private IEnumerator MovePos()
        {
            var _tr = owner.transform;
            yield return owner.StartCoroutine(owner.transform.CheckDis(m_UltimatePos, 2f, () =>
            {
                _tr.position = Vector3.Lerp(_tr.position, m_UltimatePos, Time.deltaTime * 0.5f);
                _tr.rotation = Quaternion.Slerp(owner.transform.rotation,
                    Quaternion.LookRotation(m_UltimatePos), 2 * Time.deltaTime);
            }));
            yield return owner.StartCoroutine(DragonLibrary.CheckTime(4f, () =>
                m_DragonTr.rotation = Quaternion.Slerp(m_DragonTr.rotation, Quaternion.identity, Time.deltaTime)));
        }

        private IEnumerator UltimateStart()
        {
            _EffectManager.SetActiveUltimate(true);
            var _time = 0f;
            yield return owner.StartCoroutine(DragonLibrary.CheckTime(30f, () =>
                owner.transform.SinMove(1f, 0.04f, ref _time)));
        }

        private IEnumerator UltimateEnd()
        {
            var _tr = owner.transform;
            var _endPos = _PlayerController.transform.position;
            yield return owner.StartCoroutine(owner.transform.CheckDis(_endPos, 4f, () =>
            {
                _tr.position = Vector3.Lerp(_tr.position, _endPos, 2f * Time.deltaTime);
                _tr.rotation = Quaternion.Slerp(_tr.rotation, Quaternion.LookRotation(_endPos), 2 * Time.deltaTime);
            }));
            var _effectPos = m_DragonTr.position;
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, _effectPos, null, m_Smoke1Return);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, _effectPos, null, m_Smoke2Return, null, m_DragonTr);
            machine.animator.SetTrigger(m_PatternHash);
            owner.nav.enabled = true;
        }
    }
}