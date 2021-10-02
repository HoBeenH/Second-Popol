using System;
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
        Frozen = 1 << 4,
        SpeedUp = 1 << 5,
        DamageUp = 1 << 6,
        HealthUp = 1 << 7,
        Dead = 1 << 8
    }

    public class DragonController : MonoSingleton<DragonController>
    {
        private StateMachine<DragonController> m_DragonStateMachine;

        [HideInInspector] public NavMeshAgent nav;
        [HideInInspector] public bool bReadyAttack = true;
        [HideInInspector] public bool bReadyTail = true;
        [HideInInspector] public bool bReadyFlyAttack = true;
        [HideInInspector] public bool bReadyBreath = true;
        [HideInInspector] public bool bReadyPattern = true;
        [SerializeField] public EDragonPhaseFlag currentPhaseFlag = EDragonPhaseFlag.Phase1;
        public LayerMask playerMask;
        public DragonInfo DragonStat { get; private set; }
        public event Action StopAnim;

        private void Awake()
        {
            DragonStat = new DragonInfo();
            nav = GetComponent<NavMeshAgent>();
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
            m_DragonStateMachine.SetState(new G_Dragon_Frozen());
            m_DragonStateMachine.SetState(new S_Dragon_Dead());
            m_DragonStateMachine.SetState(new G_Dragon_Pattern());
            StartCoroutine(DragonStat.DragonRecovery());
        }

        private void Update()
        {
            m_DragonStateMachine?.Update();

            if (Input .GetKey(KeyCode.Tab))
            {
                m_DragonStateMachine?.ChangeState<G_Dragon_Pattern>();
            }   
            if (Input .GetKey(KeyCode.LeftAlt))
            {
                m_DragonStateMachine?.ChangeState<G_Dragon_FlyAttack>();
            }
        }

        private void FixedUpdate() => m_DragonStateMachine?.FixedUpdate();

        public void TakeDamage(int damage, ECurrentWeaponFlag weapon)
        {
            var _damage = damage;
            if (weapon.HasFlag(ECurrentWeaponFlag.Magic))
            {
                _damage -= DragonStat.magicDefence;
                if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase1))
                {
                    _DragonPhaseManager.HitCheck(ECurrentWeaponFlag.Magic);
                }
            }
            else if (weapon.HasFlag(ECurrentWeaponFlag.Sword))
            {
                _damage -= DragonStat.defence;
                if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase1))
                {
                    _DragonPhaseManager.HitCheck(ECurrentWeaponFlag.Sword);
                }
            }

            DragonStat.Health -= _damage;
            if (DragonStat.Health <= 0f)
            {
                m_DragonStateMachine.ChangeState<S_Dragon_Dead>();
                return;
            }
            Debug.Log(DragonStat.Health);

        }

        [Button]
        public void Stun()
        {
            StopAnim?.Invoke();
            m_DragonStateMachine.ChangeState<S_Dragon_Stun>();
        }

        public void Frozen()
        {
            StopAnim?.Invoke();
            m_DragonStateMachine.ChangeState<G_Dragon_Frozen>();
        }
    }
}