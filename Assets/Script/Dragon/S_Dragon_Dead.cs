using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Dead : State<DragonController>
    {
        private readonly int m_DeathHash = Animator.StringToHash("Death");
        private readonly int m_FallDownHash = Animator.StringToHash("FallDown");
        private readonly int m_FlyAttack = Animator.StringToHash("FlyAttack");
        private readonly int m_Attack = Animator.StringToHash("Attack");
        private readonly int m_Breath = Animator.StringToHash("Breath");
        private readonly int m_Tail = Animator.StringToHash("Tail");
        private readonly int m_Stun = Animator.StringToHash("Stun");
        

        public override void OnStateEnter()
        {
            owner.StopAllCoroutines();
            machine.animator.ResetTrigger(m_Attack);
            machine.animator.ResetTrigger(m_Tail);
            machine.animator.ResetTrigger(m_FlyAttack);
            machine.animator.ResetTrigger(m_Breath);
            machine.animator.ResetTrigger(m_Stun);
            machine.animator.SetTrigger(m_DeathHash);
            if (owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Fly))
            {
                owner.StartCoroutine(FallDown(owner.nav.baseOffset));
            }
        }

        private IEnumerator FallDown(float currentOffset)
        {
            while (currentOffset >= 2f)
            {
                currentOffset = Mathf.Lerp(currentOffset, 0f, 2f * Time.deltaTime);
                owner.nav.baseOffset = currentOffset;

                yield return null;
            }

            machine.animator.SetTrigger(m_FallDownHash);
            owner.nav.baseOffset = 0;
        }
    }
}