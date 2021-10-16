using System.Collections;
using UnityEngine;

namespace Script.Dragon.FSM
{
    public class Dragon_Dead : State<Dragon_Controller>
    {
        private readonly int m_DeathHash = Animator.StringToHash("Death");
        private readonly int m_FlyDeathHash = Animator.StringToHash("FlyDeath");
        private readonly int m_FallDownHash = Animator.StringToHash("FallDown");

        public override void OnStateEnter()
        {
            owner.StopAllCoroutines();
            owner.nav.enabled = false;
            if (owner.stateFlag.HasFlag(EDragonFlag.Fly))
            {
                machine.anim.SetTrigger(m_FlyDeathHash);
                owner.StartCoroutine(FallDown());
            }
            else
            {
                machine.anim.SetTrigger(m_DeathHash);
            }
        }

        private IEnumerator FallDown()
        {
            Physics.Raycast(owner.transform.position, Vector3.down, out var hit);
            var _rig = owner.GetComponent<Rigidbody>();
            _rig.isKinematic = false;
            _rig.useGravity = true;
            while (true)
            {
                if (owner.transform.position.y <= 4f)
                {
                    machine.anim.SetTrigger(m_FallDownHash);
                    break;
                }

                yield return null;
            }

            owner.transform.position = hit.point;
        }
    }
}