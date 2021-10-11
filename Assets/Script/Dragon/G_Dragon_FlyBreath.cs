using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static Script.Facade;

namespace Script.Dragon
{
    public class G_Dragon_FlyBreath : State<Dragon_Controller>
    {
        private GameObject m_FlyPos;
        private NavMeshLink m_Link;
        private Vector3 m_LinkPos;
        private Vector3 m_CurrentPos;
        private readonly Collider[] m_Result = new Collider[1];
        private readonly WaitForSeconds m_ForceReturn = new WaitForSeconds(8f);
        private readonly WaitForSeconds m_ForceDelay = new WaitForSeconds(0.3f);
        private readonly WaitForSeconds m_BreathDelay = new WaitForSeconds(2.5f);
        private readonly int m_BreathHash = Animator.StringToHash("HeadFire");
        private readonly int m_FlyHash = Animator.StringToHash("FlyBreath");
        private readonly int m_FlyAnimHash = Animator.StringToHash("Base Layer.FlyBreath.Fly");
        private WaitUntil m_CurrentAnimIsFly;


        protected override void Init()
        {
            var _find = GameObject.FindWithTag("Pos").GetComponentsInChildren<Transform>();
            foreach (var child in _find)
            {
                if (child.name.Equals("FlyPos"))
                {
                    m_FlyPos = child.gameObject;
                }

                if (child.name.Equals("Link"))
                {
                    m_Link = child.GetComponent<NavMeshLink>();
                    m_LinkPos = child.position;
                }
            }

            m_CurrentAnimIsFly = new WaitUntil(() =>
                machine.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_FlyAnimHash);
        }

        public override void OnStateEnter()
        {
            owner.nav.speed -= 2;
            owner.nav.autoTraverseOffMeshLink = true;
            owner.StartCoroutine(FlyBreath());
        }

        public override void OnStateExit()
        {
            owner.nav.speed += 2;
            m_Link.gameObject.SetActive(false);
            owner.nav.autoTraverseOffMeshLink = false;
        }

        private void Phase2(Vector3 pos)
        {
            _EffectManager.GetEffect(EPrefabName.BreathForce, pos, null, m_ForceReturn, m_ForceDelay);
            var _size = Physics.OverlapSphereNonAlloc(pos, 10f, m_Result, owner.playerMask);
            if (_size != 0)
            {
                _PlayerController.useFallDown.Invoke((_PlayerController.transform.position - pos).normalized, 5f);
            }
        }

        private void SetLinkPos(Vector3 from, Vector3 to)
        {
            m_Link.gameObject.SetActive(true);
            m_Link.startPoint = from - m_LinkPos;
            m_Link.endPoint = to - m_LinkPos;
        }

        private IEnumerator FlyBreath()
        {
            machine.animator.SetTrigger(m_FlyHash);
            yield return owner.StartCoroutine(Fly());
            yield return owner.StartCoroutine(Breath());
            yield return owner.StartCoroutine(EndBreath());
            yield return machine.WaitForState();
        }

        private IEnumerator Fly()
        {
            m_CurrentPos = owner.transform.position;

            if (owner.currentStateFlag.HasFlag(EDragonPhaseFlag.Phase2))
            {
                Phase2(m_CurrentPos);
            }

            var _flyPos = m_CurrentPos;
            _flyPos.y = 5f;
            m_FlyPos.transform.position = _flyPos;
            SetLinkPos(m_CurrentPos, _flyPos);
            yield return m_CurrentAnimIsFly;
            owner.nav.SetDestination(_flyPos);
            while (owner.transform.position != _flyPos)
            {
                yield return null;
            }
        }

        private IEnumerator Breath()
        {
            machine.animator.SetLayerWeight(1, 1);
            machine.animator.SetTrigger(m_BreathHash);
            _EffectManager.DragonFlyBreath(true);
            var _timer = 0f;
            while (true)
            {
                // owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation,
                //     Quaternion.LookRotation(_PlayerController.transform.position),Time.deltaTime);
                owner.transform.LookAt(_PlayerController.transform);
                _timer += Time.deltaTime;
                if (_timer >= 7f)
                {
                    break;
                }

                yield return null;
            }

            _EffectManager.DragonFlyBreath(false);
            machine.animator.SetLayerWeight(1, 0);
        }

        private IEnumerator EndBreath()
        {
            SetLinkPos(owner.transform.position, m_CurrentPos);
            owner.nav.SetDestination(m_CurrentPos);
            while (true)
            {
                var _dis = (m_CurrentPos - owner.transform.position).sqrMagnitude;
                if (_dis <= 4f)
                {
                    break;
                }

                yield return null;
            }

            machine.animator.SetTrigger(m_FlyHash);
        }
    }
}