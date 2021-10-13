using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_DownSpawn : Dragon_SkillSpawn
    {
        private readonly WaitForSeconds m_DownReturn = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_DownSpawnDelay = new WaitForSeconds(0.5f);
        private readonly WaitForSeconds m_DownExDelay = new WaitForSeconds(3.8f);

        private void OnEnable()
        {
            if (isAwake)
            {
                isAwake = false;
                this.gameObject.SetActive(false);
                return;
            }

            StartCoroutine(nameof(DownSpawner));
        }

        private IEnumerator DownSpawner()
        {
            for (var i = 0; i < loop; i++)
            {
                var _spawnPos = new Vector3(Random.Range(-size.x * 0.5f, size.x * 0.5f),
                    1f, Random.Range(-size.z * 0.5f, size.z * 0.5f)) + pivot;
                _EffectManager.GetEffect(EPrefabName.FireDragon, _spawnPos, null, m_DownReturn);
                _EffectManager.GetEffect(EPrefabName.FireDragonSpawn, _spawnPos, null, m_DownReturn,
                    m_DownSpawnDelay);
                _EffectManager.GetEffect(EPrefabName.FireDragonEx, _spawnPos, null, m_DownReturn,
                    m_DownExDelay);
                yield return patternDelay;
            }
        }
    }
}