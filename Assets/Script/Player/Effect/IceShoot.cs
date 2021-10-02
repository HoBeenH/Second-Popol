using System;
using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.Effect
{
    public class IceShoot : MonoBehaviour
    {
        private readonly WaitForSeconds m_ReturnTime = new WaitForSeconds(3.0f);

        private void Update()
        {
            Debug.DrawRay(transform.position,transform.forward * 20f,Color.magenta);
        }

        void OnEnable()
        {
            if (Physics.Raycast(transform.position, transform.forward, out var _hit, 20f,
                _PlayerController.dragon))
            {
                // if (!DragonController.Instance.currentPhaseFlag.HasFlag(EDragonPhaseFlag.CantParry))
                {
                    _EffectManager.GetEffectOrNull(EPrefabName.Ice, _hit.point, null, m_ReturnTime);
                    _DragonController.Frozen();
                }
                Debug.Log(_hit);
            }
        }
    }
}