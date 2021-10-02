using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class PlayerEvent : MonoBehaviour
    {
        private readonly int m_NowWeapon = Animator.StringToHash("NowWeapon");
        private Collider m_WeaponCollider;
        private GameObject m_ObjWeapon;
        private Animator m_Animator;

        #region Animation Event

        private void Awake()
        {
            var _findWeapon = GetComponentsInChildren<Transform>();
            foreach (var _transform in _findWeapon)
            {
                if (_transform.name == "Weapon_r")
                {
                    m_ObjWeapon = _transform.gameObject;
                    m_WeaponCollider = m_ObjWeapon.GetComponent<BoxCollider>();
                    break;
                }
            }

            m_Animator = GetComponent<Animator>();
        }

        public void WeaponAnimEvent()
        {
            if (_PlayerController.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Sword))
            {
                m_ObjWeapon.SetActive(false);
                _PlayerController.currentWeaponFlag |= ECurrentWeaponFlag.Magic;
                _PlayerController.currentWeaponFlag &= ~ECurrentWeaponFlag.Sword;
                m_Animator.SetBool(m_NowWeapon, false);
            }
            else if (_PlayerController.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Magic))
            {
                m_ObjWeapon.SetActive(true);
                _PlayerController.currentWeaponFlag |= ECurrentWeaponFlag.Sword;
                _PlayerController.currentWeaponFlag &= ~ECurrentWeaponFlag.Magic;
                m_Animator.SetBool(m_NowWeapon, true);
            }
        }

        public void WeaponCollider(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    m_WeaponCollider.enabled = false;
                    break;
                case 1:
                    m_WeaponCollider.enabled = true;
                    break;
                default:
                    Debug.LogError($"{trueOrFalse} Is Unknown Num");
                    break;
            }
        }

        #endregion
    }
}