using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private readonly float m_ViewAngle = 130f;
        private float m_DisToCondition;
        private float m_StopDis;
        private Transform m_Dragon;

        public override void Init()
        {
            m_Dragon = owner.GetComponent<Transform>();
            m_StopDis = owner.nav.stoppingDistance;
            m_DisToCondition = Mathf.Pow(m_StopDis, 2);
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateUpdate()
        {
            var _playerPos = owner.player.position;

            
            // if (m_StopDis > _dis.magnitude)
            // {
            //     machine.animator.SetFloat(m_MovementFloatHash,-1f,owner.dragonStat.moveAnimDamp,Time.deltaTime);
            //     return;
            // }
            machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude,
                owner.dragonStat.moveAnimDamp, Time.deltaTime);
            owner.nav.SetDestination(_playerPos);
            
            if (CheckDis() == false)
                return;
            var _player = Quaternion.LookRotation((_playerPos - owner.transform.position).normalized);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, _player,
                owner.dragonStat.rotSpeed * Time.deltaTime);
        }

        public override void OnStateExit()
        {
            owner.nav.ResetPath();
            machine.animator.SetFloat(m_MovementFloatHash, 0f);
        }

        public override void OnStateChangePoint()
        {
            if (owner.currentPhaseFlag.HasFlag(EDragonPhaseFlag.Angry))
            {
            }

            if (CheckDis())
            {
                if (PlayerIsBehind() && owner.bReadyTail)
                {
                    machine.ChangeState<G_Dragon_Tail>();
                }
                else if(owner.bReadyAttack)
                {
                    machine.ChangeState<G_Dragon_Attack>();
                }
            }
        }

        // private bool Find()
        // {
        //     var _results = new Collider[70];
        //     var _size = Physics.OverlapSphereNonAlloc(owner.transform.position, 5, _results, owner.playerMask);
        //     for (var i = 0; i < _size; i++)
        //     {
        //         var _dragonTr = owner.transform;
        //         var _tr = _results[i].transform;
        //         var _dir = (_tr.position - _dragonTr.position).normalized;
        //         if (Vector3.Angle(_dragonTr.forward, _dir) < m_ViewAngle * 0.5f)
        //         {
        //             var _dis = Vector3.Distance(owner.transform.position, _tr.position);
        //             if (Physics.Raycast(owner.transform.position, _dir, _dis, owner.playerMask))
        //             {
        //                 return true;
        //             }
        //         }
        //     }
        //     return false;
        // }

        private bool PlayerIsBehind() => Vector3.Dot(m_Dragon.forward, owner.player.position - m_Dragon.position) <= 0;

        private bool CheckDis() => (owner.player.position - owner.transform.position).sqrMagnitude <= m_DisToCondition;
    }
}