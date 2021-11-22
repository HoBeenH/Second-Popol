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
    }

    public class Dragon_Controller : MonoSingleton<Dragon_Controller>
    {
        private StateMachine<Dragon_Controller> m_Machine;

        [HideInInspector] public EDragonFlag stateFlag = EDragonFlag.Default;
        [HideInInspector] public LayerMask playerMask = 1 << 10;
        [HideInInspector] public NavMeshAgent nav;
        public DragonStatus Stat { get; private set; }

        private void Awake()
        {
            Cam.Instance.end += Init;
        }

        public void Init()
        {
            Stat = new DragonStatus();
            nav = GetComponent<NavMeshAgent>();
            m_Machine = new StateMachine<Dragon_Controller>(GetComponent<Animator>(), this, new Dragon_Movement());
            m_Machine.SetState(new Dragon_Bite());
            m_Machine.SetState(new Dragon_Tail());
            m_Machine.SetState(new Dragon_Breath());
            m_Machine.SetState(new Dragon_FlyAttack());
            m_Machine.SetState(new Dragon_Stun());
            m_Machine.SetState(new Dragon_Dead());
            m_Machine.SetState(new Dragon_Ultimate());
            m_Machine.SetState(new Dragon_FlyBreath());
        }

        private void Update() => m_Machine?.OnUpdate();

        public void TakeDamage(int damage)
        {
            Stat.health -= damage;
            if (Stat.health <= 0f)
            {
                m_Machine.ChangeState(typeof(Dragon_Dead));
            }
        }

        public void Stun() => m_Machine.ChangeState(typeof(Dragon_Stun));

        public void Debug()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                m_Machine.ChangeState(typeof(Dragon_Dead));
            }
        }
    }
}