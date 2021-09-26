using System;
using Script.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Script.Dragon
{
    [System.Flags]
    public enum EDragonPhaseFlag
    {
        Phase1 = 1 << 0,
        Phase2 = 1 << 1,
        Angry = 1 << 2,
        Exhausted = 1 << 3,
        Dead = 1 << 4
    }
    
    public class DragonController : MonoSingleton<DragonController>
    {
        private StateMachine<DragonController> m_DragonStateMachine;
        public LayerMask playerMask;
        public EDragonPhaseFlag currentPhaseFlag = EDragonPhaseFlag.Phase1;
        public NavMeshAgent nav;
        public DragonStatus dragonStat;
        public Transform player;

        public delegate void StopWaitAnim();

        public StopWaitAnim AttackWaitCoru;

        private void Awake()
        {
            dragonStat = new DragonStatus();
            nav = GetComponent<NavMeshAgent>();
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Start()
        {
            var anim = GetComponent<Animator>();
            m_DragonStateMachine = new StateMachine<DragonController>(anim, this, new S_Dragon_Movement());
            m_DragonStateMachine.SetState(new G_Dragon_Attack());
            m_DragonStateMachine.SetState(new G_Dragon_Tail());
            StartCoroutine(DragonPhaseManager.Instance.DragonAngry());
            StartCoroutine(dragonStat.DragonRecovery());
        }

        private void Update()
        {
            m_DragonStateMachine?.Update();
        }

        private void FixedUpdate()
        {
            m_DragonStateMachine?.FixedUpdate();
        }

        public void TakeDamage(int damage)
        {
            dragonStat.Health -= damage;
        }

        public void Stun()
        {
            AttackWaitCoru.Invoke();
            // stun
        }
    }
}