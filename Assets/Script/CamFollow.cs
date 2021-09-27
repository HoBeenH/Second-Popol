using Script.Player;
using UnityEngine;

namespace Script
{
    public class CamFollow : MonoBehaviour
    {
        [SerializeField] private GameObject defaultCam;
        [SerializeField] private GameObject actionCam;
        public Vector3 camOffset;
        public float smoothTime = 5f;
        public float rotSpeed = 3f;
        private Transform m_TargetPos;

        private void Awake()
        {
            PlayerController.Instance.useDefaultCam = () =>
            {
                defaultCam.SetActive(true);
                actionCam.SetActive(false);
            };
            PlayerController.Instance.useActionCam = () =>
            {
                actionCam.SetActive(true);
                defaultCam.SetActive(false);
            };

            m_TargetPos = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Update()
        {
            LookAround();
        }

        private void LateUpdate()
        {
            CamFollowSet();
        }

        private void CamFollowSet()
        {
            var _position = transform.position;
            var _nextPos = m_TargetPos.TransformPoint(camOffset);
            _position = Vector3.Lerp(_position, _nextPos, smoothTime * Time.deltaTime);
            transform.position = _position;
        }

        private void LookAround()
        {
            var _mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotSpeed;
            var _camAngle = transform.rotation.eulerAngles;
            var _clampX = _camAngle.x - _mouseDelta.y;
            var _x = _clampX < 180f
                ? Mathf.Clamp(_clampX, -1f, 0f)
                : Mathf.Clamp(_clampX, 300f, 360f);

            this.transform.rotation = Quaternion.Euler(_x, _camAngle.y + _mouseDelta.x, _camAngle.z);
        }
    }
}