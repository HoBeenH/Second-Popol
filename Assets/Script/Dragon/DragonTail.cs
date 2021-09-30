using Script.Player;
using UnityEngine;

namespace Script.Dragon
{
    public class DragonTail : MonoBehaviour
    {
        public Collider[] m_Tails;

        private void Awake()
        {
            m_Tails = GetComponentsInChildren<Collider>();
            Debug.Log(m_Tails.Length);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var _dir = (other.transform.position - transform.position).normalized;
                PlayerController.Instance.TakeDamage(DragonController.Instance.DragonStat.damage,_dir);
                foreach (var tail in m_Tails)
                {
                    tail.enabled = false;
                }
            }
        }
    }
}
