using Script.Dragon;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class S_Player_FallDown : State<PlayerController>
    {
        private readonly int m_BkHash = Animator.StringToHash("FallDownBk");
        private readonly int m_FwHash = Animator.StringToHash("FallDownFw");
        private readonly int m_BkAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownBk");
        private readonly int m_FwAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownFw");

        public override void OnStateEnter()
        {
            owner.playerFlag |= EPlayerFlag.FallDown;
            var _transform = owner.transform;
            // 전방 후방 확인
            var _point = Vector3.Dot(_transform.forward,
                (_DragonController.transform.position - _transform.position).normalized);
            if (_point >= 0)
            {
                machine.animator.SetTrigger(m_BkHash);
                owner.StartCoroutine(machine.WaitForState(m_BkAnimHash));
            }
            else
            {
                machine.animator.SetTrigger(m_FwHash);
                owner.StartCoroutine(machine.WaitForState(m_FwAnimHash));
            }
        }

        public override void OnStateExit()
        {
            owner.playerFlag &= ~EPlayerFlag.FallDown;
        }
    }
}