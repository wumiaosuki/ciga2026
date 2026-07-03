using UnityEngine;

namespace Ciga2026.Framework.UI
{
    /// <summary>
    /// UI 资源映射项。
    /// </summary>
    [System.Serializable]
    public sealed class UIResourceEntry
    {
        [Tooltip("UI 资源枚举 ID，用于代码中定位对应 prefab。")]
        [SerializeField]
        private UIResourceType resourceType;

        [Tooltip("该 UI 枚举对应的 prefab 资源。")]
        [SerializeField]
        private GameObject prefab;

        /// <summary>
        /// UI 资源枚举 ID。
        /// </summary>
        public UIResourceType ResourceType => resourceType;

        /// <summary>
        /// UI prefab 资源。
        /// </summary>
        public GameObject Prefab => prefab;
    }
}
