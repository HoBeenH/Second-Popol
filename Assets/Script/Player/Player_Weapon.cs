using Cinemachine;
using UnityEngine;
using static Script.Facade;
using Random = Unity.Mathematics;

namespace Script.Player
{
    public class Player_Weapon : MonoBehaviour
    {
        private CinemachineImpulseSource m_Source;
        private Collider m_Collider;
        private readonly WaitForSeconds m_BloodReturn = new WaitForSeconds(15.0f);
        [SerializeField] private Transform m_RayPos;

        private void Awake()
        {
            m_Collider = GetComponent<BoxCollider>();
            m_Source = Camera.main.gameObject.GetComponent<CinemachineImpulseSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Dragon"))
            {
                m_Collider.enabled = false;
                _DragonController.TakeDamage(_PlayerController.PlayerStat.damage);
                m_Source.GenerateImpulse();
                SetBlood();
            }
        }

        private void SetBlood()
        {
            Physics.Raycast(m_RayPos.position, m_RayPos.up, out var hit, 2f, 1 << 11);
            var _dir = hit.normal;
            var _angle = Mathf.Atan2(_dir.x, _dir.z) * Mathf.Rad2Deg + 180f;
            var _bloodEffect = (EPrefabName)UnityEngine.Random.Range(25, 42);
            var _blood = _EffectManager.GetEffect(_bloodEffect, hit.point, m_BloodReturn);
            _blood.transform.rotation = Quaternion.Euler(0,_angle + 90, 0);
            _blood.TryGetComponent<BFX_BloodSettings>(out var set);
            set.GroundHeight = hit.point.y;

            var _nearestBone = GetNearestBone(hit.transform.root, hit.point);
            if (_nearestBone == null)
            {
                return;
            }

            var decal =
                _EffectManager.GetEffect(EPrefabName.BloodDecal, _nearestBone.position, m_BloodReturn);
            var bloodT = decal.transform;
            bloodT.position = hit.point;
            bloodT.localRotation = Quaternion.identity;
            bloodT.LookAt(hit.point + hit.normal, _dir);
            bloodT.Rotate(90, 0, 0);
        }

        private Transform GetNearestBone(Transform characterTransform, Vector3 hitPos)
        {
            var closestPos = 10f;
            Transform closestBone = null;
            var childs = characterTransform.GetComponentsInChildren<Transform>();

            foreach (var child in childs)
            {
                var dist = Vector3.Distance(child.position, hitPos);
                if (dist < closestPos)
                {
                    closestPos = dist;
                    closestBone = child;
                }
            }

            var distRoot = Vector3.Distance(characterTransform.position, hitPos);
            if (distRoot < closestPos)
            {
                closestPos = distRoot;
                closestBone = characterTransform;
            }

            return closestBone;
        }
    }
}