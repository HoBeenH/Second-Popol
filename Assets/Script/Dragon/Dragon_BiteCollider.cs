using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_BiteCollider : MonoBehaviour
    {
        private Collider m_Col;

        private void Awake() => m_Col = GetComponent<Collider>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _PlayerController.TakeDamage(_DragonController.Stat.damage,
                    (_PlayerController.transform.position - _DragonController.transform.position).normalized);
                m_Col.enabled = false;
            }
        }
    }
}