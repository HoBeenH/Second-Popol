using System;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private float m_DisCondition;

        protected override void Init()
        {
            m_DisCondition = Mathf.Pow(owner.nav.stoppingDistance, 2);
        }
        
        public override void OnStateUpdate()
        {
            machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude,
                0.02f, Time.deltaTime);
            owner.nav.SetDestination(_PlayerController.transform.position);
        }

        public override void OnStateChangePoint()
        {
            if (CheckCondition())
            {
                machine.ChangeState(_DragonPattern.StartPattern());
            }
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
            machine.animator.SetFloat(m_MovementFloatHash, 0f);
        }

        private bool CheckCondition()
        {
            return (_PlayerController.transform.position - owner.transform.position).sqrMagnitude <= m_DisCondition;
        }
    }
}