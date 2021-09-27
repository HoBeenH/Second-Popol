using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class EffectManager : MonoSingleton<EffectManager>
    {
        #region Player Weapon Effect

        private readonly Queue<PSMeshRendererUpdater> m_EffectRenderersOfWeapon = new Queue<PSMeshRendererUpdater>();
        private readonly WaitForSeconds m_EffectWeaponDelay = new WaitForSeconds(3.0f);
        private readonly Queue<GameObject> m_WeaponEffects = new Queue<GameObject>();
        [SerializeField] private GameObject objWeapon;

        private void Awake()
        {
            var _findWeapon = GameObject.FindGameObjectWithTag("Player").GetComponentsInChildren<Transform>();
            foreach (var _transform in _findWeapon)
            {
                if (!_transform.name.Equals("Weapon_r"))
                    continue;
                objWeapon = _transform.gameObject;
                break;
            }
        }

        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                var _currentEffect = ObjPool.Instance.GetObj(EPrefabName.PlayerWeaponEffect);
                var _effectRendererOfWeapon = _currentEffect.GetComponent<PSMeshRendererUpdater>();
                _currentEffect.transform.SetParent(objWeapon.transform);
                _effectRendererOfWeapon.UpdateMeshEffect(objWeapon);
                m_WeaponEffects.Enqueue(_currentEffect);
                m_EffectRenderersOfWeapon.Enqueue(_effectRendererOfWeapon);
            }
            else
            {
                m_EffectRenderersOfWeapon.Dequeue().IsActive = false;
                ObjPool.Instance.ReTurnObj(m_WeaponEffects.Dequeue(), EPrefabName.PlayerWeaponEffect,m_EffectWeaponDelay);
            }
        }
        // 열거형으로 각 모션의 딜레이타임 적용하기
        #endregion
    }
}