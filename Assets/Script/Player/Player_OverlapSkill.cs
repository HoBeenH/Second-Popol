using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script.Player
{
    public class Player_OverlapSkill : OverlapSkill
    {
        private void Awake() => base.Init(1 << 11);

        private void OnEnable() => StartCoroutine(CheckOverlap());

        private IEnumerator CheckOverlap()
        {
            yield return time;
            if (hasImpulse)
            {
                source.GenerateImpulse();
            }

            if (Physics.OverlapSphereNonAlloc(transform.position, radius, result, layer) != 0)
            {
                _DragonController.TakeDamage(_PlayerController.Stat.damage);
            }
        }
    }
}