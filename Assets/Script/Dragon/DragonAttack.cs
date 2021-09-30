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
                PlayerController.Instance.TakeDamage(DragonController.Instance.DragonStat.damage);
                var temp = (other.transform.position - transform.position).normalized;
                Debug.Log(temp);
                PlayerController.Instance.useFallDown.Invoke(temp,5f);
                m_Col.enabled = false;
            }
        }
    }
}
