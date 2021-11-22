using UnityEngine;

namespace Script.Player.FSM
{
    // 이동상태 상속으로 이동구현
    public class Player_WeaponChange : Player_Movement
    {
        private readonly int m_WeaponChangeHash = Animator.StringToHash("WeaponChange");
        private readonly int m_Weapon = Animator.StringToHash("Base Layer.Weapon.Weapon");
        private readonly int m_Magic = Animator.StringToHash("Base Layer.Weapon.Skill");

        public override void OnStateEnter()
        {
            machine.anim.SetLayerWeight(1, 1);
            isMove = true;
            machine.anim.SetTrigger(m_WeaponChangeHash);

            machine.cancel.Add(owner.playerFlag.HasFlag(EPlayerFlag.Magic)
                ? owner.StartCoroutine(machine.WaitForState(m_Weapon))
                : owner.StartCoroutine(machine.WaitForState(m_Magic)));
        }

        public override void OnStateExit() => machine.anim.SetLayerWeight(1, 0);

        public override void OnStateChangePoint()
        {
        }
    }
}