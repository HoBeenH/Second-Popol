using System;
using Script.Player;
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
        CantParry = 1 << 2,
        Fly = 1 << 3,
        Frozen = 1 << 4,
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
        public LayerMask playerMask;
        public DragonStatus DragonStat { get; private set; }
        public event Action StopAnim;

        private void Awake()
        {
            DragonStat = new DragonStatus();
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
            m_DragonStateMachine.SetState(new S_Dragon_Dead());
            StartCoroutine(DragonStat.DragonRecovery());
        }

        private void Update()
        {
            m_DragonStateMachine?.Update();
        }

        private void FixedUpdate() => m_DragonStateMachine?.FixedUpdate();

        public void TakeDamage(int damage, ECurrentWeaponFlag weapon)
        {
            var _damage = damage;
            if (weapon.HasFlag(ECurrentWeaponFlag.Parry))
            {
                Stun();
                return;
            }
            if (weapon.HasFlag(ECurrentWeaponFlag.Magic))
            {
                _damage -= DragonStat.magicDefence;
            }

            if (weapon.HasFlag(ECurrentWeaponFlag.Sword))
            {
                _damage -= DragonStat.defence;
            }
            DragonStat.Health -= _damage;
            if (DragonStat.Health <= 0f)
            {
                m_DragonStateMachine.ChangeState<S_Dragon_Dead>();
            }
            if (currentPhaseFlag.HasFlag(EDragonPhaseFlag.Phase1))
            {
                DragonPhaseManager.Instance.HitCheck(weapon);
            }
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
            currentPhaseFlag |= EDragonPhaseFlag.Frozen;
            m_DragonStateMachine.ChangeState<S_Dragon_Movement>();
            nav.SetDestination(transform.position);
        }    
        public void DeFrozen()
        {
            currentPhaseFlag &= ~EDragonPhaseFlag.Frozen;
        }
    }
}