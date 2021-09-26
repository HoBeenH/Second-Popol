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
                if (_Instance == null)
                {
                    _Instance = GameObject.FindObjectOfType<T>();

                    if (_Instance == null)
                    {
                        var _monoSingleton = new GameObject();
                        _Instance = _monoSingleton.AddComponent<T>();
                        _Instance.name = $"{typeof(T)} (SingleTon)";
                    }
                }

                return _Instance;
            }
        }
    }
}
