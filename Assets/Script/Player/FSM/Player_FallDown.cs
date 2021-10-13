using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_FallDown : State<Player_Controller>
    {
        private readonly int m_BkAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownBk");
        private readonly int m_FwAnimHash = Animator.StringToHash("Base Layer.FallDown.FallDownFw");
        private readonly int m_BkHash = Animator.StringToHash("FallDownBk");
        private readonly int m_FwHash = Animator.StringToHash("FallDownFw");

        public override void OnStateEnter()
        {
            foreach (var state in machine.cancel)
            {
                owner.StopCoroutine(state);
            }
            // 누워있는상태에서는 FallDown 안되게 만듬
            owner.playerFlag |= EPlayerFlag.FallDown;
            var _transform = owner.transform;
            // 전방 후방 확인
            var _point = Vector3.Dot(_transform.forward,
                (_DragonController.transform.position - _transform.position).normalized);
            if (_point >= 0)
            {
                machine.animator.SetTrigger(m_BkHash);
                machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_BkAnimHash)));
            }
            else
            {
                machine.animator.SetTrigger(m_FwHash);
                machine.cancel.Add(owner.StartCoroutine(machine.WaitForState(m_FwAnimHash)));
            }
        }

        public override void OnStateExit()
        {
            owner.playerFlag &= ~EPlayerFlag.FallDown;
        }
    }
}