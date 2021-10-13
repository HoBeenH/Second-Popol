using System;
using UnityEngine;
using static Script.Facade;

namespace Script.Player.FSM
{
    public class Player_Movement : State<Player_Controller>
    {
        private readonly int m_MoveXHash = Animator.StringToHash("MoveX");
        private readonly int m_MoveZHash = Animator.StringToHash("MoveZ");
        private Transform m_CamPos;
        private int m_RunBlend;
        private float m_RunSpeed;
        private float m_Hor;
        private float m_Ver;
        protected bool canMove = false;

        private readonly Type m_MTopDown = typeof(Player_MagicTopDown);
        private readonly Type m_Shoot = typeof(Player_Shoot);
        private readonly Type m_HeavyShoot = typeof(Player_HeavyShoot);
        private readonly Type m_WTopDown = typeof(Player_WeaponTopDown);
        private readonly Type m_Attack = typeof(Player_SwordAttack);
        private readonly Type m_Parrying = typeof(Player_Parrying);
        private readonly Type m_ChangeWeapon = typeof(Player_WeaponChange);
        private readonly Type m_Sliding = typeof(Player_Sliding);

        protected override void Init()
        {
            m_CamPos = Camera.main.transform;
        }

        public override void OnStateEnter()
        {
            canMove = true;
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

                if (Input.GetKeyDown(KeyCode.Q) && SkillManager.FindSkill(m_WTopDown).isActive)
                {
                    SkillManager.FindSkill(m_WTopDown).isActive = false;
                    machine.ChangeState(m_WTopDown);
                }
            }
            else if (owner.playerFlag.HasFlag(EPlayerFlag.Magic))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    machine.ChangeState(m_Shoot);
                }

                if (Input.GetMouseButtonDown(1) && SkillManager.FindSkill(m_HeavyShoot).isActive)
                {
                    SkillManager.FindSkill(m_HeavyShoot).isActive = false;
                    machine.ChangeState(m_HeavyShoot);
                }

                if (Input.GetKeyDown(KeyCode.Q) && SkillManager.FindSkill(m_MTopDown).isActive)
                {
                    SkillManager.FindSkill(m_MTopDown).isActive = false;
                    machine.ChangeState(m_MTopDown);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                canMove = true;
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
            if (Input.GetKey(KeyCode.LeftShift) && canMove)
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
            machine.animator.SetFloat(m_MoveXHash, 0);
            machine.animator.SetFloat(m_MoveZHash, 0);
        }
    }
}