using System.Collections;
using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private readonly float m_DisToCondition = Mathf.Pow(5, 2);

        public override void Init()
        {
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateUpdate()
        {
            machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude, owner.dragonStat.moveAnimDamp, Time.deltaTime);
            owner.nav.SetDestination(owner.player.position);
        }

        public override void OnStateFixedUpdate()
        {
            base.OnStateFixedUpdate();
        }

        public override void OnStateExit()
        {
        }

        public override void OnStateChangePoint()
        {
            var _dis = (owner.player.position - owner.transform.position).sqrMagnitude;
            if (owner.nav.velocity == Vector3.zero && _dis <= m_DisToCondition)
            {
                machine.ChangeState<G_Dragon_Attack>();
            }
        }
    }
}