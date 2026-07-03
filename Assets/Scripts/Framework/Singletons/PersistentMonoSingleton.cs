using UnityEngine;

namespace Ciga2026.Framework.Singletons
{
    public abstract class PersistentMonoSingleton<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            var hadInstance = HasInstance;

            base.Awake();

            if (!hadInstance && HasInstance)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
