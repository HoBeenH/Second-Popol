using System.Collections;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.VFX;

namespace Script.Dragon
{
    public class Dragon_AnimationEvent : MonoBehaviour
    {
        [SerializeField] private Collider m_Col;
        public VisualEffect breath;
        public VisualEffect flyBreath;
        private Collider[] m_Tails;
        private readonly WaitForSeconds m_BreathDeActiveTime = new WaitForSeconds(1f);
        private readonly WaitForSeconds m_BreathDelay = new WaitForSeconds(2.5f);
        private readonly WaitForSeconds m_FlyBreathDelay = new WaitForSeconds(5f);

        private void Awake()
        {
            var temp = GetComponentInChildren<Dragon_TailCollider>();
            m_Tails = temp.GetComponentsInChildren<Collider>();
        }

        public void BiteCol(int trueOrFalse)
        {
            m_Col.enabled = trueOrFalse switch
            {
                0 => false,
                1 => true,
                _ => m_Col.enabled
            };
        }

        public void TailCol(int trueOrFalse)
        {
            switch (trueOrFalse)
            {
                case 0:
                    m_Tails.ForEach(t => t.enabled = false);
                    break;
                case 1:
                    m_Tails.ForEach(t => t.enabled = true);
                    break;
            }
        }

        public void GroundBreath() => StartCoroutine(BreathPlay(m_BreathDelay,breath));

        public void FlyBreath() => StartCoroutine(BreathPlay(m_FlyBreathDelay,flyBreath));

        private IEnumerator BreathPlay(WaitForSeconds time, VisualEffect visualEffect)
        {
            visualEffect.gameObject.SetActive(true);
            yield return time;
            visualEffect.Stop();
            yield return m_BreathDeActiveTime;
            visualEffect.gameObject.SetActive(false);
        }
    }
}