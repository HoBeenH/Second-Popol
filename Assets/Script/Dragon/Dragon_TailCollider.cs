using Script.Player;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_TailCollider : MonoBehaviour
    {
        public Collider[] m_Tails;

        private void Awake()
        {
            m_Tails = GetComponentsInChildren<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (other.transform.position - transform.position).normalized);
                foreach (var tail in m_Tails)
                {
                    tail.enabled = false;
                }
            }
        }
    }
}