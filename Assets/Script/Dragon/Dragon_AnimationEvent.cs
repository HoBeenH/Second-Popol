using System;
using Script.Player;
using UnityEngine;

namespace Script.Dragon
{
    public class Dragon_AnimationEvent : MonoBehaviour
    {
        public Collider m_AttackCol;
        public Collider[] tails;

        private void Awake()
        {
            var temp = GetComponentInChildren<Dragon_TailCollider>();
            tails = temp.GetComponentsInChildren<Collider>();
        }

        public void BiteCol(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    m_AttackCol.enabled = false;
                    break;
                case 1:
                    m_AttackCol.enabled = true;
                    break;
            }
        }

        public void TailCol(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    foreach (var tail in tails)
                    {
                        tail.enabled = false;
                    }

                    break;
                case 1:
                    foreach (var tail in tails)
                    {
                        tail.enabled = true;
                    }
                    break;
            }
        }
    }
}