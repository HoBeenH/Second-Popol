using System;
using System.Collections;
using Cinemachine;
using Script;
using Script.Player;
using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

public class SkillManager : MonoBehaviour
{
    private enum EOwner
    {
        Dragon,
        Player
    }

    private enum ESkillType
    {
        Shoot,
        Boom
    }

    private LayerMask m_Mask;
    private readonly Collider[] m_Results = new Collider[1];
    [SerializeField] private ESkillType m_Type;
    [SerializeField] private EOwner m_Owner;
    public float speed;
    public float radius;
    public bool BHasImpulse;
    public CinemachineImpulseSource source;
    public bool BHasDelay;
    public float delayTime;
    private Action<Collider> m_TriggerHandler;
    private Action m_ImpulseHandler;

    private void Awake()
    {
        switch (m_Owner)
        {
            case EOwner.Dragon:
                m_Mask = 1 << 10;
                m_TriggerHandler += DragonFire;
                break;
            case EOwner.Player:
                m_Mask = 1 << 11;
                m_TriggerHandler += PlayerSkill;
                break;
        }

        if (BHasImpulse)
        {
            source = GetComponent<CinemachineImpulseSource>();
            m_ImpulseHandler = () =>
            {
                this.source.GenerateImpulse();
            };
        }
    }

    private void OnEnable()
    {
        switch (m_Type)
        {
            case ESkillType.Shoot:
                StartCoroutine(nameof(Move));
                break;
            case ESkillType.Boom when m_Owner == EOwner.Dragon:
                CheckOverlap();
                break;
            case ESkillType.Boom when m_Owner == EOwner.Player:
                if (BHasDelay)
                {
                    StartCoroutine(BoomDelay(delayTime));
                }
                else
                {
                    CheckOverlap();
                }

                break;
            default:
                throw new Exception($"{m_Type.ToString()} or {m_Owner.ToString()} is Null");
        }
    }

    private void OnDisable()
    {
        if (m_Type == ESkillType.Shoot)
        {
            StopCoroutine(nameof(Move));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        m_TriggerHandler?.Invoke(other);
    }

    private void CheckOverlap()
    {
        m_ImpulseHandler?.Invoke();
        var _size = Physics.OverlapSphereNonAlloc(transform.position, radius, m_Results, m_Mask);
        if (_size != 0)
        {
            if (m_Owner == EOwner.Player)
            {
                _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
            }
            else
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (_PlayerController.transform.position - transform.position).normalized);
            }
        }
    }

    private IEnumerator BoomDelay(float time)
    {
        yield return new WaitForSeconds(time);
        CheckOverlap();
    }

    private IEnumerator Move()
    {
        while (true)
        {
            transform.Translate(transform.forward * Time.deltaTime * speed, Space.World);
            yield return null;
        }
    }


    private void DragonFire(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                (_PlayerController.transform.position - transform.position).normalized);
            DragonSpawnEx();

            m_ImpulseHandler?.Invoke();
        }
        else if (other.CompareTag("Ground"))
        {
            CheckOverlap();
            DragonSpawnEx();
        }
    }

    private void DragonSpawnEx()
    {
        var random = Random.Range(0, 2);
        switch (random)
        {
            case 0:
                _EffectManager.GetEffectOrNull(EPrefabName.FireEx, transform.position, null,
                    new WaitForSeconds(3.0f));
                break;
            case 1:
                _EffectManager.GetEffectOrNull(EPrefabName.FireEx2, transform.position, null,
                    new WaitForSeconds(3.0f));
                break;
        }
    }

    private void PlayerSkill(Collider other)
    {
        if (other.CompareTag("Dragon"))
        {
            _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
            m_ImpulseHandler?.Invoke();
        }
        else if (other.CompareTag("Ground"))
        {
            CheckOverlap();
        }
    }
}