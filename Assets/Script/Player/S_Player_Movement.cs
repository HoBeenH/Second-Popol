using TMPro.EditorUtilities;
using UnityEngine;

namespace Script.Player
{
    public class S_Player_Movement : State<PlayerController>
    {
        private readonly int m_MoveXHash = Animator.StringToHash("MoveX");
        private readonly int m_MoveZHash = Animator.StringToHash("MoveZ");
        private int m_RunBlend;
        private float m_RunSpeed;
        private Transform m_CamPos;
        private float m_Hor;
        private float m_Ver;

        public override void Init()
        {
            m_CamPos = Camera.main.transform;
        }

        public override void OnStateEnter()
        {
            Debug.Log(ToString());
        }

        public override void OnStateChangePoint()
        {
            if (owner.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Sword))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    machine.ChangeState<W_Player_Attack>();
                }

                if (Input.GetMouseButtonDown(1))
                {
                    machine.ChangeState<W_Player_Parrying>();
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    machine.ChangeState<W_Player_TopDown>();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    machine.ChangeState<W_Player_Skill>();
                }
            }
            else if (owner.currentWeaponFlag.HasFlag(ECurrentWeaponFlag.Magic))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    machine.ChangeState<M_Player_Shoot>();
                }       
                if (Input.GetMouseButtonDown(1))
                {
                    machine.ChangeState<M_Player_HeavyShoot>();
                }   
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    machine.ChangeState<M_Player_TopDown>();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                machine.ChangeState<S_Player_ChangeWeapon>();
            }

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                machine.ChangeState<S_Player_Sliding>();
            }
        }

        public override void OnStateUpdate()
        {
            m_Hor = Input.GetAxis("Horizontal");
            m_Ver = Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.LeftShift))
            {
                m_RunBlend = 2;
                m_RunSpeed = 1;
            }
            else
            {
                m_RunBlend = 1;
                m_RunSpeed = 0;
            }
            machine.animator.SetFloat(m_MoveXHash, m_Hor * m_RunBlend, owner.PlayerStat.moveAnimDamp,
                Time.deltaTime);
            machine.animator.SetFloat(m_MoveZHash, m_Ver * m_RunBlend, owner.PlayerStat.moveAnimDamp,
                Time.deltaTime);

            var _lookRight = m_CamPos.right;
            var _lookForward = m_CamPos.forward;
            _lookRight.y = 0f;
            _lookForward.y = 0f;
            var _moveDir = ((_lookForward * m_Ver) + (_lookRight * m_Hor)).normalized;

            owner.transform.forward = Vector3.Lerp(owner.transform.forward, _lookForward,
                owner.PlayerStat.rotSpeed * Time.deltaTime);
            owner.transform.Translate(_moveDir * Time.deltaTime * (owner.PlayerStat.moveSpeed + m_RunSpeed),
                Space.World);
        }

        public override void OnStateExit()
        {
            machine.animator.SetFloat(m_MoveXHash, 0f);
            machine.animator.SetFloat(m_MoveZHash, 0f);
        }
    }
}