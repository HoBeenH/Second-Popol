using UnityEngine;

namespace Script.Player
{
    public class S_Player_Dead : State<PlayerController>
    {
        private Collider[] m_Collider;
        private Collider m_MainCol;
        private Rigidbody[] m_Rig;
        private GameObject root;
        protected override void Init()
        {
            var temp = owner.GetComponentsInChildren<Transform>();
            foreach (var _temp in temp)
            {
                if (_temp.name == "root")
                {
                    root = _temp.gameObject;
                }
            }
            m_Collider = root.GetComponentsInChildren<Collider>();
            foreach (var collider in m_Collider)
            {
                collider.enabled = false;
            }
            m_Rig = root.GetComponentsInChildren<Rigidbody>();
            foreach (var rig in m_Rig)
            {
                rig.isKinematic = true;
            }
            m_MainCol = owner.GetComponent<CapsuleCollider>();
        }

        public override void OnStateEnter()
        {
            m_MainCol.enabled = false;
            foreach (var col in m_Collider)
            {
                col.enabled = true;
            }
            foreach (var col in m_Rig)
            {
                col.isKinematic = false;
            }
        }
    }
}