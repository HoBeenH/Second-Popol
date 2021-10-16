using UnityEngine;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_Movement : State<Dragon_Controller>
    {
        private readonly int m_MovementHash = Animator.StringToHash("Move");
        private float m_Dis;

        protected override void Init() => m_Dis = Mathf.Pow(owner.nav.stoppingDistance, 2);

        public override void OnStateUpdate()
        {
            machine.anim.SetFloat(m_MovementHash, owner.nav.desiredVelocity.magnitude, 0.02f, Time.deltaTime);
            owner.nav.SetDestination(_PlayerController.transform.position);
        }

        public override void OnStateChangePoint()
        {
            if ((_PlayerController.transform.position - owner.transform.position).sqrMagnitude <= m_Dis &&
                !_DragonPattern.nowDelay)
            {
                var _nextState = _DragonPattern.NextPattern();
                if (_nextState != null)
                {
                    machine.ChangeState(_nextState);
                }
            }
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
            machine.anim.SetFloat(m_MovementHash, 0f);
        }
    }
}