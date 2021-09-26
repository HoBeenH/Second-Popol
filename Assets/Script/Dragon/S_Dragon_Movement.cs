using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Script.Dragon
{
    public class S_Dragon_Movement : State<DragonController>
    {
        private readonly int m_MovementFloatHash = Animator.StringToHash("Move");
        private readonly float m_DisToCondition = Mathf.Pow(5, 2);
        private float m_ViewAngle = 130f;
        private Transform m_Dragon;

        public override void Init()
        {
            m_Dragon = owner.GetComponent<Transform>();
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateUpdate()
        {
            var playerPos = owner.player.position;

            machine.animator.SetFloat(m_MovementFloatHash, owner.nav.desiredVelocity.magnitude,
                owner.dragonStat.moveAnimDamp, Time.deltaTime);
            owner.nav.SetDestination(playerPos);

            if (CheckDis() == false)
                return;
            var player = Quaternion.LookRotation((playerPos - owner.transform.position).normalized);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, player,
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

            if (owner.nav.velocity == Vector3.zero && CheckDis())
            {
                if (PlayerIsBehind())
                {
                    machine.ChangeState<G_Dragon_Tail>();
                }
                else
                {
                    if (Find())
                    {
                        machine.ChangeState<G_Dragon_Attack>();
                    }
                }
            }
        }

        private bool Find()
        {
            var results = new Collider[10];
            var size = Physics.OverlapSphereNonAlloc(owner.transform.position, 5, results, owner.playerMask);
            for (int i = 0; i < size; i++)
            {
                var dragonTr = owner.transform;
                var tr = results[i].transform;
                var dir = (tr.position - dragonTr.position).normalized;
                if (Vector3.Angle(dragonTr.forward, dir) < m_ViewAngle * 0.5f)
                {
                    var dis = Vector3.Distance(owner.transform.position, tr.position);
                    if (Physics.Raycast(owner.transform.position, dir, dis, owner.playerMask))
                    {
                        Debug.Log("Hit");
                        return true;
                    }
                }
            }

            Debug.Log("No Hit");

            return false;
        }

        private bool PlayerIsBehind()
        {
            var dragonPos = m_Dragon.position;
            var playerPos = owner.player.position;
            var dot = Vector3.Dot(m_Dragon.forward, playerPos - dragonPos);
            return dot <= 0;
        }

        private bool CheckDis()
        {
            var dis = (owner.player.position - owner.transform.position).sqrMagnitude;
            return dis <= m_DisToCondition;
        }
    }
}