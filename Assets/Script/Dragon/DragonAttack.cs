using Script.Player;
using UnityEngine;

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
                var _dir = (other.transform.position - transform.position).normalized;
                PlayerController.Instance.TakeDamage(DragonController.Instance.DragonStat.damage, _dir);
                m_Col.enabled = false;
            }
        }
    }
}
