using Script.Dragon;
using UnityEngine;

namespace Script.Player.Effect
{
    public class IceShoot : MonoBehaviour
    {
        private readonly WaitForSeconds m_ReturnTime = new WaitForSeconds(3.0f);
        
        void Update()
        {
            if (Physics.Raycast(transform.position, transform.forward, out var _hit, 20f, PlayerController.Instance.dragon))
            {
                if (DragonController.Instance.currentPhaseFlag.HasFlag(EDragonPhaseFlag.CantParry))
                {
                    return;
                }
                EffectManager.Instance.GetEffectOrNull(EPrefabName.Ice, _hit.point, null, m_ReturnTime);
                DragonController.Instance.Frozen();
                gameObject.SetActive(false);
            }
        }
    }
}
