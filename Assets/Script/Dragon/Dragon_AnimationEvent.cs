using UnityEngine;

namespace Script.Dragon
{
    public class Dragon_AnimationEvent : MonoBehaviour
    {
        [SerializeField] private Collider m_AttackCol;
        private Collider[] m_Tails;

        private void Awake()
        {
            var temp = GetComponentInChildren<Dragon_TailCollider>();
            m_Tails = temp.GetComponentsInChildren<Collider>();
        }

        public void BiteCol(int trueOrFalse)
        {
            m_AttackCol.enabled = trueOrFalse switch
            {
                0 => false,
                1 => true,
                _ => m_AttackCol.enabled
            };
        }

        public void TailCol(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    foreach (var tail in m_Tails)
                    {
                        tail.enabled = false;
                    }

                    break;
                case 1:
                    foreach (var tail in m_Tails)
                    {
                        tail.enabled = true;
                    }
                    break;
            }
        }
    }
}