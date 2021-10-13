using System.Collections;
using System.Collections.Generic;
using Script.Dragon;
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
        private readonly WaitForSeconds m_ReturnDelay = new WaitForSeconds(3.0f);

        // 이펙트 스폰 위치
        private Transform m_ObjWeapon;
        [HideInInspector] public Transform playerLeftHand;
        [HideInInspector] public Transform playerRightHand;
        [HideInInspector] public Transform playerSpawnPosUp;
        [HideInInspector] public Transform playerSpawnPosFW;

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
                    playerSpawnPosUp = t;
                }

                if (t.name.Equals("SpawnPosFw"))
                {
                    playerSpawnPosFW = t;
                }

                if (t.name.Equals("ik_hand_l"))
                {
                    playerLeftHand = t;
                }

                if (t.name.Equals("ik_hand_r"))
                {
                    playerRightHand = t;
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
                _ObjPool.ReTurnObj(m_WeaponEffects.Dequeue(), EPrefabName.PlayerWeaponEffect, m_ReturnDelay);
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

        public GameObject GetEffect(EPrefabName effectName, Vector3 position, WaitForSeconds returnTime = null)
        {
            var obj = _ObjPool.GetObj(effectName);

            if (returnTime != null)
            {
                _ObjPool.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            return obj;
        }

        public PSMeshRendererUpdater GetMeshEffect(EPrefabName effectName, Vector3 position, GameObject owner)
        {
            var obj = _ObjPool.GetObj(effectName);

            obj.transform.position = position;
            obj.transform.SetParent(owner.transform);

            var rendererUpdater = obj.GetComponent<PSMeshRendererUpdater>();
            rendererUpdater.UpdateMeshEffect(owner);

            return rendererUpdater;
        }

        private IEnumerator EffectDelayGet(EPrefabName effectName, Vector3 position, Quaternion? rotPos,
            WaitForSeconds returnTime, WaitForSeconds delay, Transform owner)
        {
            yield return delay;
            GetEffect(effectName, position, rotPos, returnTime, null, owner);
        }

        #endregion

        #region DragonEffect

        private readonly Queue<PSMeshRendererUpdater> m_MeshPS = new Queue<PSMeshRendererUpdater>();
        private readonly Queue<GameObject> m_MeshEffect = new Queue<GameObject>();
        public Dragon_BreathTrigger dragonBreath;
        public GameObject backPos;
        public GameObject upPos;

        public void ActiveDragonsMesh(EPrefabName meshName)
        {
            var obj = GetMeshEffect(meshName, _DragonController.transform.position, _DragonController.gameObject);
            m_MeshEffect.Enqueue(obj.gameObject);
            m_MeshPS.Enqueue(obj);
        }

        public void DeActiveDragonMesh()
        {
            m_MeshPS.Dequeue().IsActive = false;
            m_MeshEffect.Dequeue();
        }

        public void DragonBreath(bool isActive, WaitForSeconds time = null)
        {
            if (time != null)
            {
                StartCoroutine(DragonDelayBreath(isActive, time));
                return;
            }

            dragonBreath.SetEnable(isActive, 1);
        }

        public void SetActiveDragonFlyBreath(bool isActive)
        {
            dragonBreath.SetEnable(isActive, 2);
        }

        public void SetActiveUltimate(bool isActive)
        {
            upPos.SetActive(isActive);
            backPos.SetActive(isActive);
        }

        private IEnumerator DragonDelayBreath(bool isActive, WaitForSeconds time)
        {
            yield return time;
            dragonBreath.SetEnable(isActive, 1);
        }

        #endregion
    }
}