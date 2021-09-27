using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Script.Dragon
{
    [Flags]
    public enum EDragonPhaseFlag
    {
        Phase1 = 1 << 0,
        Phase2 = 1 << 1,
        Angry = 1 << 2,
        Exhausted = 1 << 3,
        CantParry = 1 << 4,
        Dead = 1 << 5
    }

    public class DragonController : MonoSingleton<DragonController>
    {
        private StateMachine<DragonController> m_DragonStateMachine;

        [HideInInspector] public NavMeshAgent nav;
        [HideInInspector] public Transform player;
        [HideInInspector] public bool bReadyAttack = true;
        [HideInInspector] public bool bReadyTail = true;
        [HideInInspector] public bool bReadyFlyAttack = true;
        [HideInInspector] public bool bReadyBreath = true;
        [SerializeField] public EDragonPhaseFlag currentPhaseFlag = EDragonPhaseFlag.Phase1;
        public GameObject headObj;
        public LayerMask playerMask;
        public DragonStatus dragonStat;
        public event Action StopAnim;

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
            m_DragonStateMachine.SetState(new G_Dragon_Breath());
            m_DragonStateMachine.SetState(new G_Dragon_FlyAttack());
            m_DragonStateMachine.SetState(new S_Dragon_Stun());
            StartCoroutine(DragonPhaseManager.Instance.DragonAngry());
            StartCoroutine(dragonStat.DragonRecovery());
        }

        private void Update()
        {
            m_DragonStateMachine?.Update();
        }

        private void FixedUpdate() => m_DragonStateMachine?.FixedUpdate();

        public void TakeDamage(int damage) => dragonStat.Health -= damage;

        [Button]
        public void Stun()
        {
            StopAnim?.Invoke();
            m_DragonStateMachine.ChangeState<S_Dragon_Stun>();
        }
    }
}