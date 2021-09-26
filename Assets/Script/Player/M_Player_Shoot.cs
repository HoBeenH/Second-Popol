using UnityEngine;

namespace Script.Player
{
    public class M_Player_Shoot : State<PlayerController>
    {
        private readonly int m_ShootHash;
        
        public M_Player_Shoot() : base("Base Layer.Skill.Shoot") => m_ShootHash = Animator.StringToHash("Shoot");
        
        public override void OnStateEnter()
        {
            owner.useActionCam();
            machine.animator.SetTrigger(m_ShootHash);
        }

        public override void OnStateUpdate()
        {
            if (machine.IsEnd())
            {
                machine.ChangeState<S_Player_Movement>();
            }
        }

        public override void OnStateExit()
        {
            owner.useDefaultCam();
        }
    }
}