using System;
using System.Collections.Generic;
using Script.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon
{
    // 드래곤 현재 상태를 나타내는 플래그
    [Flags]
    public enum EDragonPhaseFlag
    {
        Phase1 = 1 << 0,
        Phase2SetUp = 1 << 1,
        Phase2 = 1 << 2,
        CantParry = 1 << 3,
        Fly = 1 << 4,
        Dead = 1 << 5
    }

    public class Dragon_Controller : MonoSingleton<Dragon_Controller>
    {
        private StateMachine<Dragon_Controller> m_StateMachine;

        [HideInInspector] public NavMeshAgent nav;

        [SerializeField] public EDragonPhaseFlag currentStateFlag = EDragonPhaseFlag.Phase1;
        public LayerMask playerMask = 1 << 10;
        public DragonStatus DragonStat { get; private set; }

        private void Awake()
        {
            DragonStat = new DragonStatus();
            nav = GetComponent<NavMeshAgent>();
            var anim = GetComponent<Animator>();
            m_StateMachine = new StateMachine<Dragon_Controller>(anim, this, new S_Dragon_Movement());
            m_StateMachine.SetState(new G_Dragon_Bite());
            m_StateMachine.SetState(new G_Dragon_Tail());
            m_StateMachine.SetState(new G_Dragon_Breath());
            m_StateMachine.SetState(new G_Dragon_FlyAttack());
            m_StateMachine.SetState(new S_Dragon_Stun());
            m_StateMachine.SetState(new S_Dragon_Dead());
            m_StateMachine.SetState(new G_Dragon_Ultimate());
            m_StateMachine.SetState(new G_Dragon_FlyBreath());
        }

        private void Update()
        {
            m_StateMachine?.Update();
        }

        public void TakeDamage(int damage, EPlayerFlag weapon)
        {
            if (weapon.HasFlag(EPlayerFlag.Magic))
            {
                damage -= DragonStat.magicDefence;
            }
            else if (weapon.HasFlag(EPlayerFlag.Sword))
            {
                damage -= DragonStat.defence;
            }

            if (currentStateFlag.HasFlag(EDragonPhaseFlag.Phase1))
            {
                _DragonPhaseManager.HitCheck(weapon);
            }

            DragonStat.health -= damage;
            if (DragonStat.health <= 0f)
            {
                m_StateMachine.ChangeState(typeof(S_Dragon_Dead));
                return;
            }

            Debug.Log(DragonStat.health);
        }

        [Button]
        public void Stun()
        {
            m_StateMachine.ChangeState(typeof(S_Dragon_Stun));
        }

        [Button]
        public void Dead()
        {
            m_StateMachine.ChangeState(typeof(S_Dragon_Dead));
        }
        
        [Button]
        public void FlyBreath()
        {
            m_StateMachine.ChangeState(typeof(G_Dragon_FlyBreath));
        }
        [Button]
        public void FlyAttack()
        {
            m_StateMachine.ChangeState(typeof(G_Dragon_FlyAttack));
        }

    }
}