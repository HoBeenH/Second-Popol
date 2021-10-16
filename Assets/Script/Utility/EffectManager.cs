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
        [SerializeField] private Transform m_ObjWeapon;
        public Transform playerLeftHand;
        public Transform playerRightHand;
        public Transform playerSpawnPosUp;
        public Transform playerSpawnPosFW;

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

        private PSMeshRendererUpdater GetMeshEffect(EPrefabName effectName, Vector3 position, GameObject owner)
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
        [SerializeField] private Dragon_BreathCollider dragonBreath;
        [SerializeField] private GameObject backPos;
        [SerializeField] private GameObject upPos;

        public void ActiveDragonMeshEffect(EPrefabName meshName)
        {
            var obj = GetMeshEffect(meshName, _DragonController.transform.position, _DragonController.gameObject);
            m_MeshEffect.Enqueue(obj.gameObject);
            m_MeshPS.Enqueue(obj);
        }

        public void DeActiveDragonMeshEffect()
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