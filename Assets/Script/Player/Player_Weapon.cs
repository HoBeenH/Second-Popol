using System;
using Script.Dragon;
using UnityEngine;

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
                DragonController.Instance.TakeDamage(PlayerController.Instance.PlayerStat.damage,
                    PlayerController.Instance.currentWeaponFlag);
                m_Collider.enabled = false;
            }
        }
    }
}