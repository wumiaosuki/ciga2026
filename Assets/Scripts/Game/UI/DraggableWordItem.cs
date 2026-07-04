using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 可点击移动的词语 UI 项。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class DraggableWordItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("显示")]
        [Tooltip("词语显示文本。")]
        [SerializeField]
        private TextMeshProUGUI label;

        [Header("自适应尺寸")]
        [Tooltip("词块左右留白宽度。")]
        [SerializeField]
        [Min(0f)]
        private float horizontalPadding = 44f;

        [Tooltip("词块上下留白高度。")]
        [SerializeField]
        [Min(0f)]
        private float verticalPadding = 18f;

        [Tooltip("词块最小宽度。")]
        [SerializeField]
        [Min(1f)]
        private float minWidth = 96f;

        [Tooltip("兜底最大宽度。实际宽度还会受“每行最多字符数”和当前容器宽度限制。")]
        [SerializeField]
        [Min(1f)]
        private float maxWidth = 360f;

        [Tooltip("每行最多显示的字符数。超过后在词块内换行。")]
        [SerializeField]
        [Min(1)]
        private int maxCharactersPerLine = 8;

        [Tooltip("词块最小高度。")]
        [SerializeField]
        [Min(1f)]
        private float minHeight = 46f;

        [Header("随机抖动")]
        [Tooltip("是否启用词块上下左右随机抖动。抖动只作用在子视觉节点，不影响布局根节点。")]
        [SerializeField]
        private bool enableJitter = true;

        [Tooltip("随机抖动的最大偏移像素。")]
        [SerializeField]
        [Min(0f)]
        private float jitterAmplitude = 2f;

        [Tooltip("随机抖动变化速度。")]
        [SerializeField]
        [Min(0.01f)]
        private float jitterFrequency = 9f;

        private RectTransform rectTransform;
        private LayoutElement layoutElement;
        private RectTransform[] jitterTargets;
        private Vector2[] originalJitterPositions;
        private float jitterSeedX;
        private float jitterSeedY;
        private static int nextLayoutSlotId;

        /// <summary>
        /// 当前词语的全局 ID。
        /// </summary>
        public string WordId { get; private set; }

        /// <summary>
        /// 当前所在的投放区域。
        /// </summary>
        public WordDropZone CurrentZone { get; private set; }

        /// <summary>
        /// 词块在随机布局中的固定槽位 ID。离开词库再回来时仍使用同一个槽位。
        /// </summary>
        public int LayoutSlotId { get; private set; } = -1;

        /// <summary>
        /// 玩家点击词块时触发。
        /// </summary>
        public event System.Action<DraggableWordItem> Clicked;

        private void Awake()
        {
            EnsureCached();
            CacheJitterTargets();
        }

        private void OnEnable()
        {
            jitterSeedX = Mathf.Abs(GetInstanceID() * 0.173f) + 17.31f;
            jitterSeedY = Mathf.Abs(GetInstanceID() * 0.379f) + 41.73f;
            CacheJitterTargets();
        }

        private void OnDisable()
        {
            ResetJitterTargets();
        }

        private void LateUpdate()
        {
            UpdateJitter();
        }

        /// <summary>
        /// 初始化词块显示和所在区域。
        /// </summary>
        /// <param name="wordId">全局词语 ID。</param>
        /// <param name="displayText">显示给玩家的词语文本。</param>
        /// <param name="initialZone">初始所在投放区域。</param>
        public void Initialize(string wordId, string displayText, WordDropZone initialZone)
        {
            EnsureCached();
            WordId = wordId;
            LayoutSlotId = nextLayoutSlotId++;

            if (label != null)
            {
                label.text = displayText;
            }

            RefreshLayoutSize();

            if (initialZone != null)
            {
                AttachToZone(initialZone);
            }
        }

        /// <summary>
        /// 把词块挂到指定投放区域。
        /// </summary>
        /// <param name="zone">目标投放区域。</param>
        public void AttachToZone(WordDropZone zone)
        {
            EnsureCached();

            if (zone == null)
            {
                return;
            }

            CurrentZone = zone;
            transform.SetParent(zone.ContentRoot, false);
            transform.SetAsLastSibling();
            rectTransform.anchoredPosition = Vector2.zero;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }

            RefreshLayoutSize();
        }

        /// <summary>
        /// 在两个投放区域之间切换词块位置。
        /// </summary>
        /// <param name="firstZone">第一个投放区域，通常是词库栏。</param>
        /// <param name="secondZone">第二个投放区域，通常是输入框。</param>
        public void ToggleBetweenZones(WordDropZone firstZone, WordDropZone secondZone)
        {
            if (firstZone == null || secondZone == null)
            {
                return;
            }

            AttachToZone(CurrentZone == secondZone ? firstZone : secondZone);
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        /// <summary>
        /// 根据当前文本刷新词块参与布局的首选尺寸。
        /// </summary>
        public void RefreshLayoutSize()
        {
            RefreshLayoutSize(maxWidth);
        }

        /// <summary>
        /// 根据当前文本和外部布局区域宽度刷新词块首选尺寸。
        /// </summary>
        /// <param name="maxAllowedWidth">当前布局区域允许的最大词块宽度。</param>
        public void RefreshLayoutSize(float maxAllowedWidth)
        {
            EnsureCached();

            var effectiveMaxWidth = Mathf.Max(minWidth, Mathf.Min(maxWidth, GetCharacterLimitedWidth(), Mathf.Max(1f, maxAllowedWidth)));
            var preferredTextSize = label != null
                ? label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity)
                : Vector2.zero;
            var preferredWidth = Mathf.Clamp(preferredTextSize.x + horizontalPadding, minWidth, effectiveMaxWidth);
            var textWidthLimit = Mathf.Max(1f, preferredWidth - horizontalPadding);

            if (label != null)
            {
                label.textWrappingMode = preferredTextSize.x + horizontalPadding > effectiveMaxWidth
                    ? TextWrappingModes.Normal
                    : TextWrappingModes.NoWrap;
                preferredTextSize = label.GetPreferredValues(label.text, textWidthLimit, Mathf.Infinity);
            }

            var preferredHeight = Mathf.Max(minHeight, preferredTextSize.y + verticalPadding);

            if (layoutElement != null)
            {
                layoutElement.minWidth = minWidth;
                layoutElement.preferredWidth = preferredWidth;
                layoutElement.minHeight = minHeight;
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }

        private float GetCharacterLimitedWidth()
        {
            if (label == null || string.IsNullOrEmpty(label.text))
            {
                return maxWidth;
            }

            var lineText = GetLeadingCharacters(label.text, Mathf.Max(1, maxCharactersPerLine));
            var lineSize = label.GetPreferredValues(lineText, Mathf.Infinity, Mathf.Infinity);
            return Mathf.Max(minWidth, lineSize.x + horizontalPadding);
        }

        private static string GetLeadingCharacters(string text, int maxCharacters)
        {
            if (string.IsNullOrEmpty(text) || maxCharacters <= 0 || text.Length <= maxCharacters)
            {
                return text;
            }

            return text.Substring(0, maxCharacters);
        }

        private void EnsureCached()
        {
            rectTransform = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
            layoutElement = layoutElement != null ? layoutElement : GetComponent<LayoutElement>();
        }

        private void CacheJitterTargets()
        {
            if (jitterTargets != null && jitterTargets.Length == transform.childCount)
            {
                return;
            }

            jitterTargets = new RectTransform[transform.childCount];
            originalJitterPositions = new Vector2[transform.childCount];

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                jitterTargets[i] = child;
                originalJitterPositions[i] = child != null ? child.anchoredPosition : Vector2.zero;
            }
        }

        private void UpdateJitter()
        {
            if (!ShouldPlayJitter())
            {
                ResetJitterTargets();
                return;
            }

            if (jitterTargets == null)
            {
                return;
            }

            var time = Time.unscaledTime * jitterFrequency;
            var offset = new Vector2(
                (Mathf.PerlinNoise(jitterSeedX, time) - 0.5f) * 2f * jitterAmplitude,
                (Mathf.PerlinNoise(jitterSeedY, time) - 0.5f) * 2f * jitterAmplitude);

            for (var i = 0; i < jitterTargets.Length; i++)
            {
                if (jitterTargets[i] != null)
                {
                    jitterTargets[i].anchoredPosition = originalJitterPositions[i] + offset;
                }
            }
        }

        private bool ShouldPlayJitter()
        {
            return enableJitter
                && jitterAmplitude > 0f
                && CurrentZone != null
                && CurrentZone.AllowWordItemJitter;
        }

        private void ResetJitterTargets()
        {
            if (jitterTargets == null || originalJitterPositions == null)
            {
                return;
            }

            for (var i = 0; i < jitterTargets.Length; i++)
            {
                if (jitterTargets[i] != null)
                {
                    jitterTargets[i].anchoredPosition = originalJitterPositions[i];
                }
            }
        }
    }
}
