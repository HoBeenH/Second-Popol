using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Pattern : State<DragonController>
    {
        private readonly WaitForSeconds m_PatternCoolTime = new WaitForSeconds(15.0f);
        private WaitUntil m_WaitTakeOff;
        private Transform m_DragonTr;
        private Vector3 m_PatternPos;
        private GameObject m_UpPos;
        private GameObject m_BackPos;
        private NavMeshLink m_Link;
        private Vector3 m_LinkPos;
        private Vector3 m_EndPos;
        private bool m_BIsFirst = true;
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly int m_TakeOffHash = Animator.StringToHash("Base Layer.Phase2Start.Takeoff");

        protected override void Init()
        {
            var _finds = GameObject.FindGameObjectWithTag("Pos").GetComponentsInChildren<Transform>();
            foreach (var child in _finds)
            {
                if (child.name.Equals("DragonPos"))
                {
                    m_PatternPos = child.position;
                }

                if (child.name.Equals("DSpawnPos Up"))
                {
                    m_UpPos = child.gameObject;
                }

                if (child.name.Equals("DSpawnPos Back"))
                {
                    m_BackPos = child.gameObject;
                }

                if (child.name.Equals("Link"))
                {
                    m_Link = child.GetComponent<NavMeshLink>();
                    m_LinkPos = child.position;
                }
            }

            m_DragonTr = owner.GetComponent<Transform>();

            m_EndPos = owner.transform.position;
            m_UpPos.SetActive(false);
            m_BackPos.SetActive(false);
            m_Link.gameObject.SetActive(false);
            m_WaitTakeOff = new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_TakeOffHash &&
                      machine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.35f);
        }

        public override void OnStateEnter()
        {
            if (m_BIsFirst)
            {
                _EffectManager.GetMeshEffect(_EffectManager.DragonMesh, _DragonController.transform.position,
                    _DragonController.gameObject);
                m_BIsFirst = false;
            }

            owner.nav.autoTraverseOffMeshLink = true;
            owner.currentStateFlag |= EDragonPhaseFlag.CantParry;
            owner.StartCoroutine(PatternStart());
        }

        public override void OnStateExit()
        {
            m_Link.gameObject.SetActive(false);
            m_UpPos.SetActive(false);
            m_BackPos.SetActive(false);
            owner.nav.autoTraverseOffMeshLink = false;
            owner.currentStateFlag &= ~EDragonPhaseFlag.CantParry;
        }

        private void SetLinkPos(Vector3 from, Vector3 to)
        {
            m_Link.gameObject.SetActive(true);
            m_Link.startPoint = from - m_LinkPos;
            m_Link.endPoint = to - m_LinkPos;
        }

        private IEnumerator PatternStart()
        {
            machine.animator.SetTrigger(m_PatternHash);
            yield return m_WaitTakeOff;
            yield return owner.StartCoroutine(MovePos());
            yield return owner.StartCoroutine(Pattern());
            yield return owner.StartCoroutine(PatternEnd());
            yield return owner.StartCoroutine(machine.WaitForIdle());

            yield return m_PatternCoolTime;
            owner.bReadyPattern = true;
        }

        private IEnumerator MovePos()
        {
            var _position = m_DragonTr.position;

            SetLinkPos(_position, m_PatternPos);
            m_Link.startPoint += Vector3.forward;
            owner.nav.SetDestination(m_PatternPos);

            var _dis = (m_PatternPos - _position).sqrMagnitude;
            while (_dis >= 20f)
            {
                _dis = (m_PatternPos - m_DragonTr.position).sqrMagnitude;
                yield return null;
            }

            var _timer = 0f;
            while (true)
            {
                m_DragonTr.rotation = Quaternion.Slerp(m_DragonTr.rotation,
                    Quaternion.identity, Time.deltaTime);
                _timer += Time.deltaTime;
                yield return null;
                if (_timer >= 4f)
                {
                    break;
                }
            }

            Debug.Log("UNDIVIDED");
        }

        private IEnumerator Pattern()
        {
            m_UpPos.SetActive(true);
            m_BackPos.SetActive(true);
            yield return new WaitForSeconds(20.0f);
        }

        private IEnumerator PatternEnd()
        {
            var _pos = m_DragonTr.position;

            SetLinkPos(_pos, m_EndPos);
            owner.nav.SetDestination(m_EndPos);

            var _dis = (m_EndPos - _pos).sqrMagnitude;
            while (_dis >= 4f)
            {
                _dis = (m_EndPos - m_DragonTr.position).sqrMagnitude;
                yield return null;
            }

            owner.nav.velocity = Vector3.zero;
            owner.nav.ResetPath();
            var _endPos = m_DragonTr.position;
            _EffectManager.GetEffectOrNull(EPrefabName.DragonDownSmoke, _endPos, null,
                new WaitForSeconds(3.0f));
            _EffectManager.GetEffectOrNull(EPrefabName.DragonDownSmoke2, _endPos, null,
                new WaitForSeconds(5.0f), null, m_DragonTr);
            machine.animator.SetTrigger(m_PatternHash);
        }
    }
}