using Cinemachine;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_Weapon : MonoBehaviour
    {
        private Collider m_Collider;

        private void Awake()
        {
            m_Collider = GetComponent<BoxCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                _DragonController.TakeDamage(_PlayerController.PlayerStat.damage, _PlayerController.playerCurrentFlag);
                _CamManager.PlayerWeapon.GenerateImpulse();
                m_Collider.enabled = false;
            }
        }
    }
}