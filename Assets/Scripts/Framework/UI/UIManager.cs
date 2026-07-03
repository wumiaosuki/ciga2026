using System.Collections.Generic;
using Ciga2026.Framework.Singletons;
using UnityEngine;

namespace Ciga2026.Framework.UI
{
    /// <summary>
    /// 全局 UI 管理器。
    /// 负责加载 UI prefab、注册面板实例、管理显示隐藏、层级顺序和释放。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ciga2026/Framework/UI Manager")]
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        [Header("默认父节点")]
        [SerializeField]
        [Tooltip("所有 UI 层级根节点的父节点。通常绑定到 Canvas 的 RectTransform。未绑定时使用当前 UIManager 的 Transform。")]
        private Transform defaultRoot;

        [Header("层级根节点")]
        [SerializeField]
        [Tooltip("背景层根节点。适合放全屏背景、底层装饰。未绑定时会自动创建。")]
        private Transform backgroundRoot;

        [SerializeField]
        [Tooltip("普通层根节点。适合放大多数常规界面。未绑定时会自动创建。")]
        private Transform normalRoot;

        [SerializeField]
        [Tooltip("弹窗层根节点。适合放确认框、背包弹窗等。未绑定时会自动创建。")]
        private Transform popupRoot;

        [SerializeField]
        [Tooltip("覆盖层根节点。适合放 loading、遮罩、全屏提示。未绑定时会自动创建。")]
        private Transform overlayRoot;

        [SerializeField]
        [Tooltip("系统层根节点。适合放断线提示、调试面板等最高优先级 UI。未绑定时会自动创建。")]
        private Transform systemRoot;

        [Header("UI 资源映射")]
        [SerializeField]
        [Tooltip("UI 枚举到 prefab 的映射列表。代码可通过枚举打开对应 UI。")]
        private List<UIResourceEntry> entrys = new();

        [Header("面板行为")]
        [SerializeField]
        [Tooltip("打开面板时是否默认将面板放到同层级最上方。")]
        private bool bringToFrontOnOpen = true;

        [SerializeField]
        [Tooltip("释放面板时是否调用 Destroy。关闭该选项后 ReleasePanel 只会取消注册并隐藏对象。")]
        private bool destroyOnRelease = true;

        [Header("默认绑定")]
        [Tooltip("场景中默认已存在、无需通过 UIManager 加载的 UI 绑定脚本。例如开场 StartMeau。")]
        public UIBind DefaultBind;

        private readonly Dictionary<string, GameObject> panelsById = new();
        private readonly Dictionary<GameObject, string> idsByPanel = new();
        private readonly Dictionary<string, UIBind> bindsById = new();
        private readonly Dictionary<UIBind, string> idsByBind = new();
        private readonly Dictionary<UIResourceType, GameObject> prefabsByResourceType = new();

        /// <summary>
        /// 所有已注册面板的只读视图。
        /// </summary>
        public IReadOnlyDictionary<string, GameObject> Panels => panelsById;

        /// <summary>
        /// 所有已注册 UI 绑定脚本的只读视图。
        /// </summary>
        public IReadOnlyDictionary<string, UIBind> Binds => bindsById;

        /// <summary>
        /// UI 资源映射列表。
        /// </summary>
        public IReadOnlyList<UIResourceEntry> Entrys => entrys;

