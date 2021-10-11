using UnityEngine;
using static Script.Facade;
using Random = UnityEngine.Random;

namespace Script.Dragon
{
    public class DragonSkill : SkillController
    {
        private readonly WaitForSeconds m_Return = new WaitForSeconds(5.0f);

        private void Awake()
        {
            base.Init();
            base.mask = 1 << 10;
            base.damage = () => _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                (_PlayerController.transform.position - transform.position).normalized);

            if (this.gameObject.name.Equals("Fire(Clone)"))
            {
                var choice = Random.Range(0, 2);
                BHasTriggerEffect = true;
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

                if (BHasImpulse)
                {
                    source.GenerateImpulse();
                }
                HitTrigger();
            }
            else if (other.CompareTag("Ground"))
            {
                CheckOverlap();
                HitTrigger();
            }
        }
    }
}