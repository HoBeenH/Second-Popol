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
                var currentWeapon = PlayerController.Instance.currentWeaponFlag;
                
                var damage = currentWeapon switch
                {
                    _ when currentWeapon.HasFlag(ECurrentWeaponFlag.Sword) => PlayerController.Instance.PlayerStat.damage,
                    _ when currentWeapon.HasFlag(ECurrentWeaponFlag.Magic) => PlayerController.Instance.PlayerStat.skillDamage,
                    _ => throw new Exception()
                };
            
                Debug.Log($"Current Weapon : {currentWeapon}\ndamage : {damage}");
                DragonController.Instance.TakeDamage(damage,currentWeapon);
                m_Collider.enabled = false;
            }
        }
    }
}