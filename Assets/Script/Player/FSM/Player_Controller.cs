using System;
using UnityEngine;
using Script.Dragon.FSM;
using static Script.Facade;

namespace Script.Player.FSM
{
    // 플레이어의 상태를 표시하는 플래그
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

        public PlayerStatus Stat { get; private set; }
        [HideInInspector] public EPlayerFlag playerFlag;

        private void Awake()
        {
            m_Rig = GetComponent<Rigidbody>();
            Stat = new PlayerStatus();

            m_Machine = new StateMachine<Player_Controller>(GetComponent<Animator>(), this, new Player_Movement());
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

            playerFlag |= EPlayerFlag.Sword;
        }

        private void Update()
        {
            DeBugInPlay();
            m_Machine?.OnUpdate();
        }

        public void TakeDamage(int damage, Vector3 dir)
        {
            if (playerFlag.HasFlag(EPlayerFlag.Parry) && !_DragonController.stateFlag.HasFlag(EDragonFlag.CantParry))
            {
                m_Machine.ChangeState(typeof(Player_Counter));
                _DragonController.Stun();
                return;
            }

            Stat.health -= damage;
            UseFallDown(dir, 5f);

            if (Stat.health <= 0)
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

        public void DeBugInPlay()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                m_Machine.ChangeState(typeof(Player_Dead));
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                var _dir = transform.position - _DragonController.transform.position;
                UseFallDown(_dir.normalized, 5f);
            }
        }
    }
}