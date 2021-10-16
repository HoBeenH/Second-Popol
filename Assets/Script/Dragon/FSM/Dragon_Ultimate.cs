using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_Ultimate : State<Dragon_Controller>
    {
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly WaitForSeconds m_Smoke1Return = new WaitForSeconds(3f);
        private readonly WaitForSeconds m_Smoke2Return = new WaitForSeconds(5f);
        private readonly WaitForSeconds m_UltimateTime = new WaitForSeconds(15f);
        private WaitUntil m_WaitTakeOff;
        private Vector3 m_UltimatePos;
        private Transform m_DragonTr;

        protected override void Init()
        {
            m_DragonTr = owner.GetComponent<Transform>();
            m_WaitTakeOff = new WaitUntil(
                () => machine.anim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Ultimate.Takeoff") &&
                      machine.anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.35f);
            m_UltimatePos = GameObject.FindGameObjectWithTag("UltimatePos").transform.position;
        }

        public override void OnStateEnter()
        {
            owner.nav.enabled = false;
            owner.stateFlag |= EDragonFlag.CantParry;
            owner.stateFlag |= EDragonFlag.Fly;
            owner.StartCoroutine(Ultimate());
            _EffectManager.ActiveDragonMeshEffect(EPrefabName.Ultimate);
        }

        public override void OnStateExit()
        {
            _EffectManager.DeActiveDragonMeshEffect();
            owner.stateFlag &= ~EDragonFlag.CantParry;
            owner.stateFlag &= ~EDragonFlag.Fly;
        }

        private IEnumerator Ultimate()
        {
            machine.anim.SetTrigger(m_PatternHash);
            yield return m_WaitTakeOff;
            
            while ((m_UltimatePos - owner.transform.position).sqrMagnitude >= 9f)
            {
                m_DragonTr.position = Vector3.Lerp(m_DragonTr.position, m_UltimatePos, Time.deltaTime * 0.5f);
                m_DragonTr.rotation = Quaternion.Slerp(m_DragonTr.rotation, Quaternion.LookRotation(m_UltimatePos),
                    2 * Time.deltaTime);
                yield return null;
            }
            
            machine.anim.SetTrigger(m_PatternHash);
            yield return owner.StartCoroutine(CheckTime());
            
            _EffectManager.SetActiveUltimate(true);
            yield return m_UltimateTime;
            
            var _endPos = _PlayerController.transform.position;
            machine.anim.SetTrigger(m_PatternHash);
            while ((_endPos - m_DragonTr.position).sqrMagnitude >= 4f)
            {
                m_DragonTr.position = Vector3.Lerp(m_DragonTr.position, _endPos, 2f * Time.deltaTime);
                yield return null;
            }
            
            SetEffect();
            _EffectManager.SetActiveUltimate(false);
            machine.anim.SetTrigger(m_PatternHash);
            owner.nav.enabled = true;
            
            yield return owner.StartCoroutine(machine.WaitForState());
        }

        private void SetEffect()
        {
            var _effectPos = m_DragonTr.position;
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, _effectPos, null, m_Smoke1Return);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, _effectPos, null, m_Smoke2Return, null, m_DragonTr);
        }
        
        private IEnumerator CheckTime()
        {
            var _timer = 0f;
            while (_timer <= 4f)
            {
                m_DragonTr.rotation = Quaternion.Slerp(m_DragonTr.rotation, Quaternion.identity, Time.deltaTime);
                _timer += Time.deltaTime;
                yield return null;
            }
        }
    }
}