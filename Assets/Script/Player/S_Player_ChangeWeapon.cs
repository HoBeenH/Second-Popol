using System;
using System.Collections;
using UnityEngine;

namespace Script.Player
{
    // public class S_Player_ChangeWeapon : State<PlayerController>
    public class S_Player_ChangeWeapon : S_Player_Movement
    {
        private readonly int m_WeaponChangeHash = Animator.StringToHash("WeaponChange");
        private readonly WaitForSeconds m_AnimTime = new WaitForSeconds(2.0f);
        
        public override void OnStateEnter()
        {
            machine.animator.SetLayerWeight(1,1);
            owner.PlayerStat.moveSpeed -= 1f;
            machine.animator.SetTrigger(m_WeaponChangeHash);
            owner.StartCoroutine(Wait());
        }
        
        public override void OnStateChangePoint()
        {
        }

        public override void OnStateUpdate()
        {
            base.OnStateUpdate();
        }

        private IEnumerator Wait()
        {
            yield return m_AnimTime;
            owner.PlayerStat.moveSpeed += 1f;
            machine.animator.SetLayerWeight(1,0);
            machine.ChangeState<S_Player_Movement>();
        }
    }
}