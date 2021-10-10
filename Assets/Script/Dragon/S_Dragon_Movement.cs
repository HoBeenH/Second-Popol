using System;
using System.Collections;
using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public enum EPlayerPoint
    {
        Forward,
        Back,
    }

    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private Transform m_Dragon;
        private readonly Type m_Attack = typeof(G_Dragon_Attack);
        private readonly Type m_Breath = typeof(G_Dragon_Breath);
        private readonly Type m_Phase2 = typeof(G_Dragon_Phase2);
        private readonly Type m_Tail = typeof(G_Dragon_Tail);
        private readonly Type m_FlyAttack = typeof(G_Dragon_FlyAttack);
        private bool m_BHasPattern;

        protected override void Init()
        {
            m_Dragon = owner.GetComponent<Transform>();
        }

        public override void OnStateEnter()
        {
            m_BHasPattern = machine.nextPattern.Count != 0;
            if (!m_BHasPattern)
            {
                owner.StartCoroutine(ChoicePattern());
            }
            else
            {
                machine.ChangeState(machine.nextPattern.Dequeue());
            }
        }

        public override void OnStateUpdate()
        {
            if (!m_BHasPattern)
            {
                machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude,
                    owner.DragonStat.moveAnimDamp, Time.deltaTime);
                owner.nav.SetDestination(_PlayerController.transform.position);
            }
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
            machine.animator.SetFloat(m_MovementFloatHash, 0f);
        }

        private IEnumerator ChoicePattern()
        {
            while (!CheckDis())
            {
                yield return null;
            }

            machine.nextPattern.Enqueue(PlayerPoint() == EPlayerPoint.Forward ? m_Attack : m_Tail);
            var _length = Random.Range(1, 3);
            for (var k = 0; k < _length; k++)
            {
                var i = Random.Range(0, 4);
                switch (i)
                {
                    case 0:
                        machine.nextPattern.Enqueue(m_Attack);
                        break;
                    case 1:
                        machine.nextPattern.Enqueue(m_Tail);
                        break;
                    case 3:
                        machine.nextPattern.Enqueue(m_Breath);
                        break;
                }
            }

            if (owner.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2SetUp) &&
                _SkillManager.FindSkill(m_Phase2).BIsActive)
            {
                machine.nextPattern.Enqueue(typeof(G_Dragon_Phase2));
                _SkillManager.FindSkill(m_Phase2).BIsActive = false;
            }
            else
            {
                machine.nextPattern.Enqueue(m_FlyAttack);
            }

            machine.ChangeState(machine.nextPattern.Dequeue());
        }


        private EPlayerPoint PlayerPoint() =>
            Vector3.Dot(m_Dragon.forward,
                (_PlayerController.transform.position - m_Dragon.position).normalized) >= 0f
                ? EPlayerPoint.Forward
                : EPlayerPoint.Back;

        private bool CheckDis() =>
            (_PlayerController.transform.position - m_Dragon.position).sqrMagnitude <=
            Mathf.Pow(owner.nav.stoppingDistance, 2);
    }
}