using System;
using Script.Player;
using UnityEngine;

namespace Script.Dragon
{
    public class DragonEvent : MonoBehaviour
    {
        public Collider m_AttackCol;
        public Collider[] tails;

        private void Awake()
        {
            var temp = GetComponentInChildren<DragonTail>();
            tails = temp.GetComponentsInChildren<Collider>();
        }

        public void AttackCol(int trueOrFalse)
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