using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_BackSpawn : Dragon_SkillSpawn
    {
        private readonly WaitForSeconds m_BackReturn = new WaitForSeconds(20.0f);

        private void Awake()
        {
            base.Init();
            type = ESpawnType.Back;
        }

        private void OnEnable() => StartCoroutine(nameof(BackSpawner));

        private IEnumerator BackSpawner()
        {
            var _pos = CreateRandomPos(type, size.x, size.y, loop, pivot);
            for (var i = 0; i < loop; i++)
            {
                var _obj = _EffectManager.GetEffect(EPrefabName.Fire, _pos[i], m_BackReturn);
                _obj.transform.LookAt(_PlayerController.transform);
                yield return patternDelay;
            }
        }
    }
}