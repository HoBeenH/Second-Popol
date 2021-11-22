using System.Collections;
using System.Collections.Generic;
using Script.Dragon;
using UnityEngine;
using XftWeapon;
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
        [SerializeField] private XWeaponTrail weaponTrail;

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

        public void TrailEffect(bool isActive)
        {
            switch (isActive)
            {
                case true:
                    weaponTrail.gameObject.SetActive(true);
                    break;
                case false:
                    weaponTrail.gameObject.SetActive(false);
                    break;
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

        private PSMeshRendererUpdater m_MeshPS;
        [SerializeField] private GameObject ultBK;
        [SerializeField] private GameObject ultUP;

        public void ActiveDragonMeshEffect(EPrefabName meshName)
        {
            m_MeshPS = GetMeshEffect(meshName, _DragonController.transform.position, _DragonController.gameObject);
            m_MeshPS.IsActive = true;
        }

        public void DeActiveDragonMeshEffect() => m_MeshPS.IsActive = false;

        public void SetActiveUltimate(bool isActive)
        {
            ultUP.SetActive(isActive);
            ultBK.SetActive(isActive);
        }

        #endregion
    }
}