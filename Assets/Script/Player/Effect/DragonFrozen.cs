using Script.Dragon;
using UnityEngine;

namespace Script.Player.Effect
{
    public class DragonFrozen : MonoBehaviour
    {
        private void OnEnable()
        {
            DragonController.Instance.Frozen();
        }

        private void OnDisable()
        {
            DragonController.Instance.DeFrozen();
        }
    }
}
