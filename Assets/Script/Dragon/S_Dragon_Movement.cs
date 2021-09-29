using UnityEngine;

namespace Script.Dragon
{
    public enum EPlayerPoint
    {
        Forward,
        Back,
        Other
    }

    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private float m_DisToCondition;
        private Transform m_Dragon;

        protected override void Init()
        {
            m_Dragon = owner.GetComponent<Transform>();
            m_DisToCondition = Mathf.Pow(owner.nav.stoppingDistance, 2);
        }

        public override void OnStateUpdate()
        {
            // if (owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Frozen))
            // {
            //     return;
            // }
            var _playerPos = owner.player.position;

            machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude,
                owner.dragonStat.moveAnimDamp, Time.deltaTime);
            owner.nav.SetDestination(_playerPos);

            if (CheckDis() == false)
                return;
            var _nextRot = Quaternion.LookRotation((_playerPos - m_Dragon.position).normalized);
            m_Dragon.rotation = Quaternion.Slerp(m_Dragon.rotation, _nextRot,
                owner.dragonStat.rotSpeed * Time.deltaTime);
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            machine.animator.SetFloat(m_MovementFloatHash, 0f);
        }

        public override void OnStateChangePoint()
        {
            if (!CheckDis() == false && owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Frozen)) 
                return;
            switch (PlayerPoint())
            {
                case EPlayerPoint.Forward when owner.bReadyAttack:
                    machine.ChangeState<G_Dragon_Attack>();
                    break;
                case EPlayerPoint.Forward when owner.bReadyBreath:
                    machine.ChangeState<G_Dragon_Breath>();
                    break;
                case EPlayerPoint.Back when owner.bReadyTail:
                    machine.ChangeState<G_Dragon_Tail>();
                    break;
                default:
                    if (owner.bReadyFlyAttack)
                    {
                        machine.ChangeState<G_Dragon_FlyAttack>();
                        return;
                    }
                    break;
            }
        }


        private EPlayerPoint PlayerPoint()
        {
            var temp = owner.player.position - m_Dragon.position;
            var point = Vector3.SignedAngle(owner.transform.forward, temp, m_Dragon.up);
            if (-40f <= point && point < 40f)
            {
                return EPlayerPoint.Forward;
            }

            if (120 <= point || point <= -120)
            {
                return EPlayerPoint.Back;
            }

            return EPlayerPoint.Other;
        }

        private bool CheckDis() => (owner.player.position - owner.transform.position).sqrMagnitude <= m_DisToCondition;
    }
}