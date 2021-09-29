using UnityEngine;
using System.Collections;

namespace Script.Dragon
{
    public class G_Dragon_Tail : State<DragonController>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Tail_Idle.Attack L");
        private readonly int m_TailHash = Animator.StringToHash("Tail");
        private readonly WaitForSeconds m_TailCoolTIme = new WaitForSeconds(12.0f);

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_TailHash);
            owner.StopAnim += HitParry;
            owner.bReadyTail = false;
            owner.StartCoroutine(CoolTime());
            owner.StartCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement), m_AttackLAnimHash));
        }

        public override void OnStateExit()
        {
            owner.StopAnim -= HitParry;
        }

        private void HitParry()
        {
            owner.StopCoroutine(machine.WaitForIdle(typeof(S_Dragon_Movement), m_AttackLAnimHash));
        }

        private IEnumerator CoolTime()
        {
            yield return m_TailCoolTIme;
            owner.bReadyTail = true;
        }
    }
}