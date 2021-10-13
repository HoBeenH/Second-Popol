using UnityEngine;

namespace Script.Dragon
{
    public class Dragon_SkillSpawn : MonoBehaviour
    {
        public float delay;
        public int loop = 10;
        protected Vector3 size;
        protected Vector3 pivot;
        
        protected WaitForSeconds patternDelay;
        protected bool isAwake = true;

        private void Awake()
        {
            var _col = GetComponent<BoxCollider>();
            size = _col.size;
            pivot = transform.position;
            patternDelay = new WaitForSeconds(delay);
        }

    }
}