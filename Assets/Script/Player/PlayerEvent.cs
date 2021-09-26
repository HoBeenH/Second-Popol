using System;
using UnityEngine;
using UnityEngine.Events;

namespace Script.Player
{
    public class PlayerEvent : MonoBehaviour
    {
        private readonly int m_NowWeapon = Animator.StringToHash("NowWeapon");
        private GameObject m_ObjWeapon;
        private Animator m_Animator;
        
        #region Animation Event

        private void Awake()
        {
            var _findWeapon = GetComponentsInChildren<Transform>();
            foreach (var _transform in _findWeapon)
            {
                if (_transform.name == "Sword_main_equip01")
                {
                    m_ObjWeapon = _transform.gameObject;
                    break;
                }
            }

            m_Animator = GetComponent<Animator>();
        }

        public void WeaponAnimEvent()
        {
            if (PlayerController.Instance.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Sword))
            {
                m_ObjWeapon.SetActive(false);
                PlayerController.Instance.currentWeaponFlag |= ECurrentWeaponFlag.Magic;
                PlayerController.Instance.currentWeaponFlag &= ~ECurrentWeaponFlag.Sword;
                m_Animator.SetBool(m_NowWeapon, false);
            }
            else if (PlayerController.Instance.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Magic))
            {
                m_ObjWeapon.SetActive(true);
                PlayerController.Instance.currentWeaponFlag |= ECurrentWeaponFlag.Sword;
                PlayerController.Instance.currentWeaponFlag &= ~ECurrentWeaponFlag.Magic;
                m_Animator.SetBool(m_NowWeapon, true);
            }
        }

        #endregion
    }
}
