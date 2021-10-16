using System.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using static Script.Facade;

namespace Script
{
    public class TriggerSkill : MonoBehaviour
    {
        [SerializeField] protected EPrefabName m_TriggerEffect;
        [SerializeField] protected float speed;
        private readonly WaitForSeconds m_HitDelay = new WaitForSeconds(2f);
        private readonly WaitForSeconds m_Return= new WaitForSeconds(20f);
        private Transform[] m_Tr;
        private Collider m_Col;

        protected void Init()
        {
            m_Tr = GetComponents<Transform>();
            m_Col = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            m_Col.enabled = true;
        }

        private void OnDisable()
        {
            m_Col.enabled = false;
        }

        private void Update()
        {
            var _tr = new TransformAccessArray(m_Tr);
            var temp = new MoveJob
            {
                moveDir = transform.forward,
                deltaTime = Time.deltaTime,
                speed = speed
            };
            temp.Schedule(_tr).Complete();
            _tr.Dispose();
        }

        protected void HitTrigger()
        {
            _EffectManager.GetEffect(m_TriggerEffect, transform.position, null, @m_Return);
            m_Col.enabled = false;
            StartCoroutine(HtiDelay());
        }

        private IEnumerator HtiDelay()
        {
            yield return m_HitDelay;
            this.gameObject.SetActive(false);
        }
        
        private struct MoveJob : IJobParallelForTransform
        {
            public Vector3 moveDir;
            public float deltaTime;
            public float speed;
            
            public void Execute(int index, TransformAccess transform)
            {
                transform.position += (moveDir * speed * deltaTime);
            }
        }
    }
}