using System;
using UnityEngine;
using static Script.Facade;

namespace Script.Player
{
    public class S_Player_Movement : State<PlayerController>
    {
        private readonly int m_MoveXHash = Animator.StringToHash("MoveX");
        private readonly int m_MoveZHash = Animator.StringToHash("MoveZ");
        private Transform m_CamPos;
        private int m_RunBlend;
        private float m_RunSpeed;
        private float m_Hor;
        private float m_Ver;
        private bool m_BCanMove = false;
        protected bool BCanRun;

        private readonly Type m_MTopDown = typeof(M_Player_TopDown);
        private readonly Type m_Shoot = typeof(M_Player_Shoot);
        private readonly Type m_HeavyShoot = typeof(M_Player_HeavyShoot);
        private readonly Type m_WTopDown = typeof(W_Player_TopDown);
        private readonly Type m_Attack = typeof(W_Player_Attack);
        private readonly Type m_Parrying = typeof(W_Player_Parrying);
        private readonly Type m_ChangeWeapon = typeof(S_Player_ChangeWeapon);
        private readonly Type m_Sliding = typeof(S_Player_Sliding);

        protected override void Init()
        {
            m_CamPos = Camera.main.transform;
        }

        public override void OnStateEnter()
        {
            BCanRun = true;
        }

        public override void OnStateChangePoint()
        {
            if (owner.playerFlag.HasFlag(EPlayerFlag.Sword))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    machine.ChangeState(m_Attack);
                }

                if (Input.GetMouseButtonDown(1))
                {
                    machine.ChangeState(m_Parrying);
                }

                if (Input.GetKeyDown(KeyCode.Q) && _SkillManager.FindSkill(m_WTopDown).BIsActive)
                {
                    _SkillManager.FindSkill(m_WTopDown).BIsActive = false;
                    machine.ChangeState(m_WTopDown);
                }
            }
            else if (owner.playerFlag.HasFlag(EPlayerFlag.Magic))
            {
                if (Input.GetMouseButtonDown(0) && _SkillManager.FindSkill(m_Shoot).BIsActive)
                {
                    _SkillManager.FindSkill(m_Shoot).BIsActive = false;
                    machine.ChangeState(m_Shoot);
                }

                if (Input.GetMouseButtonDown(1) && _SkillManager.FindSkill(m_HeavyShoot).BIsActive)
                {
                    _SkillManager.FindSkill(m_HeavyShoot).BIsActive = false;
                    machine.ChangeState(m_HeavyShoot);
                }

                if (Input.GetKeyDown(KeyCode.Q) && _SkillManager.FindSkill(m_MTopDown).BIsActive)
                {
                    _SkillManager.FindSkill(m_MTopDown).BIsActive = false;
                    machine.ChangeState(m_MTopDown);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                m_BCanMove = true;
                machine.ChangeState(m_ChangeWeapon);
            }

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                machine.ChangeState(m_Sliding);
            }
        }

        public override void OnStateUpdate()
        {
            m_Hor = Input.GetAxis("Horizontal");
            m_Ver = Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.LeftShift) && BCanRun)
            {
                m_RunBlend = 2;
                m_RunSpeed = 1;
            }
            else
            {
                m_RunBlend = 1;
                m_RunSpeed = 0;
            }

            machine.animator.SetFloat(m_MoveXHash, m_Hor * m_RunBlend, 0.02f,
                Time.deltaTime);
            machine.animator.SetFloat(m_MoveZHash, m_Ver * m_RunBlend, 0.02f,
                Time.deltaTime);

            var _lookForward = m_CamPos.forward;
            _lookForward.y = 0f;

            var _moveDIr = new Vector3(m_Hor, 0f, m_Ver).normalized;
            owner.transform.forward = Vector3.Lerp(owner.transform.forward, _lookForward,
                owner.PlayerStat.rotSpeed * Time.deltaTime);
            owner.transform.Translate(_moveDIr * (Time.deltaTime * (owner.PlayerStat.moveSpeed + m_RunSpeed)));
        }

        public override void OnStateExit()
        {
            if (!m_BCanMove)
            {
                machine.animator.SetFloat(m_MoveXHash, 0);
                machine.animator.SetFloat(m_MoveZHash, 0);
            }
            else
            {
                m_BCanMove = false;
            }
        }
    }
}