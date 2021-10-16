using UnityEngine;

namespace Script
{
    public enum ESpawnType
    {
        Back,
        Down
    }
    public class Dragon_SkillSpawn : MonoBehaviour
    {
        [HideInInspector]public ESpawnType type;
        public float delay;
        public int loop = 10;
        protected Vector3 size;
        protected Vector3 pivot;

        protected WaitForSeconds patternDelay;

        protected void Init()
        {
            var _col = GetComponent<BoxCollider>();
            size = _col.size;
            pivot = transform.position;
            patternDelay = new WaitForSeconds(delay);
            this.gameObject.SetActive(false);
        }
    }
}