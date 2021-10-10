using System.Collections;
using UnityEngine;

namespace Script.Player
{
    // 이동상태 상속으로 이동구현
    public class S_Player_ChangeWeapon : S_Player_Movement
    {
        private readonly int m_WeaponChangeHash = Animator.StringToHash("WeaponChange");
        private readonly int m_Weapon = Animator.StringToHash("Base Layer.Weapon.Weapon");
        private readonly int m_Skill = Animator.StringToHash("Base Layer.Weapon.Skill");

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_WeaponChangeHash);

            machine.cancel.Add(owner.playerFlag.HasFlag(EPlayerFlag.Magic)
                ? owner.StartCoroutine(machine.WaitForState(m_Weapon))
                : owner.StartCoroutine(machine.WaitForState(m_Skill)));

            BCanRun = false;
            owner.PlayerStat.moveSpeed -= 1f;
            machine.animator.SetLayerWeight(1, 0.5f);
        }

        public override void OnStateExit()
        {
            owner.PlayerStat.moveSpeed += 1f;
            machine.animator.SetLayerWeight(1, 0);
        }

        public override void OnStateChangePoint()
        {
            return;
        }
    }
}