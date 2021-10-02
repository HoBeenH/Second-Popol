using Script.Dragon;
using Script.Player;
using Script.Player.Effect;
using UnityEngine;

namespace Script
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _Instance;
        
        public static T Instance
        {
            get
            {
                if (_Instance != null) 
                    return _Instance;
                
                _Instance = GameObject.FindObjectOfType<T>();
                if (_Instance != null) 
                    return _Instance;

                var _monoSingleton = new GameObject();
                _Instance = _monoSingleton.AddComponent<T>();
                _Instance.name = $"{typeof(T)} (SingleTon)";

                return _Instance;
            }
        }
    }

    // 가독성 및 관리용 파사드 클래스
    public static class Facade
    {
        public static PlayerController _PlayerController => PlayerController.Instance;
        public static DragonController _DragonController => DragonController.Instance;
        public static DragonPhaseManager _DragonPhaseManager => DragonPhaseManager.Instance;
        public static ObjPool _ObjPool => ObjPool.Instance;
        public static EffectManager _EffectManager => EffectManager.Instance;
        public static CamManager _CamManager => CamManager.Instance;
    }
}