        /// <summary>
        /// 初始化 UIManager 并确保默认层级根节点存在。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            RebuildEntryCache();
            EnsureLayerRoots();
            RegisterDefaultBind();
        }

        /// <summary>
        /// 根据 UI 资源枚举打开面板。
        /// </summary>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <param name="parent">指定父节点。传 null 时使用 layer 对应的默认层级根节点。</param>
        /// <param name="panelId">面板唯一 ID。传空时默认使用枚举名。</param>
        /// <param name="layer">面板所在 UI 层级；仅在 parent 为空时决定默认父节点。</param>
        /// <param name="reuseExisting">同 ID 面板已存在时是否复用并显示它。</param>
        /// <returns>打开后的面板 GameObject；失败时返回 null。</returns>
        public GameObject OpenPanel(UIResourceType resourceType, Transform parent = null, string panelId = null, UILayer layer = UILayer.Normal, bool reuseExisting = true)
        {
            if (!TryGetPrefab(resourceType, out var prefab))
            {
                Debug.LogWarning($"UIManager.OpenPanel failed: prefab for '{resourceType}' is not mapped.");
                return null;
            }

            var id = string.IsNullOrWhiteSpace(panelId) ? resourceType.ToString() : panelId;
            return OpenPanel(prefab, parent, id, layer, reuseExisting);
        }

        /// <summary>
        /// 根据 UI 资源枚举打开面板，并返回指定组件类型。
        /// </summary>
        /// <typeparam name="TPanel">面板上需要获取的组件类型，通常是继承 UIPanel 的脚本。</typeparam>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <param name="parent">指定父节点。传 null 时使用 layer 对应的默认层级根节点。</param>
        /// <param name="panelId">面板唯一 ID。传空时默认使用枚举名。</param>
        /// <param name="layer">面板所在 UI 层级。</param>
        /// <param name="reuseExisting">同 ID 面板已存在时是否复用并显示它。</param>
        /// <returns>面板上的 TPanel 组件；不存在时返回 null。</returns>
        public TPanel OpenPanel<TPanel>(UIResourceType resourceType, Transform parent = null, string panelId = null, UILayer layer = UILayer.Normal, bool reuseExisting = true) where TPanel : Component
        {
            var panel = OpenPanel(resourceType, parent, panelId, layer, reuseExisting);
            return panel != null && panel.TryGetComponent<TPanel>(out var component) ? component : null;
        }

        /// <summary>
        /// 打开一个 UI prefab，并把实例注册到 UIManager。
        /// </summary>
        /// <param name="prefab">要实例化的 UI prefab。为空时返回 null。</param>
        /// <param name="parent">指定父节点。传 null 时使用 layer 对应的默认层级根节点。</param>
        /// <param name="panelId">面板唯一 ID。传空时默认使用 prefab.name。</param>
        /// <param name="layer">面板所在 UI 层级；仅在 parent 为空时决定默认父节点。</param>
        /// <param name="reuseExisting">同 ID 面板已存在时是否复用并显示它。</param>
        /// <returns>打开后的面板 GameObject；失败时返回 null。</returns>
        public GameObject OpenPanel(GameObject prefab, Transform parent = null, string panelId = null, UILayer layer = UILayer.Normal, bool reuseExisting = true)
        {
            if (prefab == null)
            {
                Debug.LogWarning("UIManager.OpenPanel failed: prefab is null.");
                return null;
            }

            var id = string.IsNullOrWhiteSpace(panelId) ? prefab.name : panelId;

            if (reuseExisting && panelsById.TryGetValue(id, out var existingPanel) && existingPanel != null)
            {
                ShowPanel(id, true);

                if (bringToFrontOnOpen)
                {
                    BringToFront(id);
                }

                return existingPanel;
            }

            var targetParent = parent != null ? parent : GetLayerRoot(layer);
            var panel = Instantiate(prefab, targetParent, false);
            panel.name = id;

            RegisterPanel(id, panel, layer);
            ShowPanel(id, true);

            if (bringToFrontOnOpen)
            {
                BringToFront(id);
            }

            return panel;
        }

        /// <summary>
        /// 打开一个 UI prefab，并返回指定组件类型。
        /// </summary>
        /// <typeparam name="TPanel">面板上需要获取的组件类型，通常是继承 UIPanel 的脚本。</typeparam>
        /// <param name="prefab">要实例化的 UI prefab。</param>
        /// <param name="parent">指定父节点。传 null 时使用 layer 对应的默认层级根节点。</param>
        /// <param name="panelId">面板唯一 ID。传空时默认使用 prefab.name。</param>
        /// <param name="layer">面板所在 UI 层级。</param>
        /// <param name="reuseExisting">同 ID 面板已存在时是否复用并显示它。</param>
        /// <returns>面板上的 TPanel 组件；不存在时返回 null。</returns>
        public TPanel OpenPanel<TPanel>(GameObject prefab, Transform parent = null, string panelId = null, UILayer layer = UILayer.Normal, bool reuseExisting = true) where TPanel : Component
        {
            var panel = OpenPanel(prefab, parent, panelId, layer, reuseExisting);
            return panel != null && panel.TryGetComponent<TPanel>(out var component) ? component : null;
        }

        /// <summary>
        /// 尝试获取指定 UI 资源枚举对应的 prefab。
        /// </summary>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <param name="prefab">找到的 prefab；未找到时为 null。</param>
        /// <returns>成功找到有效 prefab 时返回 true。</returns>
        public bool TryGetPrefab(UIResourceType resourceType, out GameObject prefab)
        {
            if (prefabsByResourceType.Count != entrys.Count)
            {
                RebuildEntryCache();
            }

            return prefabsByResourceType.TryGetValue(resourceType, out prefab) && prefab != null;
        }

        /// <summary>
        /// 获取指定 UI 资源枚举对应的 prefab。
        /// </summary>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <returns>找到的 prefab；未配置时返回 null。</returns>
        public GameObject GetPrefab(UIResourceType resourceType)
        {
            return TryGetPrefab(resourceType, out var prefab) ? prefab : null;
        }

        /// <summary>
        /// 注册一个已经存在于场景中的面板对象。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。后续通过该 ID 查找、显示、隐藏和释放。</param>
        /// <param name="panel">要注册的面板 GameObject。</param>
        /// <param name="layer">面板所在 UI 层级。</param>
        /// <param name="moveToLayerRoot">是否把面板移动到对应层级根节点下。</param>
        public void RegisterPanel(string panelId, GameObject panel, UILayer layer = UILayer.Normal, bool moveToLayerRoot = false)
        {
            if (string.IsNullOrWhiteSpace(panelId) || panel == null)
            {
                Debug.LogWarning("UIManager.RegisterPanel failed: panelId or panel is invalid.");
                return;
            }

            if (panelsById.TryGetValue(panelId, out var oldPanel) && oldPanel != null && oldPanel != panel)
            {
                ReleasePanel(panelId);
            }

            panelsById[panelId] = panel;
            idsByPanel[panel] = panelId;

            if (moveToLayerRoot)
            {
                panel.transform.SetParent(GetLayerRoot(layer), false);
            }

            if (panel.TryGetComponent<UIPanel>(out var uiPanel))
            {
                uiPanel.Initialize(panelId, layer);
            }

            var uiBind = panel.GetComponentInChildren<UIBind>(true);

            if (uiBind != null)
            {
                RegisterBind(panelId, uiBind, layer);
            }
            else
            {
                RemoveBind(panelId);
            }
        }

        /// <summary>
        /// 注册一个已经存在于场景中的 UI 绑定脚本。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <param name="bind">要注册的绑定脚本。</param>
        /// <param name="layer">绑定对象所在 UI 层级。</param>
        public void RegisterBind(string panelId, UIBind bind, UILayer layer = UILayer.Normal)
        {
            if (string.IsNullOrWhiteSpace(panelId) || bind == null)
            {
                Debug.LogWarning("UIManager.RegisterBind failed: panelId or bind is invalid.");
                return;
            }

            bindsById[panelId] = bind;
            idsByBind[bind] = panelId;
            bind.Initialize(panelId, layer);
        }

        /// <summary>
        /// 判断指定 ID 的面板是否已注册。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        public bool HasPanel(string panelId)
        {
            return !string.IsNullOrWhiteSpace(panelId) && panelsById.ContainsKey(panelId);
        }

        /// <summary>
        /// 获取指定 ID 的面板对象。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <returns>面板 GameObject；不存在或已被销毁时返回 null。</returns>
        public GameObject GetPanel(string panelId)
        {
            return !string.IsNullOrWhiteSpace(panelId) && panelsById.TryGetValue(panelId, out var panel) ? panel : null;
        }

        /// <summary>
        /// 获取指定 ID 面板上的组件。
        /// </summary>
        /// <typeparam name="TPanel">要获取的组件类型。</typeparam>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <returns>目标组件；不存在时返回 null。</returns>
        public TPanel GetPanel<TPanel>(string panelId) where TPanel : Component
        {
            var panel = GetPanel(panelId);
            return panel != null && panel.TryGetComponent<TPanel>(out var component) ? component : null;
        }

        /// <summary>
        /// 获取指定 ID 的 UI 绑定脚本。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <returns>绑定脚本；不存在或已被销毁时返回 null。</returns>
        public UIBind GetBind(string panelId)
        {
            return !string.IsNullOrWhiteSpace(panelId) && bindsById.TryGetValue(panelId, out var bind) ? bind : null;
        }

        /// <summary>
        /// 获取指定 UI 资源枚举对应的绑定脚本。
        /// </summary>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <returns>绑定脚本；不存在或已被销毁时返回 null。</returns>
        public UIBind GetBind(UIResourceType resourceType)
        {
            return GetBind(resourceType.ToString());
        }

        /// <summary>
        /// 获取指定 ID 的 UI 绑定脚本。
        /// </summary>
        /// <typeparam name="TBind">绑定脚本类型。</typeparam>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <returns>目标绑定脚本；不存在或类型不匹配时返回 null。</returns>
        public TBind GetBind<TBind>(string panelId) where TBind : UIBind
        {
            return GetBind(panelId) as TBind;
        }

        /// <summary>
        /// 获取指定 UI 资源枚举对应的绑定脚本。
        /// </summary>
        /// <typeparam name="TBind">绑定脚本类型。</typeparam>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <returns>目标绑定脚本；不存在或类型不匹配时返回 null。</returns>
        public TBind GetBind<TBind>(UIResourceType resourceType) where TBind : UIBind
        {
            return GetBind<TBind>(resourceType.ToString());
        }

        /// <summary>
        /// 尝试获取指定 ID 的 UI 绑定脚本。
        /// </summary>
        /// <typeparam name="TBind">绑定脚本类型。</typeparam>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <param name="bind">找到的绑定脚本；未找到时为 null。</param>
        /// <returns>成功找到且类型匹配时返回 true。</returns>
        public bool TryGetBind<TBind>(string panelId, out TBind bind) where TBind : UIBind
        {
            bind = GetBind<TBind>(panelId);
            return bind != null;
        }

        /// <summary>
        /// 尝试获取指定 UI 资源枚举对应的 UI 绑定脚本。
        /// </summary>
        /// <typeparam name="TBind">绑定脚本类型。</typeparam>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <param name="bind">找到的绑定脚本；未找到时为 null。</param>
        /// <returns>成功找到且类型匹配时返回 true。</returns>
        public bool TryGetBind<TBind>(UIResourceType resourceType, out TBind bind) where TBind : UIBind
        {
            return TryGetBind(resourceType.ToString(), out bind);
        }

        /// <summary>
        /// 显示或隐藏指定面板。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <param name="visible">true 为显示，false 为隐藏。</param>
        public void ShowPanel(string panelId, bool visible)
        {
            var panel = GetPanel(panelId);

            if (panel == null)
            {
                return;
            }

            if (panel.activeSelf == visible)
            {
                return;
            }

            panel.SetActive(visible);

            if (panel.TryGetComponent<UIPanel>(out var uiPanel))
            {
                if (visible)
                {
                    uiPanel.OnShow();
                }
                else
                {
                    uiPanel.OnHide();
                }
            }
        }

        /// <summary>
        /// 显示或隐藏指定 UI 资源枚举对应的面板。
        /// </summary>
        /// <param name="resourceType">UI 资源枚举。</param>
        /// <param name="visible">true 为显示，false 为隐藏。</param>
        public void ShowPanel(UIResourceType resourceType, bool visible)
        {
            ShowPanel(resourceType.ToString(), visible);
        }

        /// <summary>
        /// 将指定面板移动到新的 UI 层级。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <param name="layer">目标 UI 层级。</param>
        /// <param name="asTop">是否移动到目标层级最上方。</param>
        public void MoveToLayer(string panelId, UILayer layer, bool asTop = true)
        {
            var panel = GetPanel(panelId);

            if (panel == null)
            {
                return;
            }

            panel.transform.SetParent(GetLayerRoot(layer), false);

            if (asTop)
            {
                panel.transform.SetAsLastSibling();
            }

            if (panel.TryGetComponent<UIPanel>(out var uiPanel))
            {
                uiPanel.SetLayer(layer);
            }

            if (bindsById.TryGetValue(panelId, out var uiBind) && uiBind != null)
            {
                uiBind.SetLayer(layer);
            }
        }

        /// <summary>
        /// 将指定面板放到当前父节点下的最上方。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        public void BringToFront(string panelId)
        {
            var panel = GetPanel(panelId);

            if (panel != null)
            {
                panel.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 将指定面板放到当前父节点下的最下方。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        public void SendToBack(string panelId)
        {
            var panel = GetPanel(panelId);

            if (panel != null)
            {
                panel.transform.SetAsFirstSibling();
            }
        }

        /// <summary>
        /// 释放指定面板。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <returns>成功找到并释放时返回 true。</returns>
        public bool ReleasePanel(string panelId)
        {
            var panel = GetPanel(panelId);

            if (panel == null)
            {
                panelsById.Remove(panelId);
                RemoveBind(panelId);
                return false;
            }

            if (panel.TryGetComponent<UIPanel>(out var uiPanel))
            {
                uiPanel.OnRelease();
            }

            panelsById.Remove(panelId);
            idsByPanel.Remove(panel);

            RemoveBind(panelId);

            if (destroyOnRelease)
            {
                Destroy(panel);
            }
            else
            {
                panel.SetActive(false);
            }

            return true;
        }

        /// <summary>
        /// 释放指定面板对象。
        /// </summary>
        /// <param name="panel">面板 GameObject。</param>
        /// <returns>成功找到并释放时返回 true。</returns>
        public bool ReleasePanel(GameObject panel)
        {
            if (panel == null || !idsByPanel.TryGetValue(panel, out var panelId))
            {
                return false;
            }

            return ReleasePanel(panelId);
        }

        /// <summary>
        /// 取消注册指定 UI 绑定脚本。
        /// </summary>
        /// <param name="bind">绑定脚本。</param>
        /// <returns>成功找到并取消注册时返回 true。</returns>
        public bool UnregisterBind(UIBind bind)
        {
            if (bind == null || !idsByBind.TryGetValue(bind, out var panelId))
            {
                return false;
            }

            idsByBind.Remove(bind);
            bindsById.Remove(panelId);
            return true;
        }

        /// <summary>
        /// 按面板 ID 清理 UI 绑定缓存。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        private void RemoveBind(string panelId)
        {
            if (bindsById.TryGetValue(panelId, out var bind) && bind != null)
            {
                idsByBind.Remove(bind);
            }

            bindsById.Remove(panelId);
        }

        /// <summary>
        /// 注册场景中默认已存在的 UI 绑定对象。
        /// </summary>
        private void RegisterDefaultBind()
        {
            if (DefaultBind == null)
            {
                return;
            }

            RegisterPanel(DefaultBind.gameObject.name, DefaultBind.gameObject, UILayer.Normal, false);
        }

        /// <summary>
        /// 释放所有已注册面板。
        /// </summary>
        public void ReleaseAllPanels()
        {
            var panelIds = new List<string>(panelsById.Keys);

            for (var i = 0; i < panelIds.Count; i++)
            {
                ReleasePanel(panelIds[i]);
            }
        }

        /// <summary>
        /// 隐藏所有已注册面板，但不释放对象。
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var panelId in panelsById.Keys)
            {
                ShowPanel(panelId, false);
            }
        }

        /// <summary>
        /// 获取指定 UI 层级的根节点。
        /// </summary>
        /// <param name="layer">目标 UI 层级。</param>
        /// <returns>该层级对应的 Transform。</returns>
        public Transform GetLayerRoot(UILayer layer)
        {
            EnsureLayerRoots();

            return layer switch
            {
                UILayer.Background => backgroundRoot,
                UILayer.Popup => popupRoot,
                UILayer.Overlay => overlayRoot,
                UILayer.System => systemRoot,
                _ => normalRoot
            };
        }

        /// <summary>
        /// 确保默认父节点和所有层级根节点存在。
        /// </summary>
        private void EnsureLayerRoots()
        {
            defaultRoot = defaultRoot != null ? defaultRoot : transform;

            backgroundRoot = EnsureLayerRoot(backgroundRoot, "Background Layer", 0);
            normalRoot = EnsureLayerRoot(normalRoot, "Normal Layer", 1);
            popupRoot = EnsureLayerRoot(popupRoot, "Popup Layer", 2);
            overlayRoot = EnsureLayerRoot(overlayRoot, "Overlay Layer", 3);
            systemRoot = EnsureLayerRoot(systemRoot, "System Layer", 4);
        }

        /// <summary>
        /// 创建或整理单个层级根节点。
        /// </summary>
        /// <param name="root">Inspector 中绑定的根节点。为空时自动创建。</param>
        /// <param name="name">自动创建时使用的对象名称。</param>
        /// <param name="siblingIndex">根节点在 defaultRoot 下的排序位置。</param>
        /// <returns>最终可用的层级根节点。</returns>
        private Transform EnsureLayerRoot(Transform root, string name, int siblingIndex)
        {
            if (root == null)
            {
                var rootObject = new GameObject(name, typeof(RectTransform));
                root = rootObject.transform;
            }

            if (root.parent != defaultRoot)
            {
                root.SetParent(defaultRoot, false);
            }

            root.SetSiblingIndex(siblingIndex);
            StretchToParent(root as RectTransform);

            return root;
        }

        /// <summary>
        /// 重建 UI 资源映射缓存。
        /// </summary>
        private void RebuildEntryCache()
        {
            prefabsByResourceType.Clear();

            for (var i = 0; i < entrys.Count; i++)
            {
                var entry = entrys[i];

                if (entry == null || entry.ResourceType == UIResourceType.None || entry.Prefab == null)
                {
                    continue;
                }

                prefabsByResourceType[entry.ResourceType] = entry.Prefab;
            }
        }

        /// <summary>
        /// 将 RectTransform 拉伸到父节点大小。
        /// </summary>
        /// <param name="rectTransform">需要拉伸的 RectTransform。非 UI Transform 会被忽略。</param>
        private void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
