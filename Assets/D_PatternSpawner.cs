using System;
using System.Collections;
using Script;
using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

public class D_PatternSpawner : MonoBehaviour
{
    private enum ESetType
    {
        Up,
        Back
    }

    private Vector3 m_Size;
    private bool bIsAwake = true;
    [SerializeField] private ESetType type;

    private void Awake()
    {
        var _col = GetComponent<BoxCollider>();
        m_Size = _col.size;
        _col.enabled = false;
    }

    private void OnEnable()
    {
        if (bIsAwake)
        {
            bIsAwake = false;
            return;
        }

        switch (type)
        {
            case ESetType.Up:
                StartCoroutine(nameof(UpSpawner));
                break;
            case ESetType.Back:
                StartCoroutine(nameof(BackSpawner));
                break;
            default:
                throw new Exception($"??? Unknown Type {type.ToString()}");
        }
    }

    private IEnumerator UpSpawner()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return StartCoroutine(Spawn(10, ESetType.Up));
        }

        yield return null;
    }

    private IEnumerator BackSpawner()
    {
        for (var i = 0; i < 5; i++)
        {
            yield return StartCoroutine(Spawn(10, ESetType.Back));
        }
    }

    private IEnumerator Spawn(int count, ESetType pos)
    {
        for (var i = 0; i < count; i++)
        {
            var _pos = transform.position;
            Vector3 _spawnOffset;
            switch (pos)
            {
                case ESetType.Up:
                    _spawnOffset = new Vector3(Random.Range(-m_Size.x * 0.5f, m_Size.x * 0.5f),
                        1f, Random.Range(-m_Size.z * 0.5f, m_Size.z * 0.5f)) + _pos;
                    _EffectManager.GetEffectOrNull(EPrefabName.FireDragon, _spawnOffset, null, new WaitForSeconds(10f));
                    _EffectManager.GetEffectOrNull(EPrefabName.FireDragonSpawn, _spawnOffset, null,
                        new WaitForSeconds(5.0f), new WaitForSeconds(0.5f));
                    _EffectManager.GetEffectOrNull(EPrefabName.FireDragonEx,_spawnOffset,null,new WaitForSeconds(10.0f),new WaitForSeconds(3.8f));
                    break;

                case ESetType.Back:
                    _spawnOffset = new Vector3(Random.Range(-m_Size.x * 0.5f, m_Size.x * 0.5f),
                        Random.Range(-m_Size.y * 0.5f, m_Size.y * 0.5f), 0f) + _pos;
                    _EffectManager.GetEffectOrNull(EPrefabName.Fire, _spawnOffset, out var _obj,
                        null, new WaitForSeconds(30f));
                    _obj.transform.LookAt(_PlayerController.transform);
                    break;
                default:
                    throw new Exception($"Unknown Type : {pos.ToString()}");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    
}