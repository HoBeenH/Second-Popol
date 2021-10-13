using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_FlyBreath : State<Dragon_Controller>
    {
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyBreath.Fly");
        private readonly int m_BreathHash = Animator.StringToHash("HeadFire");
        private readonly int m_FlyHash = Animator.StringToHash("FlyBreath");
        private readonly WaitForSeconds m_ForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_ForceReturn = new WaitForSeconds(8f);
        private WaitUntil m_CurrentAnimIsFly;

        protected override void Init()
        {
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }
        
        public override void OnStateEnter()
        {
            owner.nav.enabled = false;
            machine.animator.SetTrigger(m_FlyHash);
            machine.cancel.Add(owner.StartCoroutine(FlyBreath()));
        }

        private IEnumerator FlyBreath()
        {
            yield return owner.StartCoroutine(Fly());
            yield return owner.StartCoroutine(Breath());
            yield return machine.WaitForState();
            owner.nav.enabled = true;
        }

        private IEnumerator Fly()
        {
            var pos = owner.transform.position;
            _EffectManager.GetEffect(EPrefabName.BreathForce, pos, null, m_ForceReturn, m_ForceDelay);
            yield return m_CurrentAnimIsFly;
            pos = pos.SetOffsetY(5.5f);
            yield return owner.StartCoroutine(owner.transform.CheckDis(pos, 1, () =>
                owner.transform.position = Vector3.Lerp(owner.transform.position, pos, 2 * Time.deltaTime)));
        }

        private IEnumerator Breath()
        {
            machine.animator.SetLayerWeight(1, 1);
            machine.animator.SetTrigger(m_BreathHash);
            _EffectManager.SetActiveDragonFlyBreath(true);
            var _runningTime = 0f;
            yield return owner.StartCoroutine(DragonLibrary.CheckTime(7f, () =>
            {
                owner.transform.LookAt(_PlayerController.transform);
                owner.transform.SinMove(2f,0.01f,ref _runningTime);
            }));
            machine.animator.SetLayerWeight(1, 0);
            _EffectManager.SetActiveDragonFlyBreath(false);
            machine.animator.SetTrigger(m_FlyHash);
        }
    }
}