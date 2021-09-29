using System.Collections;
using Script.Player;
using Script.Player.Effect;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Breath : State<DragonController>
    {
        private readonly int m_BreathAnimHash = Animator.StringToHash("Base Layer.Breath_Idle.Breath_Idle");
        private readonly int m_BreathHash = Animator.StringToHash("Breath");
        private readonly WaitForSeconds m_BreathCoolTime = new WaitForSeconds(20f);
        private readonly WaitForSeconds m_BreathForceReturn = new WaitForSeconds(8f);
        private readonly WaitForSeconds m_BreathForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_BreathDelay = new WaitForSeconds(2.5f);

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry;
            owner.bReadyBreath = false;
            machine.animator.SetTrigger(m_BreathHash);
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement), m_BreathAnimHash));
            EffectManager.Instance.GetEffectOrNull(EPrefabName.BreathForce, owner.transform.position, null,
                m_BreathForceReturn, m_BreathForceDelay);
            PlayerController.Instance.useFallDown.Invoke();
            
            EffectManager.Instance.DragonBreath(true,m_BreathDelay);
        }

        public override void OnStateExit()
        {
            EffectManager.Instance.DragonBreath(false);
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
        }

        private IEnumerator CoolTime()
        {
            yield return m_BreathCoolTime;
            owner.bReadyBreath = true;
        }
    }
}