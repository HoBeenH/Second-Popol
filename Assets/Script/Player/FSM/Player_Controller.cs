using System;
using Script.Dragon.FSM;
using Sirenix.OdinInspector;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    // 플레이어 상태 플래그
    [Flags]
    public enum EPlayerFlag
    {
        Sword = 1 << 0,
        Magic = 1 << 1,
        Parry = 1 << 2,
        FallDown = 1 << 3
    }

    public class Player_Controller : MonoSingleton<Player_Controller>
    {
        private StateMachine<Player_Controller> m_Machine;
        private Rigidbody m_Rig;

        public PlayerStatus PlayerStat { get; private set; }
        public EPlayerFlag playerFlag;

        private void Awake()
        {
            m_Rig = GetComponent<Rigidbody>();
            PlayerStat = new PlayerStatus();

            var anim = GetComponent<Animator>();
            m_Machine = new StateMachine<Player_Controller>(anim, this, new Player_Movement());
            m_Machine.SetState(new Player_WeaponChange());
            m_Machine.SetState(new Player_SwordAttack());
            m_Machine.SetState(new Player_WeaponTopDown());
            m_Machine.SetState(new Player_Parrying());
            m_Machine.SetState(new Player_Counter());
            m_Machine.SetState(new Player_Sliding());
            m_Machine.SetState(new Player_Shoot());
            m_Machine.SetState(new Player_HeavyShoot());
            m_Machine.SetState(new Player_MagicTopDown());
            m_Machine.SetState(new Player_FallDown());
            m_Machine.SetState(new Player_Dead());
        }

        private void Start()
        {
            playerFlag |= EPlayerFlag.Sword;
        }

        private void Update()
        {
            m_Machine?.Update();
        }

        public void TakeDamage(int damage, Vector3 dir)
        {
            if (playerFlag.HasFlag(EPlayerFlag.FallDown))
            {
                return;
            }

            if (playerFlag.HasFlag(EPlayerFlag.Parry) &&
                !_DragonController.currentStateFlag.HasFlag(EDragonFlag.CantParry))
            {
                m_Machine.ChangeState(typeof(Player_Counter));
                _DragonController.Stun();
                return;
            }

            PlayerStat.health -= damage;
            UseFallDown(dir,5f);

            if (PlayerStat.health <= 0)
            {
                m_Machine.ChangeState(typeof(Player_Dead));
            }
        }

        public void UseFallDown(Vector3 dir, float force)
        {
            if (playerFlag.HasFlag(EPlayerFlag.FallDown))
                return;
            m_Machine.ChangeState(typeof(Player_FallDown));
            m_Rig.AddForce(dir * force, ForceMode.Impulse);
        }

        [Button]
        public void Dead()
        {
            m_Machine.ChangeState(typeof(Player_Dead));
        }
    }
}