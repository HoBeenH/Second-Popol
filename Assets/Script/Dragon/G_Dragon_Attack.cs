using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Attack : State<DragonController>
    {
        private readonly int m_AttackTriggerHash = Animator.StringToHash("Attack");
        private readonly int m_AttackAnimHash = Animator.StringToHash("Base Layer.Attack_Idle.Attack 1");
        private readonly WaitForSeconds m_AttackCoolTime = new WaitForSeconds(12.0f);

        public override void OnStateEnter()
        {
            owner.bReadyAttack = false;
            owner.StopAnim += HitParry;
            machine.animator.SetTrigger(m_AttackTriggerHash);
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement),m_AttackAnimHash));
        }

        public override void OnStateExit()
        {
            owner.StopAnim -= HitParry;
        }

        private void HitParry()
        {
            owner.StopAllCoroutines();
        }

        private IEnumerator CoolTime()
        {
            yield return m_AttackCoolTime;
            Debug.Log("!");
            owner.bReadyAttack = true;
        }
    }
}