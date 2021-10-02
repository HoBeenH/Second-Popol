using System.Collections;
using UnityEngine;

namespace Script.Player
{
    public class S_Player_ChangeWeapon : S_Player_Movement
    {
        // 이동상태 상속으로 이동구현
        private readonly int m_WeaponChangeHash = Animator.StringToHash("WeaponChange");
        private readonly WaitForSeconds m_AnimTime = new WaitForSeconds(2.0f);

        public override void OnStateEnter()
        {
            bcanRun = false;
            owner.PlayerStat.moveSpeed -= 1f;
            owner.StartCoroutine(WaitForAnim());
            machine.animator.SetTrigger(m_WeaponChangeHash);
            machine.animator.SetLayerWeight(1, 0.5f);
        }

        public override void OnStateChangePoint()
        {
        }

        private IEnumerator WaitForAnim()
        {
            yield return m_AnimTime;
            owner.PlayerStat.moveSpeed += 1f;
            machine.animator.SetLayerWeight(1, 0);
            machine.ChangeState<S_Player_Movement>();
        }
    }
}