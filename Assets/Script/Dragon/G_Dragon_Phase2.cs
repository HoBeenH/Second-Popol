using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_Phase2 : State<DragonController>
    {
        private readonly int m_TakeOffHash = Animator.StringToHash("Base Layer.Phase2Start.Takeoff");
        private readonly int m_PatternHash = Animator.StringToHash("Pattern");
        private readonly int m_Phase2 = Animator.StringToHash("Phase2");
        private WaitUntil m_WaitTakeOff;
        private Transform m_DragonTr;
        private Vector3 m_PatternPos;
        private GameObject m_UpPos;
        private GameObject m_BackPos;
        private NavMeshLink m_Link;
        private Vector3 m_LinkPos;
        private bool m_BIsFirst = true;
        private readonly WaitForSeconds m_Pattern = new WaitForSeconds(40f);

        protected override void Init()
        {
            _SkillManager.AddSkill(typeof(G_Dragon_Phase2),40f);
            
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
            m_UpPos.SetActive(false);
            m_BackPos.SetActive(false);
            m_WaitTakeOff = new WaitUntil(
                () => machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_TakeOffHash &&
                      machine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.35f);
            
        }

        public override void OnStateEnter()
        {
            if (m_BIsFirst)
            {
                owner.currentStateFlag |= EDragonPhaseFlag.Phase2;
                _EffectManager.GetMeshEffect(_EffectManager.DragonMesh, _DragonController.transform.position,
                    _DragonController.gameObject);
                machine.animator.SetBool(m_Phase2,true);
                m_BIsFirst = false;
            }

            _SkillManager.FindSkill(typeof(G_Dragon_Phase2)).BIsActive = false;
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
            yield return owner.StartCoroutine(machine.WaitForState());
        }

        private IEnumerator MovePos()
        {
            var _position = m_DragonTr.position;

            SetLinkPos(_position, m_PatternPos);
            m_Link.startPoint += owner.transform.forward;
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
        }

        private IEnumerator Pattern()
        {
            m_UpPos.SetActive(true);
            m_BackPos.SetActive(true);
            yield return m_Pattern;
        }

        private IEnumerator PatternEnd()
        {
            var _startPos = m_DragonTr.position;
            var _endPos = _PlayerController.transform.position;

            SetLinkPos(_startPos, _endPos);
            owner.nav.SetDestination(_endPos);

            var _dis = (_endPos - _startPos).sqrMagnitude;
            while (_dis >= 4f)
            {
                _dis = (_endPos - m_DragonTr.position).sqrMagnitude;
                yield return null;
            }

            owner.nav.velocity = Vector3.zero;
            owner.nav.ResetPath();
            var _effectPos = m_DragonTr.position;
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, _effectPos, null,
                new WaitForSeconds(3.0f));
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, _effectPos, null,
                new WaitForSeconds(5.0f), null, m_DragonTr);
            machine.animator.SetTrigger(m_PatternHash);
        }
    }
}