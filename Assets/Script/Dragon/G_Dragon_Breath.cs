using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Breath : State<Dragon_Controller>
    {
        private readonly int m_BreathAnimHash = Animator.StringToHash("Base Layer.Breath_Idle.Breath_Idle");
        private readonly int m_BreathHash = Animator.StringToHash("Breath");
        private readonly WaitForSeconds m_ForceReturn = new WaitForSeconds(8f);
        private readonly WaitForSeconds m_ForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_BreathDelay = new WaitForSeconds(2.5f);
        private readonly Collider[] m_Result = new Collider[1];

        public override void OnStateEnter()
        {
            owner.currentStateFlag |= EDragonPhaseFlag.CantParry;
            owner.StartCoroutine(machine.WaitForState(m_BreathAnimHash));
            machine.animator.SetTrigger(m_BreathHash);
            if (owner.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                Phase2(owner.transform.position);
            }

            _EffectManager.DragonBreath(true, m_BreathDelay);
        }

        public override void OnStateExit()
        {
            _EffectManager.DragonBreath(false);
            owner.currentStateFlag &= ~EDragonPhaseFlag.CantParry;
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
        }

        private void Phase2(Vector3 pos)
        {
            _EffectManager.GetEffect(EPrefabName.BreathForce, pos, null, m_ForceReturn, m_ForceDelay);
            var _size = Physics.OverlapSphereNonAlloc(pos, 10f, m_Result, owner.playerMask);
            if (_size != 0)
            {
                _PlayerController.useFallDown.Invoke((_PlayerController.transform.position - pos).normalized, 5f);
            }
        }
    }
}