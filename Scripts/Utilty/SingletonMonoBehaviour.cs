using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    /// <summary>
    /// Singleton base class for MonoBehaviours.
    /// </summary>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                return;
            }

            Destroy(gameObject);
        }
    }
}
