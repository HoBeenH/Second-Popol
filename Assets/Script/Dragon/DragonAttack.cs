using Script.Player;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class DragonAttack : MonoBehaviour
    {
        private Collider m_Col;

        private void Awake()
        {
            m_Col = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (other.transform.position - transform.position).normalized);
                m_Col.enabled = false;
            }
        }
    }
}