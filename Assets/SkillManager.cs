using System;
using System.Collections;
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

    private void Awake()
    {
        m_Mask = m_Owner switch
        {
            EOwner.Dragon => m_Mask = 1 << 10,
            EOwner.Player => m_Mask = 1 << 11,
            _ => throw new Exception($"Pls Set Owner {m_Owner.ToString()}")
        };
    }

    private void OnEnable()
    {
        switch (m_Type)
        {
            case ESkillType.Shoot:
                StartCoroutine(nameof(Move));
                break;
            case ESkillType.Boom when m_Owner == EOwner.Dragon:
                if (CheckOverlap())
                {
                    _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                        (_PlayerController.transform.position - transform.position).normalized);
                }

                break;
            case ESkillType.Boom when m_Owner == EOwner.Player:
                if (CheckOverlap())
                {
                    _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
                }

                break;
            default:
                throw new Exception($"{m_Type.ToString()} or {m_Owner.ToString()} is Null");
        }
    }

    private void OnDisable()
    {
        StopCoroutine(nameof(Move));
    }

    private bool CheckOverlap()
    {
        var _size = Physics.OverlapSphereNonAlloc(transform.position, radius, m_Results, m_Mask);
        return _size != 0;
    }

    private IEnumerator Move()
    {
        while (true)
        {
            transform.Translate(transform.forward * Time.deltaTime * speed, Space.World);
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_Owner == EOwner.Dragon && other.CompareTag("Ground") || other.CompareTag("Player"))
        {
            DragonFire();
        }
        else if (m_Owner == EOwner.Player && other.CompareTag("Dragon"))
        {
            PlayerSkill();
        }

        this.gameObject.SetActive(false);
    }

    private void DragonFire()
    {
        StopCoroutine(nameof(Move));
        if (CheckOverlap())
        {
            _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                (_PlayerController.transform.position - transform.position).normalized);
        }

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

    private void PlayerSkill()
    {
        StopCoroutine(nameof(Move));
        _DragonController.TakeDamage(_PlayerController.PlayerStat.skillDamage, EPlayerFlag.Magic);
    }
}