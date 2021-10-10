using Cinemachine;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_Weapon : MonoBehaviour
    {
        private CinemachineImpulseSource m_Source;
        private Collider m_Collider;

        private void Awake()
        {
            m_Collider = GetComponent<BoxCollider>();
            m_Source = Camera.main.gameObject.GetComponent<CinemachineImpulseSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                _DragonController.TakeDamage(_PlayerController.PlayerStat.damage, _PlayerController.playerFlag);
                m_Source.GenerateImpulse();
                m_Collider.enabled = false;
            }
        }
    }
}