using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_FlyBreath : State<Dragon_Controller>
    {
        private readonly int m_BreathHash = Animator.StringToHash("HeadFire");
        private readonly int m_FlyHash = Animator.StringToHash("FlyBreath");
        private readonly WaitForSeconds m_ForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_BreathTime = new WaitForSeconds(7f);
        private WaitUntil m_WaitFly;

        protected override void Init()
        {
            m_WaitFly = new WaitUntil(() =>
                machine.anim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.FlyBreath.Fly"));
        }

        public override void OnStateEnter()
        {
            owner.stateFlag |= EDragonFlag.CantParry;
            owner.stateFlag |= EDragonFlag.Fly;
            owner.StartCoroutine(FlyBreath());
            machine.anim.SetTrigger(m_FlyHash);
            owner.nav.enabled = false;
        }

        public override void OnStateExit()
        {
            owner.stateFlag &= ~EDragonFlag.CantParry;
            owner.stateFlag &= ~EDragonFlag.Fly;
        }

        private IEnumerator FlyBreath()
        {
            var _pos = owner.transform.position;
            _EffectManager.GetEffect(EPrefabName.BreathForce, _pos, null, m_BreathTime, m_ForceDelay);
            yield return m_WaitFly;

            _pos.y += 6.1f;
            while ((_pos - owner.transform.position).sqrMagnitude >= 1)
            {
                owner.transform.position = Vector3.Lerp(owner.transform.position, _pos, 2 * Time.deltaTime);
                yield return null;
            }
            Breath(true);
            yield return m_BreathTime;
            Breath(false);

            yield return machine.WaitForState();
            owner.nav.enabled = true;
        }

        private void Breath(bool isActive)
        {
            if (isActive)
            {
                machine.anim.SetLayerWeight(1, 0.5f);
                machine.anim.SetTrigger(m_BreathHash);
            }
            else
            {
                machine.anim.SetLayerWeight(1, 0);
                machine.anim.SetTrigger(m_FlyHash);
            }
        }
    }
}