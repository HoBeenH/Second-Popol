using System.Collections;
using Script.Player;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_FlyAttack : State<Dragon_Controller>
    {
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyAttack.Fly");
        private readonly int m_FlyAttackHash = Animator.StringToHash("FlyAttack");
        private readonly WaitForSeconds m_SmokeReturn = new WaitForSeconds(5.0f);
        private readonly WaitForSeconds m_FlyDelay = new WaitForSeconds(3.5f);
        private WaitUntil m_CurrentAnimIsFly;
        private readonly Collider[] m_Results = new Collider[1];
        private NavMeshLink m_Link;
        private Vector3 m_LinkPos;
        private Transform m_DragonTr;

        protected override void Init()
        {
            var _find = GameObject.FindGameObjectWithTag("Pos").GetComponentsInChildren<Transform>();
            foreach (var link in _find)
            {
                if (link.name.Equals("Link"))
                {
                    m_Link = link.GetComponent<NavMeshLink>();
                    m_LinkPos = m_Link.transform.position;
                }
            }

            m_DragonTr = owner.GetComponent<Transform>();
            _SkillManager.AddSkill(typeof(G_Dragon_FlyAttack), 20f);
            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.nav.autoTraverseOffMeshLink = true;
            owner.currentStateFlag |= EDragonPhaseFlag.CantParry;
            owner.currentStateFlag |= EDragonPhaseFlag.Fly;
            owner.StartCoroutine(FlyAttack());
        }

        public override void OnStateExit()
        {
            owner.nav.autoTraverseOffMeshLink = false;
            m_Link.gameObject.SetActive(false);
        }

        private void SetLinkPos(Vector3 from, Vector3 to)
        {
            m_Link.gameObject.SetActive(true);
            m_Link.startPoint = from - m_LinkPos;
            m_Link.endPoint = to - m_LinkPos;
        }

        private IEnumerator FlyAttack()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);

            yield return owner.StartCoroutine(Fly());
            yield return owner.StartCoroutine(FallDown());
            yield return owner.StartCoroutine(Damage());
            yield return owner.StartCoroutine(machine.WaitForState());
            owner.currentStateFlag &= ~EDragonPhaseFlag.CantParry;
            owner.currentStateFlag &= ~EDragonPhaseFlag.Fly;
        }

        private IEnumerator Fly()
        {
            var _pos = m_DragonTr.position;
            var _endPos = _pos;
            _endPos.y = 30f;
            SetLinkPos(_pos, _endPos);
            yield return m_CurrentAnimIsFly;
            owner.nav.SetDestination(_endPos);
            yield return m_FlyDelay;
        }
        public override void OnStateUpdate()
        {
            var temp = machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash;
            Debug.Log(temp);
        }

        private IEnumerator FallDown()
        {
            machine.animator.SetTrigger(m_FlyAttackHash);
            var _pos = m_DragonTr.position;
            var _endPos = _PlayerController.transform.position;
            SetLinkPos(_pos, _endPos);
            owner.nav.speed += 5;
            owner.nav.SetDestination(_endPos);
            var _dis = (_endPos - _pos).sqrMagnitude;
            while (_dis >= 10f)
            {
                _dis = (_endPos - m_DragonTr.position).sqrMagnitude;
                yield return null;
            }

            owner.nav.speed -= 5;
            owner.nav.ResetPath();
            owner.nav.velocity = Vector3.zero;
            machine.animator.SetTrigger(m_FlyAttackHash);
        }

        private IEnumerator Damage()
        {
            var _position = m_DragonTr.position;
            _position.y = 1f;
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke, _position, null, m_SmokeReturn);
            _EffectManager.GetEffect(EPrefabName.DragonDownSmoke2, _position, null, m_SmokeReturn, null,
                owner.transform);

            var _radius = owner.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2) ? 10f : 5f;

            var _size = Physics.OverlapSphereNonAlloc(_position, _radius, m_Results, owner.playerMask);
            if (_size == 0)
            {
                yield break;
            }

            _PlayerController.TakeDamage(owner.DragonStat.damage,
                (_PlayerController.transform.position - m_DragonTr.position).normalized);
        }
    }
}