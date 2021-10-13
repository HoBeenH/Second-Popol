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
            var temp = owner.GetComponentsInChildren<Transform>();
            foreach (var _temp in temp)
            {
                if (_temp.name == "root")
                {
                    m_Root = _temp.gameObject;
                }
            }
            m_Collider = m_Root.GetComponentsInChildren<Collider>();
            m_Rig = m_Root.GetComponentsInChildren<Rigidbody>();
            m_MainCol = owner.GetComponent<CapsuleCollider>();
            m_MainRig = owner.GetComponent<Rigidbody>();
            DoRagDoll(false);
        }

        public override void OnStateEnter()
        {
            owner.StopAllCoroutines();
            DoRagDoll(true);
        }

        private void DoRagDoll(bool doRagDoll)
        {
            m_MainCol.enabled = !doRagDoll;
            machine.animator.enabled = !doRagDoll;
            foreach (var col in m_Rig)
            {
                col.isKinematic = !doRagDoll;
            }
            
            m_MainRig.isKinematic = doRagDoll;
            foreach (var col in m_Collider)
            {
                col.enabled = doRagDoll;
            }

        }
    }
}