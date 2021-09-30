using System;
using System.Collections;
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
        [HideInInspector] public Transform leftHand;
        [HideInInspector] public Transform rightHand;
        [HideInInspector] public Transform spawnPosUp;
        [HideInInspector] public Transform spawnPosFw;
        public Transform _dragonBreath;

        private void Awake()
        {
            var _dragonFind = GameObject.FindGameObjectWithTag("Dragon").GetComponentsInChildren<Transform>();
            foreach (var t in _dragonFind)
            {
                if (t.name.Equals("Spawn Pos"))
                {
                    _dragonBreath = t;
                    break;
                }
            }

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

        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                var obj = GetMeshEffect(EPrefabName.PlayerWeaponEffect, m_ObjWeapon.position,
                    m_ObjWeapon.gameObject);
                m_WeaponEffects.Enqueue(obj.gameObject);
                m_EffectRenderersOfWeapon.Enqueue(obj);
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

        public void GetEffectOrNull(EPrefabName effectName, Vector3 position, Quaternion? rotPos = null,
            WaitForSeconds returnTime = null, WaitForSeconds delay = null, Transform owner = null)
        {
            if (delay != null)
            {
                StartCoroutine(EffectDelayGet(effectName, position, rotPos, returnTime, delay, owner));
                return;
            }

            var obj = ObjPool.Instance.GetObj(effectName);

            if (returnTime != null)
            {
                ObjPool.Instance.ReTurnObj(obj, effectName, returnTime);
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

        public PSMeshRendererUpdater GetMeshEffect(EPrefabName effectName, Vector3 position,
            GameObject owner, WaitForSeconds returnTime = null, WaitForSeconds fadeTime = null)
        {
            var obj = ObjPool.Instance.GetObj(effectName);
            if (returnTime != null)
            {
                ObjPool.Instance.ReTurnObj(obj, effectName, returnTime);
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
            GetEffectOrNull(effectName, position, rotPos, returnTime, null, owner);
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

            _dragonBreath.gameObject.SetActive(isActive);
        }

        private IEnumerator DragonDelayBreath(bool isActive, WaitForSeconds time)
        {
            yield return time;
            _dragonBreath.gameObject.SetActive(isActive);
        }

        #endregion
    }
}