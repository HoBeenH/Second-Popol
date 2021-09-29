using System;
using Script.Dragon;
using UnityEngine;

namespace Script.Player
{
    [Flags]
    public enum ECurrentWeaponFlag
    {
        Sword = 1 << 0,
        Magic = 1 << 1,
        Parry = 1 << 2
    }

    public class PlayerController : MonoSingleton<PlayerController>
    {
        private StateMachine<PlayerController> m_PlayerStateMachine;

        public PlayerStatus PlayerStat { get; private set; }
        public ECurrentWeaponFlag currentWeaponFlag;
        public Action useDefaultCam;
        public Action useActionCam;
        public Action useFallDown;
        public LayerMask dragon;
        public GameObject temp;
        [HideInInspector] public bool bTopDownCoolTime = true;

        private void Awake()
        {
            useFallDown = () => m_PlayerStateMachine.ChangeState<S_Player_FallDown>();
            PlayerStat = new PlayerStatus();
            var anim = GetComponent<Animator>();
            m_PlayerStateMachine = new StateMachine<PlayerController>(anim, this, new S_Player_Movement());
            m_PlayerStateMachine.SetState(new S_Player_ChangeWeapon());
            m_PlayerStateMachine.SetState(new W_Player_Attack());
            m_PlayerStateMachine.SetState(new W_Player_TopDown());
            m_PlayerStateMachine.SetState(new W_Player_Parrying());
            m_PlayerStateMachine.SetState(new W_Player_Skill());
            m_PlayerStateMachine.SetState(new S_Player_Sliding());
            m_PlayerStateMachine.SetState(new M_Player_Shoot());
            m_PlayerStateMachine.SetState(new M_Player_HeavyShoot());
            m_PlayerStateMachine.SetState(new M_Player_TopDown());
            m_PlayerStateMachine.SetState(new S_Player_FallDown());
            currentWeaponFlag |= ECurrentWeaponFlag.Sword;
        }

        private void Update()
        {
            m_PlayerStateMachine?.Update();
            Test();
        }

        private void FixedUpdate() => m_PlayerStateMachine?.FixedUpdate();

        private void TakeDamage(int damage)
        {
            if (currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Parry) &&
                !(DragonController.Instance.currentPhaseFlag.HasFlag(EDragonPhaseFlag.CantParry)))
            {
                m_PlayerStateMachine.ChangeState<W_Player_Skill>();
                DragonController.Instance.Stun();
                return;
            }

            PlayerStat.Health -= damage;
            if (PlayerStat.Health <= 0)
            {
                // 죽음
            }
            // 피격판정
        }

        private void Test()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                PlayerStat.Health -= 20;
                Debug.Log(PlayerStat.Health);
            }

            if (Input.GetKey(KeyCode.X))
            {
                m_PlayerStateMachine.ChangeState<S_Player_FallDown>();
            }
        }
    }
}