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
        private float m_RunBlend = 1f;
        private float m_RunSpeed;
        protected bool isMove = false;

        private readonly Type m_MTopDown = typeof(Player_MagicTopDown);
        private readonly Type m_Shoot = typeof(Player_Shoot);
        private readonly Type m_HeavyShoot = typeof(Player_HeavyShoot);
        private readonly Type m_WTopDown = typeof(Player_WeaponTopDown);
        private readonly Type m_Attack = typeof(Player_SwordAttack);
        private readonly Type m_Parrying = typeof(Player_Parrying);
        private readonly Type m_ChangeWeapon = typeof(Player_WeaponChange);
        private readonly Type m_Sliding = typeof(Player_Sliding);

        protected override void Init() => m_CamPos = Camera.main.transform;

        public override void OnStateEnter() => isMove = false;

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

                if (Input.GetKeyDown(KeyCode.Q) && _SkillManager.FindSkill(m_WTopDown).isActive)
                {
                    _SkillManager.FindSkill(m_WTopDown).isActive = false;
                    machine.ChangeState(m_WTopDown);
                }
            }
            else if (owner.playerFlag.HasFlag(EPlayerFlag.Magic))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    machine.ChangeState(m_Shoot);
                }

                if (Input.GetMouseButtonDown(1) && _SkillManager.FindSkill(m_HeavyShoot).isActive)
                {
                    _SkillManager.FindSkill(m_HeavyShoot).isActive = false;
                    machine.ChangeState(m_HeavyShoot);
                }

                if (Input.GetKeyDown(KeyCode.Q) && _SkillManager.FindSkill(m_MTopDown).isActive)
                {
                    _SkillManager.FindSkill(m_MTopDown).isActive = false;
                    machine.ChangeState(m_MTopDown);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                isMove = true;
                machine.ChangeState(m_ChangeWeapon);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                machine.ChangeState(m_Sliding);
            }
        }

        public override void OnStateUpdate()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                m_RunBlend = Mathf.Lerp(m_RunBlend, 2f, 5 * Time.deltaTime);
                m_RunSpeed = Mathf.Lerp(m_RunSpeed, 1f, 5 * Time.deltaTime);
            }
            else
            {
                m_RunBlend = Mathf.Lerp(m_RunBlend, 1f, 5 * Time.deltaTime);
                m_RunSpeed = Mathf.Lerp(m_RunSpeed, 0f, 5 * Time.deltaTime);
            }

            var _x = Input.GetAxis("Horizontal") * m_RunBlend;
            var _z = Input.GetAxis("Vertical") * m_RunBlend;

            machine.anim.SetFloat(m_MoveXHash, _x, 0.1f, Time.deltaTime);
            machine.anim.SetFloat(m_MoveZHash, _z, 0.1f, Time.deltaTime);

            var _lookForward = m_CamPos.forward;
            _lookForward.y = 0f;

            var _moveDIr = new Vector3(_x, 0f, _z).normalized;
            owner.transform.forward = Vector3.Lerp(owner.transform.forward, _lookForward,
                owner.Stat.rotSpeed * Time.deltaTime);
            owner.transform.Translate(_moveDIr * (Time.deltaTime * (owner.Stat.moveSpeed + m_RunSpeed)));
        }

        public override void OnStateExit()
        {
            if (!isMove)
            {
                machine.anim.SetFloat(m_MoveXHash, 0);
                machine.anim.SetFloat(m_MoveZHash, 0);
            }
        }
    }
}