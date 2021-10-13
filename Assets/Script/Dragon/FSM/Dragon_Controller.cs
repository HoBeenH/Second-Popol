using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Script.Dragon.FSM
{
    // 드래곤 현재 상태를 나타내는 플래그
    [Flags]
    public enum EDragonFlag
    {
        Default = 1 << 0,
        CantParry = 1 << 1,
        Fly = 1 << 2,
        Dead = 1 << 3
    }

    public class Dragon_Controller : MonoSingleton<Dragon_Controller>
    {
        private StateMachine<Dragon_Controller> m_StateMachine;
        [HideInInspector] public NavMeshAgent nav;
        [SerializeField] public EDragonFlag currentStateFlag = EDragonFlag.Default;
        [HideInInspector] public LayerMask playerMask = 1 << 10;
        public DragonStatus DragonStat { get; private set; }

        private void Awake()
        {
            DragonStat = new DragonStatus();
            nav = GetComponent<NavMeshAgent>();
            var anim = GetComponent<Animator>();
            m_StateMachine = new StateMachine<Dragon_Controller>(anim, this, new Dragon_Movement());
            m_StateMachine.SetState(new Dragon_Bite());
            m_StateMachine.SetState(new Dragon_Tail());
            m_StateMachine.SetState(new Dragon_Breath());
            m_StateMachine.SetState(new Dragon_FlyAttack());
            m_StateMachine.SetState(new Dragon_Stun());
            m_StateMachine.SetState(new Dragon_Dead());
            m_StateMachine.SetState(new Dragon_Ultimate());
            m_StateMachine.SetState(new Dragon_FlyBreath());
        }

        private void Update()
        {
            m_StateMachine?.Update();
        }

        public void TakeDamage(int damage)
        {
            DragonStat.health -= damage;
            if (DragonStat.health <= 0f)
            {
                m_StateMachine.ChangeState(typeof(Dragon_Dead));
                return;
            }

            Debug.Log(DragonStat.health);
        }

        #region DeBug

        [Button]
        public void Stun()
        {
            m_StateMachine.ChangeState(typeof(Dragon_Stun));
        }
        [Button]
        public void Dead()
        {
            m_StateMachine.ChangeState(typeof(Dragon_Dead));
        }
        [Button]
        public void FlyBreath()
        {
            m_StateMachine.ChangeState(typeof(Dragon_FlyBreath));
        }
        [Button]
        public void FlyAttack()
        {
            m_StateMachine.ChangeState(typeof(Dragon_FlyAttack));
        }
        [Button]
        public void Ultimate()
        {
            m_StateMachine.ChangeState(typeof(Dragon_Ultimate));
        }

        #endregion
    }
}