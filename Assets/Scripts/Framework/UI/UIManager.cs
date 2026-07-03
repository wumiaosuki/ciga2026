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
    public sealed class UIManager : PersistentMonoSingleton<UIManager>
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

        [Header("面板行为")]
        [SerializeField]
        [Tooltip("打开面板时是否默认将面板放到同层级最上方。")]
        private bool bringToFrontOnOpen = true;

        [SerializeField]
        [Tooltip("释放面板时是否调用 Destroy。关闭该选项后 ReleasePanel 只会取消注册并隐藏对象。")]
        private bool destroyOnRelease = true;

        private readonly Dictionary<string, GameObject> panelsById = new();
        private readonly Dictionary<GameObject, string> idsByPanel = new();

        /// <summary>
        /// 所有已注册面板的只读视图。
        /// </summary>
        public IReadOnlyDictionary<string, GameObject> Panels => panelsById;

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

            EnsureLayerRoots();
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
                return false;
            }

            if (panel.TryGetComponent<UIPanel>(out var uiPanel))
            {
                uiPanel.OnRelease();
            }

            panelsById.Remove(panelId);
            idsByPanel.Remove(panel);

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
