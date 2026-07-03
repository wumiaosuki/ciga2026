using UnityEngine;

namespace Ciga2026.Framework.Utilities
{
    public static class ComponentExtensions
    {
        public static bool TryGetOrAddComponent<T>(this GameObject gameObject, out T component) where T : Component
        {
            if (gameObject.TryGetComponent(out component))
            {
                return false;
            }

            component = gameObject.AddComponent<T>();
            return true;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();
        }
    }
}
