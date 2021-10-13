using Script.Player.FSM;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_AnimationEvent : MonoBehaviour
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
            if (_PlayerController.playerFlag.HasFlag(EPlayerFlag.Sword))
            {
                m_ObjWeapon.SetActive(false);
                _PlayerController.playerFlag |= EPlayerFlag.Magic;
                _PlayerController.playerFlag &= ~EPlayerFlag.Sword;
                m_Animator.SetBool(m_NowWeapon, false);
            }
            else if (_PlayerController.playerFlag.HasFlag(EPlayerFlag.Magic))
            {
                m_ObjWeapon.SetActive(true);
                _PlayerController.playerFlag |= EPlayerFlag.Sword;
                _PlayerController.playerFlag &= ~EPlayerFlag.Magic;
                m_Animator.SetBool(m_NowWeapon, true);
            }
        }

        public void WeaponCollider(int zeroIsFalse)
        {
            switch (zeroIsFalse)
            {
                case 0:
                    m_WeaponCollider.enabled = false;
                    break;
                case 1:
                    m_WeaponCollider.enabled = true;
                    break;
                default:
                    Debug.LogError($"{zeroIsFalse} Is Unknown Num");
                    break;
            }
        }

        #endregion
    }
}