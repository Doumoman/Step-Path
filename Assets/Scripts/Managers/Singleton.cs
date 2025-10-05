using UnityEngine;

namespace Core
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("@" + typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null)
            {
                if (_instance != this)
                {
                    Debug.LogWarning($"Singleton instance of {typeof(T).Name} already exists. Destroying duplicate.");
                    Destroy(gameObject);
                }
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            AfterAwake();
        }

        protected virtual void AfterAwake() { }
    }
}