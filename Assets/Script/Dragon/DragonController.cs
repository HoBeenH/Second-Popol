using System;
using Script.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Script.Dragon
{
    public class DragonController : MonoSingleton<DragonController>
    {
        private StateMachine<DragonController> m_DragonStateMachine;
        public NavMeshAgent nav;
        public DragonStatus dragonStat;
        public Transform player;

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
    }
}