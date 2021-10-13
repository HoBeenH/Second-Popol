using System.Collections;
using UnityEngine;
using static Script.Facade;
namespace Script.Dragon
{
    public class Dragon_BackSpawn : Dragon_SkillSpawn
    {
        private readonly WaitForSeconds m_BackReturn = new WaitForSeconds(30.0f);
        private void OnEnable()
        {
            if (isAwake)
            {
                isAwake = false;
                this.gameObject.SetActive(false);
                return;
            }

            StartCoroutine(nameof(BackSpawner));
        }
        
        private IEnumerator BackSpawner()
        {
            for (var i = 0; i < loop; i++)
            {
                var _spawnPos = new Vector3(Random.Range(-size.x * 0.5f, size.x * 0.5f),
                    Random.Range(-size.y * 0.5f, size.y * 0.5f), 0f) + pivot;
                _EffectManager.GetEffect(EPrefabName.Fire, _spawnPos, m_BackReturn)
                    .transform.LookAt(_PlayerController.transform);
                yield return patternDelay;
            }
        }
    }
}