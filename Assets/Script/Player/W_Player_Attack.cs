using System.Collections;
using UnityEngine;

namespace Script.Player
{
    public class W_Player_Attack : State<PlayerController>
    {
        private readonly int m_AttackLAnimHash = Animator.StringToHash("Base Layer.Attack.First Attack");
        private readonly int m_IdleAnimHash = Animator.StringToHash("Base Layer.Move");
        private readonly int m_Attack = Animator.StringToHash("Attack");
        private WaitUntil m_WaitForCurrentAnimFirstAttack;
        private WaitUntil m_WaitForCurrentAnimIdle;

        protected override void Init()
        {
            m_WaitForCurrentAnimFirstAttack = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_AttackLAnimHash);

            m_WaitForCurrentAnimIdle = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_IdleAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.useActionCam();
            EffectManager.Instance.EffectPlayerWeapon(true);
            machine.animator.SetTrigger(m_Attack);
            owner.StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return m_WaitForCurrentAnimFirstAttack;
            yield return m_WaitForCurrentAnimIdle;
            machine.animator.ResetTrigger(m_Attack);
            machine.ChangeState<S_Player_Movement>();
        }

        public override void OnStateChangePoint()
        {
            if (Input.GetMouseButtonDown(0))
            {
                machine.animator.SetTrigger(m_Attack);
            }
        }

        public override void OnStateExit()
        {
            owner.useDefaultCam();
            EffectManager.Instance.EffectPlayerWeapon(false);
        }
    }
}