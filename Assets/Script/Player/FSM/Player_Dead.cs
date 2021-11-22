using Sirenix.Utilities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Script.Player.FSM
{
    public class Player_Dead : State<Player_Controller>
    {
        private Collider[] m_Collider;
        private Rigidbody m_MainRig;
        private Collider m_MainCol;
        private Rigidbody[] m_Rig;
        private GameObject m_Root;

        protected override void Init()
        {
            owner.GetComponentsInChildren<Transform>().ForEach(t =>
            {
                if (t.name == "root")
                {
                    m_Root = t.gameObject;
                }
            });
            m_Collider = m_Root.GetComponentsInChildren<Collider>();
            m_Rig = m_Root.GetComponentsInChildren<Rigidbody>();
            m_MainCol = owner.GetComponent<CapsuleCollider>();
            m_MainRig = owner.GetComponent<Rigidbody>();
        }

        public override void OnStateEnter()
        {
            owner.StopAllCoroutines();
            DoRagDoll();
        }

        private void DoRagDoll()
        {
            m_MainCol.enabled = false;
            machine.anim.enabled = false;
            m_MainRig.isKinematic = true;
            m_Rig.ForEach(r =>
            {
                r.isKinematic = false;
                r.useGravity = true;
            });

            m_Collider.ForEach(c => c.isTrigger = false);
        }
    }
}