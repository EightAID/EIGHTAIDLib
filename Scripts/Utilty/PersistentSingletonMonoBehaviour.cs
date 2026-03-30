using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    /// <summary>
    /// Singleton base class that survives scene loads.
    /// </summary>
    public class PersistentSingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}
