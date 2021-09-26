using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class EffectManager : MonoSingleton<EffectManager>
    {
        #region Player Weapon Effect

        private readonly Queue<PSMeshRendererUpdater> m_EffectRenderersOfWeapon = new Queue<PSMeshRendererUpdater>();
        private readonly Queue<GameObject> m_EffectsOfWeapon = new Queue<GameObject>();
        private readonly WaitForSeconds m_EffectWeaponDelay = new WaitForSeconds(3.0f);
        [SerializeField] private GameObject objWeapon;

        public void EffectPlayerWeapon(bool isActive)
        {
            if (isActive)
            {
                var _currentEffect = ObjPool.Instance.GetObj(EPrefabName.PlayerWeaponEffect);
                var _effectRendererOfWeapon = _currentEffect.GetComponent<PSMeshRendererUpdater>();
                _currentEffect.transform.SetParent(objWeapon.transform);
                _effectRendererOfWeapon.UpdateMeshEffect(objWeapon);
                m_EffectsOfWeapon.Enqueue(_currentEffect);
                m_EffectRenderersOfWeapon.Enqueue(_effectRendererOfWeapon);
            }
            else
            {
                m_EffectRenderersOfWeapon.Dequeue().IsActive = false;
                ObjPool.Instance.ReTurnObj(m_EffectsOfWeapon.Dequeue(), EPrefabName.PlayerWeaponEffect,m_EffectWeaponDelay);
            }
        }
        // 열거형으로 각 모션의 딜레이타임 적용하기
        #endregion
    }
}