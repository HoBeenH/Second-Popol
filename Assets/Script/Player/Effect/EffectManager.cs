﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Player.Effect
{
    public class EffectManager : MonoSingleton<EffectManager>
    {
        #region Player Weapon Effect

        private readonly Queue<PSMeshRendererUpdater> m_EffectRenderersOfWeapon = new Queue<PSMeshRendererUpdater>();
        private readonly Queue<GameObject> m_WeaponEffects = new Queue<GameObject>();
        private readonly WaitForSeconds m_EffectWeaponDelay = new WaitForSeconds(3.0f);
        private Transform m_ObjWeapon;
        public Transform leftHand;
        public Transform rightHand;
        public Transform spawnPosUp;
        public Transform spawnPosFw;

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
                    Debug.Log("!!!");
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

        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                m_WeaponEffects.Enqueue(GetMeshEffect(EPrefabName.PlayerWeaponEffect, m_ObjWeapon.position,
                    m_ObjWeapon.gameObject, out var _rendererUpdater));
                m_EffectRenderersOfWeapon.Enqueue(_rendererUpdater);
            }
            else
            {
                m_EffectRenderersOfWeapon.Dequeue().IsActive = false;
                ObjPool.Instance.ReTurnObj(m_WeaponEffects.Dequeue(), EPrefabName.PlayerWeaponEffect,
                    m_EffectWeaponDelay);
            }
        }

        #endregion

        #region Player Skill Effect

        public GameObject GetEffectOrNull(EPrefabName effectName, Vector3 position, Quaternion? rotPos = null, WaitForSeconds returnTime = null,
            WaitForSeconds delay = null, Transform owner = null)
        {
            if (delay != null)
            {
                StartCoroutine(EffectDelayGet(effectName, position, rotPos, returnTime, delay));
                return null;
            }


            var obj = ObjPool.Instance.GetObj(effectName);
            if (returnTime != null)
            {
                ObjPool.Instance.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            if (rotPos != null)
            {
                obj.transform.rotation = (Quaternion)rotPos;
            }
            if (owner != null)
            {
                obj.transform.SetParent(owner.transform);
            }

            return obj;
        }

        public GameObject GetMeshEffect(EPrefabName effectName, Vector3 position,
            GameObject owner, out PSMeshRendererUpdater rendererUpdater, WaitForSeconds returnTime = null,
            WaitForSeconds fadeTime = null)
        {
            var obj = ObjPool.Instance.GetObj(effectName);
            if (returnTime != null)
            {
                ObjPool.Instance.ReTurnObj(obj, effectName, returnTime);
            }

            obj.transform.position = position;
            obj.transform.SetParent(owner.transform);

            rendererUpdater = obj.GetComponent<PSMeshRendererUpdater>();
            rendererUpdater.UpdateMeshEffect(owner);
            if (fadeTime != null)
            {
                StartCoroutine(PsFade(rendererUpdater, fadeTime));
            }

            return obj;
        }

        private IEnumerator EffectDelayGet(EPrefabName effectName, Vector3 position, Quaternion? rotPos, WaitForSeconds returnTime,
            WaitForSeconds delay)
        {
            yield return delay;
            GetEffectOrNull(effectName, position, rotPos, returnTime);
        }

        private IEnumerator PsFade(PSMeshRendererUpdater updater, WaitForSeconds time)
        {
            yield return time;
            updater.IsActive = false;
        }

        #endregion
    }
}