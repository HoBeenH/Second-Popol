using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public class Dragon_Skill : Skill
    {
        public bool hasDamage;

        private void Awake()
        {
            base.Init();
            base.layer = 1 << 10;
            if (hasDamage)
            {
                base.action = () => _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (_PlayerController.transform.position - transform.position).normalized);
            }
            else
            {
                base.action = () =>
                    _PlayerController.UseFallDown((_PlayerController.transform.position - transform.position).normalized, 5f);
            }

            if (this.gameObject.name.Equals("Fire(Clone)"))
            {
                var choice = Random.Range(0, 2);
                m_TriggerEffect = choice switch
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

                if (HasImpulseSource)
                {
                    source.GenerateImpulse();
                }

                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                StartCoroutine(CheckOverlap());
                HitTrigger();
            }
        }
    }
}