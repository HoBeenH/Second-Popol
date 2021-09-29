using System.Collections;
using Script.Player;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Breath : State<DragonController>
    {
        private readonly int m_BreathAnimHash = Animator.StringToHash("Base Layer.Breath_Idle.Breath_Idle");
        private readonly int m_BreathHash = Animator.StringToHash("Breath");
        private readonly WaitForSeconds m_BreathCoolTime = new WaitForSeconds(20f);

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.CantParry;
            owner.bReadyBreath = false;
            machine.animator.SetTrigger(m_BreathHash);
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement),m_BreathAnimHash));

            PlayerController.Instance.useFallDown.Invoke();
        }

        public override void OnStateExit()
        {
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.CantParry;
        }

        private IEnumerator CoolTime()
        {
            yield return m_BreathCoolTime;
            owner.bReadyBreath = true;
        }
    }
}