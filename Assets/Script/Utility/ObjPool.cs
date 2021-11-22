using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script
{
    public enum EPrefabName
    {
        PlayerWeaponEffect,
        TopDownHand,
        TopDown,
        Shoot,
        BreathForce,
        DragonDownSmoke,
        DragonDownSmoke2,
        Fire,
        FireEx,
        HealthUp,
        Ultimate,
        FlyAttack,
        FireDragon,
        FireDragonSpawn,
        FireEx2,
        FireDragonEx,
        ShootHand,
        ShootEx,
        HeavyShootHand,
        FirstHeavyShoot,
        FirstHeavyShootEx,
        LastHeavyShoot,
        LastHeavyShootEx,
        SecondHeavyShoot,
        SecondHeavyShootEx,
        Blood1,
        Blood2,
        Blood3,
        Blood4,
        Blood5,
        Blood6,
        Blood7,
        Blood8,
        Blood9,
        Blood10,
        Blood11,
        Blood12,
        Blood13,
        Blood14,
        Blood15,
        Blood16,
        Blood17,
        BloodDecal
    }

    public class ObjPool : MonoSingleton<ObjPool>
    {
        [SerializeField] private Prefabs[] m_PlayerPrefab;
        [SerializeField] private Prefabs[] m_EnemyPrefab;

        #region PrefabClass

        [Serializable]
        public class Prefabs
        {
            public EPrefabName name;
            public GameObject prefabObj;
            public int cnt;
            public Queue<GameObject> objQueue = new Queue<GameObject>();
            [HideInInspector] public Transform parent;
        }

        #endregion

        private void Awake() => CreatParent();

        private void CreatParent()
        {
            var _transform = transform;
            foreach (var _t in m_PlayerPrefab)
            {
                var _currentPrefabParent = new GameObject(_t.name.ToString());
                _currentPrefabParent.transform.SetParent(_transform);
                _t.parent = _currentPrefabParent.transform;
                EnqueueObj(_t);
            }

            foreach (var _t in m_EnemyPrefab)
            {
                var _currentPrefabParent = new GameObject(_t.name.ToString());
                _currentPrefabParent.transform.SetParent(_transform);
                _t.parent = _currentPrefabParent.transform;
                EnqueueObj(_t);
            }
        }

        private void EnqueueObj(Prefabs prefabName)
        {
            for (var i = 0; i < prefabName.cnt; i++)
            {
                prefabName.objQueue.Enqueue(CreatNewObj(prefabName));
            }
        }

        private GameObject CreatNewObj(Prefabs prefabName)
        {
            var _obj = Instantiate(prefabName.prefabObj, prefabName.parent);
            _obj.SetActive(false);
            return _obj;
        }

        private Prefabs FindObjName(EPrefabName prefabName) => m_PlayerPrefab.FirstOrDefault(p => p.name == prefabName);

        private Prefabs FindEnemyObjName(EPrefabName prefabName) =>
            m_EnemyPrefab.FirstOrDefault(p => p.name == prefabName);

        public GameObject GetObj(EPrefabName prefabName)
        {
            var _currentPrefab = FindObjName(prefabName) ?? FindEnemyObjName(prefabName);
            if (_currentPrefab.objQueue.Count > 0)
            {
                var _obj = _currentPrefab.objQueue.Dequeue();
                _obj.transform.SetParent(null);
                _obj.SetActive(true);
                return _obj;
            }
            else
            {
                var _obj = CreatNewObj(_currentPrefab);
                _obj.transform.SetParent(null);
                _obj.SetActive(true);
                return _obj;
            }
        }

        private void ReTurnObj(GameObject returnObj, EPrefabName prefabName)
        {
            var _currentPrefab = FindObjName(prefabName) ?? FindEnemyObjName(prefabName);
            returnObj.transform.SetParent(_currentPrefab.parent);
            returnObj.SetActive(false);
            _currentPrefab.objQueue.Enqueue(returnObj);
        }

        public void ReTurnObj(GameObject returnObj, EPrefabName prefabName, WaitForSeconds time) =>
            StartCoroutine(ReTurnDelay(returnObj, prefabName, time));

        public void ReTurnObj(GameObject returnObj, EPrefabName prefabName, float time) =>
            StartCoroutine(ReTurnDelay(returnObj, prefabName, new WaitForSeconds(time)));

        private IEnumerator ReTurnDelay(GameObject returnObj, EPrefabName prefabName, WaitForSeconds time)
        {
            yield return time;
            ReTurnObj(returnObj, prefabName);
        }
    }
}