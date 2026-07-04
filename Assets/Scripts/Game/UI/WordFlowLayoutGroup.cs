using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 按子物体首选尺寸横向排列，并在空间不足时自动换行的轻量布局组件。
    /// </summary>
    public sealed class WordFlowLayoutGroup : LayoutGroup
    {
        [Header("词块流式布局")]
        [Tooltip("同一行词块之间的间距。")]
        [SerializeField]
        private float horizontalSpacing = 10f;

        [Tooltip("不同行之间的间距。")]
        [SerializeField]
        private float verticalSpacing = 10f;

        [Tooltip("子物体没有首选宽度时使用的默认宽度。")]
        [SerializeField]
        private float fallbackChildWidth = 120f;

        [Tooltip("子物体没有首选高度时使用的默认高度。")]
        [SerializeField]
        private float fallbackChildHeight = 52f;

        [Header("随机散布")]
        [Tooltip("是否在区域内随机散布词块。关闭时使用横向换行布局。")]
        [SerializeField]
        private bool useRandomDistribution;

        [Tooltip("随机散布使用的固定种子。同一组词每次进入关卡会得到稳定位置。")]
        [SerializeField]
        private int randomSeed = 2026;

        [Tooltip("随机尝试寻找不重叠位置的次数。随机失败后会使用网格扫描继续寻找。")]
        [SerializeField]
        [Min(1)]
        private int randomPlacementAttempts = 40;

        [Tooltip("词块之间保留的最小间隔。")]
        [SerializeField]
        [Min(0f)]
        private float randomSpacing = 6f;

        private readonly Dictionary<int, Vector2> randomPositionsBySlotId = new();
        private readonly HashSet<int> activeSlotIds = new();
        private readonly List<int> staleSlotIds = new();

        /// <inheritdoc />
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
        }

        /// <inheritdoc />
        public override void CalculateLayoutInputVertical()
        {
            var requiredHeight = useRandomDistribution
                ? Mathf.Max(padding.vertical, rectTransform.rect.height)
                : CalculateRequiredHeight();
            SetLayoutInputForAxis(requiredHeight, requiredHeight, -1f, 1);
        }

        /// <inheritdoc />
        public override void SetLayoutHorizontal()
        {
            SetChildren();
        }

        /// <inheritdoc />
        public override void SetLayoutVertical()
        {
            SetChildren();
        }

        private void SetChildren()
        {
            if (useRandomDistribution)
            {
                SetChildrenRandomly();
                return;
            }

            SetChildrenAlongFlow();
        }

        private float CalculateRequiredHeight()
        {
            var availableWidth = GetAvailableWidth();
            RefreshChildLayoutSizes(availableWidth);
            var cursorX = (float)padding.left;
            var totalHeight = (float)(padding.top + padding.bottom);
            var currentRowHeight = 0f;
            var hasItemInRow = false;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var childWidth = GetChildWidth(child);
                var childHeight = GetChildHeight(child);
                var needsWrap = hasItemInRow && cursorX + childWidth > padding.left + availableWidth;

                if (needsWrap)
                {
                    totalHeight += currentRowHeight + verticalSpacing;
                    cursorX = padding.left;
                    currentRowHeight = 0f;
                    hasItemInRow = false;
                }

                cursorX += (hasItemInRow ? horizontalSpacing : 0f) + childWidth;
                currentRowHeight = Mathf.Max(currentRowHeight, childHeight);
                hasItemInRow = true;
            }

            if (hasItemInRow)
            {
                totalHeight += currentRowHeight;
            }

            return totalHeight;
        }

        private void SetChildrenAlongFlow()
        {
            var availableWidth = GetAvailableWidth();
            RefreshChildLayoutSizes(availableWidth);
            var cursorX = (float)padding.left;
            var cursorY = (float)padding.top;
            var currentRowHeight = 0f;
            var hasItemInRow = false;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var childWidth = GetChildWidth(child);
                var childHeight = GetChildHeight(child);
                var needsWrap = hasItemInRow && cursorX + childWidth > padding.left + availableWidth;

                if (needsWrap)
                {
                    cursorX = padding.left;
                    cursorY += currentRowHeight + verticalSpacing;
                    currentRowHeight = 0f;
                    hasItemInRow = false;
                }

                if (hasItemInRow)
                {
                    cursorX += horizontalSpacing;
                }

                SetChildAlongAxis(child, 0, cursorX, childWidth);
                SetChildAlongAxis(child, 1, cursorY, childHeight);

                cursorX += childWidth;
                currentRowHeight = Mathf.Max(currentRowHeight, childHeight);
                hasItemInRow = true;
            }
        }

        private void SetChildrenRandomly()
        {
            var availableWidth = GetAvailableWidth();
            var availableHeight = GetAvailableHeight();
            RefreshChildLayoutSizes(availableWidth);
            var random = new System.Random(randomSeed + rectChildren.Count * 397);
            var placedRects = new Rect[rectChildren.Count];
            var placedCount = 0;
            activeSlotIds.Clear();

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var childWidth = Mathf.Min(GetChildWidth(child), availableWidth);
                var childHeight = Mathf.Min(GetChildHeight(child), availableHeight);
                var slotId = GetSlotId(child, i);
                activeSlotIds.Add(slotId);
                var hasCachedPosition = randomPositionsBySlotId.TryGetValue(slotId, out var cachedPosition);
                var position = hasCachedPosition
                    ? ClampPositionToBounds(cachedPosition, childWidth, childHeight, availableWidth, availableHeight)
                    : Vector2.zero;

                if (!hasCachedPosition || OverlapsAny(new Rect(position.x, position.y, childWidth, childHeight), placedRects, placedCount, randomSpacing))
                {
                    position = FindRandomPosition(random, childWidth, childHeight, availableWidth, availableHeight, placedRects, placedCount);
                }

                randomPositionsBySlotId[slotId] = position;

                SetChildAlongAxis(child, 0, position.x, childWidth);
                SetChildAlongAxis(child, 1, position.y, childHeight);
                placedRects[placedCount] = new Rect(position.x, position.y, childWidth, childHeight);
                placedCount++;
            }

            RemoveInactiveCachedPositions();
        }

        private void RemoveInactiveCachedPositions()
        {
            staleSlotIds.Clear();
            foreach (var slotId in randomPositionsBySlotId.Keys)
            {
                if (!activeSlotIds.Contains(slotId))
                {
                    staleSlotIds.Add(slotId);
                }
            }

            for (var i = 0; i < staleSlotIds.Count; i++)
            {
                randomPositionsBySlotId.Remove(staleSlotIds[i]);
            }
        }

        private static int GetSlotId(RectTransform child, int fallbackIndex)
        {
            var wordItem = child.GetComponent<DraggableWordItem>();
            return wordItem != null && wordItem.LayoutSlotId >= 0 ? wordItem.LayoutSlotId : fallbackIndex;
        }

        private Vector2 FindRandomPosition(
            System.Random random,
            float childWidth,
            float childHeight,
            float availableWidth,
            float availableHeight,
            Rect[] placedRects,
            int placedCount)
        {
            var position = Vector2.zero;
            var foundNonOverlappingPosition = false;

            for (var attempt = 0; attempt < randomPlacementAttempts; attempt++)
            {
                position = GetRandomPosition(random, childWidth, childHeight, availableWidth, availableHeight);
                var candidateRect = new Rect(position.x, position.y, childWidth, childHeight);

                if (!OverlapsAny(candidateRect, placedRects, placedCount, randomSpacing))
                {
                    foundNonOverlappingPosition = true;
                    break;
                }
            }

            if (foundNonOverlappingPosition)
            {
                return position;
            }

            if (TryFindGridPosition(childWidth, childHeight, availableWidth, availableHeight, placedRects, placedCount, out var gridPosition))
            {
                return gridPosition;
            }

            if (TryFindGridPosition(childWidth, childHeight, availableWidth, availableHeight, placedRects, placedCount, 0f, out var tightGridPosition))
            {
                return tightGridPosition;
            }

            return position;
        }

        private Vector2 ClampPositionToBounds(Vector2 position, float childWidth, float childHeight, float availableWidth, float availableHeight)
        {
            return new Vector2(
                Mathf.Clamp(position.x, padding.left, padding.left + Mathf.Max(0f, availableWidth - childWidth)),
                Mathf.Clamp(position.y, padding.top, padding.top + Mathf.Max(0f, availableHeight - childHeight)));
        }

        private bool TryFindGridPosition(
            float childWidth,
            float childHeight,
            float availableWidth,
            float availableHeight,
            Rect[] placedRects,
            int placedCount,
            out Vector2 position)
        {
            var step = Mathf.Max(4f, randomSpacing);
            var maxX = Mathf.Max(0f, availableWidth - childWidth);
            var maxY = Mathf.Max(0f, availableHeight - childHeight);

            for (var y = 0f; y <= maxY; y += step)
            {
                for (var x = 0f; x <= maxX; x += step)
                {
                    var candidate = new Vector2(padding.left + x, padding.top + y);
                    var candidateRect = new Rect(candidate.x, candidate.y, childWidth, childHeight);
                    if (!OverlapsAny(candidateRect, placedRects, placedCount, randomSpacing))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = Vector2.zero;
            return false;
        }

        private bool TryFindGridPosition(
            float childWidth,
            float childHeight,
            float availableWidth,
            float availableHeight,
            Rect[] placedRects,
            int placedCount,
            float spacing,
            out Vector2 position)
        {
            var step = spacing > 0f ? Mathf.Max(4f, spacing) : 1f;
            var maxX = Mathf.Max(0f, availableWidth - childWidth);
            var maxY = Mathf.Max(0f, availableHeight - childHeight);

            for (var y = 0f; y <= maxY; y += step)
            {
                for (var x = 0f; x <= maxX; x += step)
                {
                    var candidate = new Vector2(padding.left + x, padding.top + y);
                    var candidateRect = new Rect(candidate.x, candidate.y, childWidth, childHeight);
                    if (!OverlapsAny(candidateRect, placedRects, placedCount, spacing))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = Vector2.zero;
            return false;
        }

        private Vector2 GetRandomPosition(System.Random random, float childWidth, float childHeight, float availableWidth, float availableHeight)
        {
            var xRange = Mathf.Max(0f, availableWidth - childWidth);
            var yRange = Mathf.Max(0f, availableHeight - childHeight);
            return new Vector2(
                padding.left + (float)random.NextDouble() * xRange,
                padding.top + (float)random.NextDouble() * yRange);
        }

        private static bool OverlapsAny(Rect candidateRect, Rect[] placedRects, int placedCount, float spacing)
        {
            var paddedCandidate = ExpandRect(candidateRect, spacing);
            for (var i = 0; i < placedCount; i++)
            {
                if (paddedCandidate.Overlaps(ExpandRect(placedRects[i], spacing)))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect ExpandRect(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private float GetAvailableWidth()
        {
            return Mathf.Max(1f, rectTransform.rect.width - padding.horizontal);
        }

        private float GetAvailableHeight()
        {
            return Mathf.Max(1f, rectTransform.rect.height - padding.vertical);
        }

        private void RefreshChildLayoutSizes(float availableWidth)
        {
            for (var i = 0; i < rectChildren.Count; i++)
            {
                var wordItem = rectChildren[i].GetComponent<DraggableWordItem>();
                if (wordItem != null)
                {
                    wordItem.RefreshLayoutSize(availableWidth);
                }
            }
        }

        private float GetChildWidth(RectTransform child)
        {
            var preferred = LayoutUtility.GetPreferredWidth(child);
            return preferred > 0f ? preferred : fallbackChildWidth;
        }

        private float GetChildHeight(RectTransform child)
        {
            var preferred = LayoutUtility.GetPreferredHeight(child);
            return preferred > 0f ? preferred : fallbackChildHeight;
        }
    }
}
