using UnityEngine;

namespace Script.Dragon
{
    public class G_Dragon_Frozen : State<DragonController>
    {
        private readonly int m_FrozenHash;
        public G_Dragon_Frozen() : base("Base Layer.Frozen") => m_FrozenHash = Animator.StringToHash("Frozen");

        public override void OnStateEnter()
        {
            owner.currentPhaseFlag |= EDragonPhaseFlag.Frozen;
            machine.animator.SetTrigger(m_FrozenHash);
            owner.StartCoroutine(machine.WaitForIdle( animToHash));
        }

        public override void OnStateExit()
        {
            owner.currentPhaseFlag &= ~EDragonPhaseFlag.Frozen;
        }
    }
}