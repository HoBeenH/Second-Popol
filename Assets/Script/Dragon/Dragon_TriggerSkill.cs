using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public class Dragon_TriggerSkill : TriggerSkill
    {
        private void Awake()
        {
            base.Init();
            if (this.gameObject.name.Equals("Fire(Clone)"))
            {
                var _choice = Random.Range(0, 2);
                m_TriggerEffect = _choice switch
                {
                    0 => EPrefabName.FireEx,
                    1 => EPrefabName.FireEx2,
                    _ => EPrefabName.FireEx
                };
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (_PlayerController.transform.position - transform.position).normalized);

                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                HitTrigger();
            }
        }
    }
}