using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public enum ESkillEffect
    {
        TopDown
    }

    public class EffectManager : MonoSingleton<EffectManager>
    {
        #region Player Weapon Effect

        private readonly Queue<PSMeshRendererUpdater> m_EffectRenderersOfWeapon = new Queue<PSMeshRendererUpdater>();
        private readonly Queue<GameObject> m_WeaponEffects = new Queue<GameObject>();
        private readonly WaitForSeconds m_EffectWeaponDelay = new WaitForSeconds(3.0f);
        [SerializeField] private GameObject objWeapon;

        private void Awake()
        {
            m_Player = GameObject.FindGameObjectWithTag("Player");
            var _findWeapon = GameObject.FindGameObjectWithTag("Player").GetComponentsInChildren<Transform>();
            foreach (var _transform in _findWeapon)
            {
                if (_transform.name.Equals("Weapon_r"))
                {
                    objWeapon = _transform.gameObject;
                }

                if (_transform.name.Equals("ik_hand_l"))
                {
                    leftHand = _transform.gameObject;
                }

                if (_transform.name.Equals("ik_hand_r"))
                {
                    rightHand = _transform.gameObject;
                }
            }
        }

        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                var _currentEffect = GetEffect(EPrefabName.PlayerWeaponEffect, objWeapon.transform.position, Quaternion.identity,null,
                    objWeapon);
                m_WeaponEffects.Enqueue(_currentEffect);
                m_EffectRenderersOfWeapon.Enqueue(_currentEffect.GetComponent<PSMeshRendererUpdater>());
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

        private readonly WaitForSeconds m_EffectTopDownHandTimer = new WaitForSeconds(6.0f);
        private readonly WaitForSeconds m_EffectTopDownTimer = new WaitForSeconds(5.0f);
        private readonly WaitForSeconds m_EffectTopDownSpawnDelay = new WaitForSeconds(0.8f);
        [SerializeField] private GameObject leftHand;
        [SerializeField] private GameObject rightHand;
        private GameObject m_Player;

        public void PlayerSkillEffect(ESkillEffect currentSkill)
        {
            switch (currentSkill)
            {
                case ESkillEffect.TopDown:
                    GetEffect(EPrefabName.TopDownHand, leftHand.transform.position, Quaternion.identity, m_EffectTopDownHandTimer, leftHand);
                    GetEffect(EPrefabName.TopDownHand, rightHand.transform.position, Quaternion.identity, m_EffectTopDownHandTimer,
                        rightHand);
                    var pos = m_Player.transform.position + m_Player.transform.forward * 3;
                    GetEffect(EPrefabName.TopDown, pos, Quaternion.identity, m_EffectTopDownTimer, m_EffectTopDownSpawnDelay);
                    break;
            }
        }

        public GameObject GetEffect(EPrefabName effectName, Vector3 position, Quaternion rotation, WaitForSeconds timer = null,
            GameObject owner = null, WaitForSeconds fadeTime = null)
        {
            var obj = ObjPool.Instance.GetObj(effectName);
            if (timer != null)
            {
                ObjPool.Instance.ReTurnObj(obj, effectName, timer);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (owner != null)
            {
                obj.transform.SetParent(owner.transform);
            }

            if (obj.TryGetComponent<PSMeshRendererUpdater>(out var _rendererUpdater))
            {
                _rendererUpdater.UpdateMeshEffect(owner);
                if (fadeTime != null)
                {
                    StartCoroutine(PsFade(_rendererUpdater, fadeTime));
                }
            }

            return obj;
        }

        public void GetEffect(EPrefabName effectName, Vector3 position, Quaternion rotation, WaitForSeconds timer, WaitForSeconds delay,
            GameObject owner = null, WaitForSeconds fadeTime = null)

        {
            StartCoroutine(DelayGet(effectName, position, rotation, timer, delay, owner, fadeTime));
        }

        private IEnumerator DelayGet(EPrefabName effectName, Vector3 position, Quaternion rotation, WaitForSeconds timer, WaitForSeconds delay,
            GameObject owner = null, WaitForSeconds fadeTime = null)
        {
            yield return delay;
            GetEffect(effectName, position, rotation, timer, owner, fadeTime);
        }

        private IEnumerator PsFade(PSMeshRendererUpdater updater, WaitForSeconds time)
        {
            yield return time;
            updater.IsActive = false;
        }

        #endregion
    }
}