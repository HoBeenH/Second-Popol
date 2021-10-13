using System;
using Cinemachine;
using UnityEngine;
using System.Collections;
using static Script.Facade;

namespace Script
{
    // 캐릭터 스킬들의 기본 베이스
    public class Skill : MonoBehaviour
    {
        // 스킬 타입 Bullet - Boom
        protected enum ESkillType
        {
            Shoot,
            Boom
        }

        [SerializeField] protected ESkillType m_Type;

        [Header("Type : Shoot")] 
        [SerializeField] protected EPrefabName m_TriggerEffect;
        private Collider m_Col;
        [SerializeField] private float speed;
        [Space,Header("---------- Overlap ----------")]
        [SerializeField] private float radius;
        [SerializeField] private bool hasDelay;
        [SerializeField] private float delayTime;
        private WaitForSeconds m_Time;
        [Space,Header("---------- Other ----------")] 
        [SerializeField]
        protected bool HasImpulseSource;
        [SerializeField] protected CinemachineImpulseSource source;
        
        private readonly WaitForSeconds m_HitDelay = new WaitForSeconds(2.0f);
        private readonly WaitForSeconds m_Return = new WaitForSeconds(10.0f);
        private readonly Collider[] m_Result = new Collider[1];
        protected LayerMask layer;
        protected Action action;

        protected void Init()
        {
            TryGetComponent(out m_Col);

            if (HasImpulseSource)
            {
                source = GetComponent<CinemachineImpulseSource>();
            }

            m_Time = hasDelay ? new WaitForSeconds(delayTime) : new WaitForSeconds(0.1f);
        }

        private void OnEnable()
        {
            switch (m_Type)
            {
                case ESkillType.Shoot:
                    m_Col.enabled = true;
                    StartCoroutine(nameof(Move));
                    break;
                case ESkillType.Boom when hasDelay:
                    StartCoroutine(CheckOverlap());
                    break;
                case ESkillType.Boom:
                    StartCoroutine(CheckOverlap());
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
                m_Col.enabled = false;
            }
        }

        protected void HitTrigger()
        {
            _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, m_Return);

            m_Col.enabled = false;
            StartCoroutine(HtiDelay());
        }

        protected IEnumerator CheckOverlap()
        {
            yield return m_Time;
            if (HasImpulseSource)
            {
                source.GenerateImpulse();
            }
            if (Physics.OverlapSphereNonAlloc(transform.position, radius, m_Result, layer) != 0)
            {
                action.Invoke();
            }
        }

        protected IEnumerator Move()
        {
            while (true)
            {
                transform.Translate(transform.forward * Time.deltaTime * speed, Space.World);
                yield return null;
            }
        }

        private IEnumerator HtiDelay()
        {
            StopCoroutine(nameof(Move));
            yield return m_HitDelay;
            this.gameObject.SetActive(false);
        }
    }
}