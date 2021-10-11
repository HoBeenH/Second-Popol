using System;
using System.Collections;
using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public class Dragon_UltimateSpawn : MonoBehaviour
    {
        // 소환되는 패턴의 위치
        private enum ESetType
        {
            Down,
            Back
        }

        [SerializeField] private ESetType type;
        public int loop = 10;
        private Vector3 m_Size;
        private readonly WaitForSeconds m_DownReturn = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_DownSpawnDelay = new WaitForSeconds(0.5f);
        private readonly WaitForSeconds m_DownExDelay = new WaitForSeconds(3.8f);
        private readonly WaitForSeconds m_BackReturn = new WaitForSeconds(30.0f);
        private readonly WaitForSeconds m_PatternDelay = new WaitForSeconds(1.5f);
        private bool BisAwake = true;

        private void Awake()
        {
            var _col = GetComponent<BoxCollider>();
            m_Size = _col.size;
            _col.enabled = false;
        }

        private void OnEnable()
        {
            if (BisAwake)
            {
                BisAwake = false;
                return;
            }
            switch (type)
            {
                case ESetType.Down:
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
                yield return StartCoroutine(Spawn(loop, ESetType.Down));
            }

            yield return null;
        }

        private IEnumerator BackSpawner()
        {
            for (var i = 0; i < 5; i++)
            {
                yield return StartCoroutine(Spawn(loop, ESetType.Back));
            }
        }

        private IEnumerator Spawn(int count, ESetType pos)
        {
            for (var i = 0; i < count; i++)
            {
                var _pivot = transform.position;
                Vector3 _spawnOffset;
                switch (pos)
                {
                    case ESetType.Down:
                        _spawnOffset = new Vector3(Random.Range(-m_Size.x * 0.5f, m_Size.x * 0.5f),
                            1f, Random.Range(-m_Size.z * 0.5f, m_Size.z * 0.5f)) + _pivot;
                        _EffectManager.GetEffect(EPrefabName.FireDragon, _spawnOffset, null, m_DownReturn);
                        _EffectManager.GetEffect(EPrefabName.FireDragonSpawn, _spawnOffset, null, m_DownReturn,
                            m_DownSpawnDelay);
                        _EffectManager.GetEffect(EPrefabName.FireDragonEx, _spawnOffset, null, m_DownReturn,
                            m_DownExDelay);
                        break;

                    case ESetType.Back:
                        _spawnOffset = new Vector3(Random.Range(-m_Size.x * 0.5f, m_Size.x * 0.5f),
                            Random.Range(-m_Size.y * 0.5f, m_Size.y * 0.5f), 0f) + _pivot;
                        _EffectManager.GetEffect(EPrefabName.Fire, _spawnOffset, m_BackReturn)
                            .transform.LookAt(_PlayerController.transform);
                        break;

                    default:
                        throw new Exception($"Unknown Type : {pos.ToString()}");
                }

                yield return m_PatternDelay;
            }
        }
    }
}