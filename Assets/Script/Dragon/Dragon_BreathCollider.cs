using UnityEngine;
using static Script.Facade;

namespace Script.Dragon
{
    public class Dragon_BreathCollider : MonoBehaviour
    {
        public ParticleSystem breath;
        public ParticleSystem flyBreath;
        private bool isStart = true;

        public void SetEnable(bool isActive, int index)
        {
            if (isActive)
            {
                isStart = true;
                switch (index)
                {
                    case 1:
                        breath.gameObject.SetActive(true);
                        breath.Play();
                        break;
                    case 2:
                        flyBreath.gameObject.SetActive(true);
                        flyBreath.Play();
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 1:
                        breath.gameObject.SetActive(false);
                        break;
                    case 2:
                        flyBreath.gameObject.SetActive(false);
                        break;
                }
            }
        }

        private void OnParticleTrigger()
        {
            if (isStart)
            {
                _PlayerController.TakeDamage(_DragonController.DragonStat.damage,
                    (_PlayerController.transform.position - transform.position).normalized);
                isStart = false;
            }
        }
    }
}