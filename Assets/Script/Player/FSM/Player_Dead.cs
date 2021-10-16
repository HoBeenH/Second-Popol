using UnityEngine;

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
            var _find = owner.GetComponentsInChildren<Transform>();
            foreach (var _child in _find)
            {
                if (_child.name == "root")
                {
                    m_Root = _child.gameObject;
                }
            }
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
            foreach (var col in m_Rig)
            {
                col.isKinematic = false;
            }
            
            foreach (var col in m_Collider)
            {
                col.enabled = true;
            }

        }
    }
}