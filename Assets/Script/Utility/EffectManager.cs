using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Script.Facade;

namespace Script
{
    public class EffectManager : MonoSingleton<EffectManager>
    {
        #region Player Weapon Effect

        // 무기 이펙트의 자연스러운 전환을 위한 큐
        private readonly Queue<PSMeshRendererUpdater> m_WeaponEffectsPs = new Queue<PSMeshRendererUpdater>();
        private readonly Queue<GameObject> m_WeaponEffects = new Queue<GameObject>();
        private readonly WaitForSeconds m_WeaponDelay = new WaitForSeconds(3.0f);

        // 이펙트 스폰 위치
        private Transform m_ObjWeapon;
        [HideInInspector] public Transform leftHand;
        [HideInInspector] public Transform rightHand;
        [HideInInspector] public Transform spawnPosUp;
        [HideInInspector] public Transform spawnPosFw;
        public Transform _dragonHead;

        public EPrefabName DragonMesh;

        private void Awake()
        {
            var _find = GameObject.FindGameObjectWithTag("Player").GetComponentsInChildren<Transform>();
            foreach (var t in _find)
            {
                if (t.name.Equals("Weapon_r"))
                {
                    m_ObjWeapon = t;
                }

                if (t.name.Equals("SpawnPos"))
                {
                    spawnPosUp = t;
                }

                if (t.name.Equals("SpawnPosFw"))
                {
                    spawnPosFw = t;
                }

                if (t.name.Equals("ik_hand_l"))
                {
                    leftHand = t;
                }

                if (t.name.Equals("ik_hand_r"))
                {
                    rightHand = t;
                }
            }
        }

        // 무기 이펙트 소환
        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                var obj = GetMeshEffect(EPrefabName.PlayerWeaponEffect, m_ObjWeapon.position, m_ObjWeapon.gameObject);
                m_WeaponEffects.Enqueue(obj.gameObject);
                m_WeaponEffectsPs.Enqueue(obj);
            }
            else
            {
                m_WeaponEffectsPs.Dequeue().IsActive = false;
                _ObjPool.ReTurnObj(m_WeaponEffects.Dequeue(), EPrefabName.PlayerWeaponEffect, m_WeaponDelay);
            }
        }

        #endregion

        #region Player Skill Effect

        // 스킬이펙트
        public void GetEffect(EPrefabName effectName, Vector3 position, Quaternion? rotPos = null,
            WaitForSeconds returnTime = null, WaitForSeconds delay = null, Transform owner = null)
        {
            if (delay != null)
            {
                StartCoroutine(EffectDelayGet(effectName, position, rotPos, returnTime, delay, owner));
                return;
            }

            var obj = _ObjPool.GetObj(effectName);

            if (returnTime != null)
            {
                _ObjPool.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            if (rotPos != null)
            {
                obj.transform.rotation = (Quaternion) rotPos;
            }

            if (owner != null)
            {
                obj.transform.SetParent(owner.transform);
            }
        }

        public void GetEffect(EPrefabName effectName, Vector3 position, out GameObject objReturn,
            Quaternion? rotPos = null, WaitForSeconds returnTime = null, float? delay = null, Transform owner = null)
        {
            if (delay != null)
            {
                var _time = 0f;
                var _maxTime = (float) delay;
                while (_maxTime >= _time)
                {
                    _time += Time.deltaTime;
                }
            }

            var obj = _ObjPool.GetObj(effectName);

            if (returnTime != null)
            {
                _ObjPool.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            if (rotPos != null)
            {
                obj.transform.rotation = (Quaternion) rotPos;
            }

            if (owner != null)
            {
                obj.transform.SetParent(owner.transform);
            }

            objReturn = obj;
        }

        public PSMeshRendererUpdater GetMeshEffect(EPrefabName effectName, Vector3 position,
            GameObject owner, WaitForSeconds returnTime = null, WaitForSeconds fadeTime = null)
        {
            var obj = _ObjPool.GetObj(effectName);
            if (returnTime != null)
            {
                _ObjPool.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            obj.transform.SetParent(owner.transform);

            var rendererUpdater = obj.GetComponent<PSMeshRendererUpdater>();
            rendererUpdater.UpdateMeshEffect(owner);
            if (fadeTime != null)
            {
                StartCoroutine(PsFade(rendererUpdater, fadeTime));
            }

            return rendererUpdater;
        }

        private IEnumerator EffectDelayGet(EPrefabName effectName, Vector3 position, Quaternion? rotPos,
            WaitForSeconds returnTime, WaitForSeconds delay, Transform owner)
        {
            yield return delay;
            GetEffect(effectName, position, rotPos, returnTime, null, owner);
        }

        private IEnumerator PsFade(PSMeshRendererUpdater updater, WaitForSeconds time)
        {
            yield return time;
            updater.IsActive = false;
        }

        public void DragonBreath(bool isActive, WaitForSeconds time = null)
        {
            if (time != null)
            {
                StartCoroutine(DragonDelayBreath(isActive, time));
                return;
            }

            _dragonHead.gameObject.SetActive(isActive);
        }

        private IEnumerator DragonDelayBreath(bool isActive, WaitForSeconds time)
        {
            yield return time;
            _dragonHead.gameObject.SetActive(isActive);
        }

        #endregion
    }
}