using System;
using System.Collections;
using Script.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon
{
    [Flags]
    public enum EDragonPhaseFlag
    {
        Phase1 = 1 << 0,
        Phase2 = 1 << 1,
        CantParry = 1 << 2,
        Fly = 1 << 3,
        SpeedUp = 1 << 4,
        DamageUp = 1 << 5,
        HealthUp = 1 << 6,
        Dead = 1 << 7
    }

    public class DragonController : MonoSingleton<DragonController>
    {
        private StateMachine<DragonController> m_StateMachine;

        [HideInInspector] public NavMeshAgent nav;

        [SerializeField] public EDragonPhaseFlag currentStateFlag = EDragonPhaseFlag.Phase1;
        public LayerMask playerMask = 1<< 10;
        public DragonInfo DragonStat { get; private set; }
        public event Action StopAnim;
        public bool bReadyAttack = true;
        public bool bReadyTail = true;
        public bool bReadyFlyAttack = true;
        public bool bReadyBreath = true;
        public bool bReadyPattern = true;

        private void Awake()
        {
            DragonStat = new DragonInfo();
            nav = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            var anim = GetComponent<Animator>();
            m_StateMachine = new StateMachine<DragonController>(anim, this, new S_Dragon_Movement());
            m_StateMachine.SetState(new G_Dragon_Attack());
            m_StateMachine.SetState(new G_Dragon_Tail());
            m_StateMachine.SetState(new G_Dragon_Breath());
            m_StateMachine.SetState(new G_Dragon_FlyAttack());
            m_StateMachine.SetState(new S_Dragon_Stun());
            m_StateMachine.SetState(new S_Dragon_Dead());
            m_StateMachine.SetState(new G_Dragon_Pattern());
        }

        private void Update()
        {
            m_StateMachine?.Update();

            if (Input .GetKey(KeyCode.Tab))
            {
                m_StateMachine?.ChangeState<G_Dragon_Pattern>();
            }   
            if (Input .GetKey(KeyCode.LeftAlt))
            {
                m_StateMachine?.ChangeState<G_Dragon_FlyAttack>();
            }
        }


        private void FixedUpdate() => m_StateMachine?.FixedUpdate();

        public void TakeDamage(int damage, EPlayerFlag weapon)
        {
            var _damage = damage;
            if (weapon.HasFlag(EPlayerFlag.Magic))
            {
                _damage -= DragonStat.magicDefence;
                if (currentStateFlag.HasFlag(EDragonPhaseFlag.Phase1))
                {
                    _DragonPhaseManager.HitCheck(EPlayerFlag.Magic);
                }
            }
            else if (weapon.HasFlag(EPlayerFlag.Sword))
            {
                _damage -= DragonStat.defence;
                if (currentStateFlag.HasFlag(EDragonPhaseFlag.Phase1))
                {
                    _DragonPhaseManager.HitCheck(EPlayerFlag.Sword);
                }
            }
            DragonStat.health -= _damage;
            if (DragonStat.health <= 0f)
            {
                m_StateMachine.ChangeState<S_Dragon_Dead>();
                return;
            }
            Debug.Log(DragonStat.health);

        }

        [Button]
        public void Stun()
        {
            StopAnim?.Invoke();
            StopAnim = null;
            m_StateMachine.ChangeState<S_Dragon_Stun>();
        }
    }
}