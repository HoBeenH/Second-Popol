using UnityEngine;
using static Script.Facade;

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
        private float m_StopDis;
        private Transform m_Dragon;

        protected override void Init()
        {
            m_Dragon = owner.GetComponent<Transform>();
            m_StopDis = owner.nav.stoppingDistance;
            m_DisToCondition = Mathf.Pow(m_StopDis, 2);
        }

        public override void OnStateUpdate()
        {
            var _dir = (_PlayerController.transform.position - m_Dragon.position);
            machine.animator.SetFloat(m_MovementFloatHash, _dir.magnitude - owner.nav.stoppingDistance,
                owner.DragonStat.moveAnimDamp, Time.deltaTime);
            if (CheckDis())
            {
                m_Dragon.rotation = Quaternion.Slerp(m_Dragon.rotation, Quaternion.LookRotation(_dir.normalized),
                    owner.DragonStat.rotSpeed * Time.deltaTime);
            }
            else
            {
                owner.nav.SetDestination(_PlayerController.transform.position);
            }
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
            machine.animator.SetFloat(m_MovementFloatHash, 0f);
        }

        public override void OnStateChangePoint()
        {
            if (CheckDis() == false)
                return;
            if (owner.bReadyPattern && owner.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                machine.ChangeState<G_Dragon_Pattern>();
                return;
            }

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
                    }
                    break;
            }
        }


        private EPlayerPoint PlayerPoint() =>
            Vector3.Dot(m_Dragon.forward,
                (_PlayerController.transform.position - m_Dragon.position).normalized) >= 0f
                ? EPlayerPoint.Forward
                : EPlayerPoint.Back;

        private bool CheckDis() =>
            (_PlayerController.transform.position - owner.transform.position).sqrMagnitude <= m_DisToCondition;
    }
}