using System;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    [Flags]
    public enum ECurrentWeaponFlag
    {
        Sword = 1 << 0,
        Magic = 1 << 1,
        Parry = 1 << 2,
        FallDown = 1 << 3
    }

    public class PlayerController : MonoSingleton<PlayerController>
    {
        private StateMachine<PlayerController> m_PlayerStateMachine;
        private Rigidbody m_Rig;

        public PlayerStatus PlayerStat { get; private set; }
        public ECurrentWeaponFlag currentWeaponFlag;
        public Action<Vector3, float> useFallDown;
        public LayerMask dragon = 1 << 11;
        [HideInInspector] public bool bTopDownCoolTime = true;

        private void Awake()
        {
            m_Rig = GetComponent<Rigidbody>();
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
        }

        private void Start()
        {
            useFallDown += (v, f) =>
            {
                if (currentWeaponFlag.HasFlag(ECurrentWeaponFlag.FallDown))
                    return;
                m_PlayerStateMachine.ChangeState<S_Player_FallDown>();
                m_Rig.AddForce(v * f, ForceMode.Impulse);
            };
            currentWeaponFlag |= ECurrentWeaponFlag.Sword;
        }

        private void Update()
        {
            m_PlayerStateMachine?.Update();
            Test();
        }

        private void FixedUpdate() => m_PlayerStateMachine?.FixedUpdate();

        public void TakeDamage(int damage, Vector3? dir = null)
        {
            if (currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Parry) &&
                !_DragonController.currentPhaseFlag.HasFlag(EDragonPhaseFlag.CantParry))
            {
                m_PlayerStateMachine.ChangeState<W_Player_Skill>();
                _DragonController.Stun();
                return;
            }

            PlayerStat.Health -= damage;
            if (PlayerStat.Health <= 0)
            {
                // 죽음
                return;
            }

            if (dir != null)
            {
                useFallDown((Vector3)dir, 5f);
            }
            Debug.Log($"Take Damage {PlayerStat.Health}");
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