using UnityEngine;
using static Script.Facade;

namespace Script.Dragon.FSM
{
    public class Dragon_Breath : State<Dragon_Controller>
    {
        private readonly WaitForSeconds m_BreathDelay = new WaitForSeconds(2.5f);
        private readonly WaitForSeconds m_ForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_ForceReturn = new WaitForSeconds(8f);
        private readonly int m_BreathHash;

        public Dragon_Breath() : base("Base Layer.Breath_Idle.Breath_Idle") =>
            m_BreathHash = Animator.StringToHash("Breath");

        public override void OnStateEnter()
        {
            owner.stateFlag |= EDragonFlag.CantParry;
            machine.anim.SetTrigger(m_BreathHash);
            owner.StartCoroutine(machine.WaitForState(animToHash));
            SetEffect();
        }

        public override void OnStateExit()
        {
            _EffectManager.DragonBreath(false);
            owner.stateFlag &= ~EDragonFlag.CantParry;
        }

        private void SetEffect()
        {
            _EffectManager.GetEffect(EPrefabName.BreathForce, owner.transform.position, null, m_ForceReturn, m_ForceDelay);
            _EffectManager.DragonBreath(true, m_BreathDelay);
        }
    }
}