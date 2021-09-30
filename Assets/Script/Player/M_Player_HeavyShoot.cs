using UnityEngine;

namespace Script.Player
{
    public class M_Player_HeavyShoot : State<PlayerController>
    {
        private readonly int m_HeavyShootHash;

        public M_Player_HeavyShoot() : base("Base Layer.Skill.Heavy Shoot") =>
            m_HeavyShootHash = Animator.StringToHash("Heavy Shoot");

        public override void OnStateEnter()
        {
            machine.animator.SetTrigger(m_HeavyShootHash);
            owner.StartCoroutine(machine.WaitForAnim(typeof(S_Player_Movement),true,animToHash));
        }

    }
}