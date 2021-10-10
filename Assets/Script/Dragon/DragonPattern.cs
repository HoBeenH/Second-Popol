using UnityEngine;
using System;
using System.Collections.Generic;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public enum EPosAngle
    {
        Forward,
        Back,
        Any
    }

    public class DragonPattern : MonoSingleton<DragonPattern>
    {
        private Transform m_Dragon;
        private readonly Type m_Attack = typeof(G_Dragon_Attack);
        private readonly Type m_Breath = typeof(G_Dragon_Breath);
        private readonly Type m_Phase2 = typeof(G_Dragon_Phase2);
        private readonly Type m_Tail = typeof(G_Dragon_Tail);
        private readonly Type m_FlyAttack = typeof(G_Dragon_FlyAttack);
        private readonly Queue<Type> m_NextPattern = new Queue<Type>();
        public int patternMin = 1;
        public int patternMax = 3;

        private void Awake()
        {
            m_Dragon = _DragonController.GetComponent<Transform>();
        }

        private void Start()
        {
            GetPattern();
        }

        public Type StartPattern()
        {
            if (m_NextPattern.Count != 0)
            {
                return m_NextPattern.Dequeue();
            }
            else
            {
                GetPattern();
                return m_NextPattern.Dequeue();
            }
        }

        private void GetPattern()
        {
            m_NextPattern.Enqueue(PlayerPoint() == EPosAngle.Forward ? m_Attack : m_Tail);
            var _length = Random.Range(patternMin, patternMax);
            for (var k = 0; k < _length; k++)
            {
                var i = Random.Range(0, 4);
                switch (i)
                {
                    case 0:
                        m_NextPattern.Enqueue(m_Attack);
                        break;
                    case 1:
                        m_NextPattern.Enqueue(m_Tail);
                        break;
                    case 3:
                        m_NextPattern.Enqueue(m_Breath);
                        break;
                }
            }

            if (_DragonController.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2SetUp) &&
                _SkillManager.FindSkill(m_Phase2).BIsActive)
            {
                m_NextPattern.Enqueue(m_Phase2);
            }
            else if (_SkillManager.FindSkill(m_FlyAttack).BIsActive)
            {
                m_NextPattern.Enqueue(m_FlyAttack);
            }
        }

        private EPosAngle PlayerPoint() =>
            Vector3.Dot(m_Dragon.forward,
                (_PlayerController.transform.position - m_Dragon.position).normalized) >= 0f
                ? EPosAngle.Forward
                : EPosAngle.Back;
    }
}