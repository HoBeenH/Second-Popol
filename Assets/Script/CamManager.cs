using Cinemachine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Script
{
    // 카메라 관리
    public class CamManager : MonoSingleton<CamManager>
    {
        public CinemachineImpulseSource PlayerWeapon;
        public CinemachineImpulseSource Boom;
    }
}
