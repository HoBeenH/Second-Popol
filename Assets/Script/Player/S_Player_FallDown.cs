using Script.Dragon;
using UnityEngine;

namespace Script.Player
{
    public class S_Player_FallDown : State<PlayerController>
    {
        private readonly int m_FallDownBkHash = Animator.StringToHash("FallDownBk");
        private readonly int m_FallDownFwHash = Animator.StringToHash("FallDownFw");
        private readonly int m_FallDownBkAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownBk");
        private readonly int m_FallDownFwAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownFw");

        public override void OnStateEnter()
        {
            owner.currentWeaponFlag |= ECurrentWeaponFlag.FallDown;
            var _transform = owner.transform;
            var _point = Vector3.Dot(_transform.forward,
                (DragonController.Instance.transform.position - _transform.position).normalized);
            if (_point >= 0)
            {
                machine.animator.SetTrigger(m_FallDownBkHash);
                owner.StartCoroutine(machine.WaitForAnim(typeof(S_Player_Movement), true,m_FallDownBkAnimHash));
            }
            else
            {
                machine.animator.SetTrigger(m_FallDownFwHash);
                owner.StartCoroutine(machine.WaitForAnim(typeof(S_Player_Movement), true,m_FallDownFwAnimHash));
            }
        }

        public override void OnStateExit()
        {
            owner.currentWeaponFlag &= ~ECurrentWeaponFlag.FallDown;
        }
    }
}