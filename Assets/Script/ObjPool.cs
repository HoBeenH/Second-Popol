using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public enum EPrefabName
    {
        PlayerWeaponEffect,
        TopDownHand,
        TopDown
    }

    public class ObjPool : MonoSingleton<ObjPool>
    {
        [SerializeField] private Prefabs[] prefab;

        #region PrefabClass

        [Serializable]
        public class Prefabs
        {
            public EPrefabName name;
            public GameObject prefabObj;
            public int count;
            public Queue<GameObject> objQueue = new Queue<GameObject>();
            [HideInInspector] public Transform prefabParent;
        }

        #endregion

        private void Awake()
        {
            CreatParent();
        }

        private void CreatParent()
        {
            for (var i = 0; i < prefab.Length; i++)
            {
                var _currentPrefabParent = new GameObject(prefab[i].name.ToString());
                _currentPrefabParent.transform.SetParent(this.transform);
                prefab[i].prefabParent = _currentPrefabParent.transform;
                EnqueueObj(prefab[i]);
            }
        }

        private void EnqueueObj(Prefabs prefabName)
        {
            for (var i = 0; i < prefabName.count; i++)
            {
                prefabName.objQueue.Enqueue(CreatNewObj(prefabName));
            }
        }

        private GameObject CreatNewObj(Prefabs prefabName)
        {
            var _obj = Instantiate(prefabName.prefabObj, prefabName.prefabParent, true);
            _obj.SetActive(false);
            return _obj;
        }

        private Prefabs FindObjName(EPrefabName prefabName)
        {
            for (var i = 0; i < prefab.Length; i++)
            {
                if (prefab[i].name == prefabName)
                {
                    return prefab[i];
                }
            }

            return null;
        }

        public GameObject GetObj(EPrefabName prefabName)
        {
            var _currentPrefab = FindObjName(prefabName);
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
            var _currentPrefab = FindObjName(prefabName);
            returnObj.transform.SetParent(_currentPrefab.prefabParent);
            returnObj.SetActive(false);
            _currentPrefab.objQueue.Enqueue(returnObj);
        }

        public void ReTurnObj(GameObject returnObj, EPrefabName prefabName, WaitForSeconds time)
        {
            StartCoroutine(ReTurnDelay(returnObj, prefabName, time));
        }

        public void ReTurnObj(GameObject returnObj, EPrefabName prefabName, float time)
        {
            var _delayTime = new WaitForSeconds(time);
            StartCoroutine(ReTurnDelay(returnObj, prefabName, _delayTime));
        }

        private IEnumerator ReTurnDelay(GameObject returnObj, EPrefabName prefabName, WaitForSeconds time)
        {
            yield return time;
            ReTurnObj(returnObj, prefabName);
        }
    }
}