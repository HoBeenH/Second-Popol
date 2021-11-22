using Script.Player.FSM;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class Player_AnimationEvent : MonoBehaviour
    {
        private readonly int m_Hash = Animator.StringToHash("NowWeapon");
        private Collider m_Col;
        [SerializeField] private GameObject m_Obj;
        private Animator m_Anim;

        #region Animation Event

        private void Awake()
        {
            m_Anim = GetComponent<Animator>();
            m_Col = m_Obj.GetComponent<BoxCollider>();
        }

        public void WeaponAnimEvent()
        {
            if (_PlayerController.playerFlag.HasFlag(EPlayerFlag.Sword))
            {
                m_Obj.SetActive(false);
                _PlayerController.playerFlag |= EPlayerFlag.Magic;
                _PlayerController.playerFlag &= ~EPlayerFlag.Sword;
                m_Anim.SetBool(m_Hash, false);
            }
            else if (_PlayerController.playerFlag.HasFlag(EPlayerFlag.Magic))
            {
                m_Obj.SetActive(true);
                _PlayerController.playerFlag |= EPlayerFlag.Sword;
                _PlayerController.playerFlag &= ~EPlayerFlag.Magic;
                m_Anim.SetBool(m_Hash, true);
            }
        }

        public void WeaponCollider(int zeroIsFalse) =>
            m_Col.enabled = zeroIsFalse switch
            {
                0 => false,
                1 => true,
                _ => false
            };

        #endregion
    }
}