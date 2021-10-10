using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

namespace Script
{
    // 캐릭터 스킬들의 기본 베이스
    public class SkillController : MonoBehaviour
    {
        // 스킬 타입 Bullet - Boom
        protected enum ESkillType
        {
            Shoot,
            Boom
        }

        [SerializeField] protected ESkillType m_Type;
        [Header("Type : Shoot")] public float speed;
        public bool BHasTriggerEffect;
        [SerializeField] protected EPrefabName m_TriggerEffect;
        protected Collider col;

        [Header("Type : Boom")] public float radius;
        public bool BHasDelay;
        public float delayTime;
        private WaitForSeconds m_Time;
        
        [Space] public bool BHasImpulse;
        public CinemachineImpulseSource source;
        protected LayerMask mask;
        private readonly Collider[] m_Results = new Collider[1];

        protected Action impulseHandler;
        protected Action damage;

        protected void Init()
        {
            TryGetComponent<Collider>(out col);

            if (BHasImpulse)
            {
                source = GetComponent<CinemachineImpulseSource>();
                impulseHandler = () => { this.source.GenerateImpulse(); };
            }

            if (BHasDelay)
            {
                m_Time = new WaitForSeconds(delayTime);
            }
        }

        private void OnEnable()
        {
            switch (m_Type)
            {
                case ESkillType.Shoot:
                    col.enabled = true;
                    StartCoroutine(nameof(Move));
                    break;
                case ESkillType.Boom when BHasDelay:
                    StartCoroutine(BoomDelay());
                    break;
                case ESkillType.Boom:
                    CheckOverlap();
                    break;
                default:
                    throw new Exception($"{m_Type.ToString()} is Null");
            }
        }

        private void OnDisable()
        {
            if (m_Type == ESkillType.Shoot)
            {
                StopCoroutine(nameof(Move));
                col.enabled = false;
            }
        }

        protected void CheckOverlap()
        {
            impulseHandler?.Invoke();
            var _size = Physics.OverlapSphereNonAlloc(transform.position, radius, m_Results, mask);
            if (_size != 0)
            {
                damage?.Invoke();
            }
        }

        private IEnumerator BoomDelay()
        {
            yield return m_Time;
            CheckOverlap();
        }

        protected IEnumerator Move()
        {
            while (true)
            {
                transform.Translate(transform.forward * Time.deltaTime * speed, Space.World);
                yield return null;
            }
        }

        protected IEnumerator HtiDelay()
        {
            StopCoroutine(nameof(Move));
            yield return new WaitForSeconds(2.0f);
            this.gameObject.SetActive(false);
        }
    }
}