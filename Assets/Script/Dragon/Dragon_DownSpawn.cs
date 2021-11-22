using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_DownSpawn : Dragon_SkillSpawn
    {
        private readonly WaitForSeconds m_DownExDelay = new WaitForSeconds(3.8f);
        private readonly WaitForSeconds m_DownReturn = new WaitForSeconds(10.0f);
        private readonly WaitForSeconds m_DownSpawnDelay = new WaitForSeconds(0.5f);

        private void Awake()
        {
            base.Init();
            type = ESpawnType.Down;
        }

        private void OnEnable() => StartCoroutine(nameof(DownSpawner));

        private IEnumerator DownSpawner()
        {
            var _pos = CreateRandomPos(type, size.x, size.z, loop, pivot);
            for (var i = 0; i < loop; i++)
            {
                _EffectManager.GetEffect(EPrefabName.FireDragon, _pos[i], null, m_DownReturn);
                _EffectManager.GetEffect(EPrefabName.FireDragonSpawn, _pos[i], null, m_DownReturn,
                    m_DownSpawnDelay);
                _EffectManager.GetEffect(EPrefabName.FireDragonEx, _pos[i], null, m_DownReturn,
                    m_DownExDelay);
                yield return patternDelay;
            }
        }
    }
}