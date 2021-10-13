using System.Collections;
using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Dead : State<Dragon_Controller>
    {
        private readonly int m_DeathHash = Animator.StringToHash("Death");
        private readonly int m_FlyDeathHash = Animator.StringToHash("FlyDeath");
        private readonly int m_FallDownHash = Animator.StringToHash("FallDown");
        private readonly int m_FlyAttack = Animator.StringToHash("FlyAttack");
        private readonly int m_Attack = Animator.StringToHash("Attack");
        private readonly int m_Breath = Animator.StringToHash("Breath");
        private readonly int m_Tail = Animator.StringToHash("Tail");
        private readonly int m_Stun = Animator.StringToHash("Stun");

        public override void OnStateEnter()
        {
            owner.currentStateFlag |= EDragonFlag.Dead;
            owner.StopAllCoroutines();
            machine.animator.ResetTrigger(m_FlyAttack);
            machine.animator.ResetTrigger(m_Breath);
            machine.animator.ResetTrigger(m_Attack);
            machine.animator.ResetTrigger(m_Stun);
            machine.animator.ResetTrigger(m_Tail);
            owner.nav.enabled = false;
            if (owner.currentStateFlag.HasFlag(EDragonFlag.Fly))
            {
                machine.animator.SetTrigger(m_FlyDeathHash);
                owner.StartCoroutine(FallDown());
            }
            else
            {
                machine.animator.SetTrigger(m_DeathHash);
            }
        }

        private IEnumerator FallDown()
        {
            Physics.Raycast(owner.transform.position, Vector3.down, out var hit);
            var _rig = owner.GetComponent<Rigidbody>();
            _rig.useGravity = true;
            while (true)
            {
                if (owner.transform.position.y <= 4f)
                {
                    break;
                }

                yield return null;
            }

            owner.transform.position = hit.point;
            machine.animator.SetTrigger(m_FallDownHash);
        }
    }
}