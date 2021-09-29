using UnityEngine;

namespace Script.Dragon
{
    public class DragonBreath : MonoBehaviour
    {
        private ParticleSystem m_Breath;

        private void Awake()
        {
            m_Breath = GetComponentInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            m_Breath.Play();
        }

    }
}
