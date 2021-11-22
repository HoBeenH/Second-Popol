using System.Collections;
using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_OverlapSkill : OverlapSkill
    {
        [SerializeField] private bool hasDamage;

        private void Awake() => base.Init(1 << 10);

        private void OnEnable() => StartCoroutine(CheckOverlap());

        private IEnumerator CheckOverlap()
        {
            yield return time;
            if (hasImpulse)
            {
                source.GenerateImpulse();
            }

            if (Physics.OverlapSphereNonAlloc(_DragonController.transform.position, radius, result, layer) != 0)
            {
                if (hasDamage)
                {
                    _PlayerController.TakeDamage(_DragonController.Stat.damage,
                        (_PlayerController.transform.position - _DragonController.transform.position).normalized);
                }
                else
                {
                    _PlayerController.UseFallDown(
                        (_PlayerController.transform.position - _DragonController.transform.position).normalized, 5f);
                }
            }
        }
    }
}