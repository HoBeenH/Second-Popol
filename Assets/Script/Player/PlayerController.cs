using System;
using System.Collections;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
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

    public class PlayerController : MonoSingleton<PlayerController>
    {
        private StateMachine<PlayerController> m_Machine;
        public Rigidbody m_Rig;

        public PlayerStatus PlayerStat { get; private set; }
        public EPlayerFlag playerFlag;
        public Action<Vector3, float> useFallDown;

        private void Awake()
        {
            m_Rig = GetComponent<Rigidbody>();
            PlayerStat = new PlayerStatus();

            var anim = GetComponent<Animator>();
            m_Machine = new StateMachine<PlayerController>(anim, this, new S_Player_Movement());
            m_Machine.SetState(new S_Player_ChangeWeapon());
            m_Machine.SetState(new W_Player_Attack());
            m_Machine.SetState(new W_Player_TopDown());
            m_Machine.SetState(new W_Player_Parrying());
            m_Machine.SetState(new W_Player_Skill());
            m_Machine.SetState(new S_Player_Sliding());
            m_Machine.SetState(new M_Player_Shoot());
            m_Machine.SetState(new M_Player_HeavyShoot());
            m_Machine.SetState(new M_Player_TopDown());
            m_Machine.SetState(new S_Player_FallDown());
            m_Machine.SetState(new S_Player_Dead());
        }

        private void Start()
        {
            useFallDown = (v, f) =>
            {
                if (playerFlag.HasFlag(EPlayerFlag.FallDown))
                    return;

                m_Machine.ChangeState(typeof(S_Player_FallDown));
                m_Rig.AddForce(v * f, ForceMode.Impulse);
            };
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
                !_DragonController.currentStateFlag.HasFlag(EDragonPhaseFlag.CantParry))
            {
                m_Machine.ChangeState(typeof(W_Player_Skill));
                _DragonController.Stun();
                return;
            }

            PlayerStat.health -= damage;
            useFallDown.Invoke(dir,5f);

            Debug.Log(PlayerStat.health);
            if (PlayerStat.health <= 0)
            {
                foreach (var pattern in m_Machine.cancel)
                {
                    StopCoroutine(pattern);
                }

                m_Machine.ChangeState(typeof(S_Player_Dead));
            }
        }
    }
}