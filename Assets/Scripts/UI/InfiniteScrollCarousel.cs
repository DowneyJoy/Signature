using System.Collections.Generic;
using UnityEngine;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 无限水平轮播：所有子物体从视口右侧外进入，向左滚动穿过画面，
    /// 移出左边界后自动包裹回右侧，循环往复。
    /// </summary>
    public class InfiniteScrollCarousel : MonoBehaviour
    {
        [Header("轮播设置")]
        [Tooltip("滚动速度（像素/秒），正值向左滚动")]
        public float scrollSpeed = 150f;

        [Tooltip("是否在Start时自动开始轮播")]
        public bool autoStart = true;

        [Tooltip("子物体间距（0=自动根据初始布局计算）")]
        public float spacing = 0f;

        private RectTransform _viewport;
        private readonly List<RectTransform> _items = new List<RectTransform>();
        private float _itemWidth;
        private float _itemSpacing;
        private float _totalSpan;
        private bool _isRunning;

        private void Start()
        {
            _viewport = GetComponent<RectTransform>();
            CollectItems();
            CalculateLayout();
            PositionItemsAtRight();

            if (autoStart)
                StartScroll();
        }

        private void CollectItems()
        {
            _items.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).GetComponent<RectTransform>();
                if (child != null)
                    _items.Add(child);
            }
        }

        private void CalculateLayout()
        {
            if (_items.Count == 0) return;

            _itemWidth = _items[0].rect.width;

            if (spacing > 0f)
            {
                _itemSpacing = spacing;
            }
            else if (_items.Count >= 2)
            {
                float sum = 0f;
                for (int i = 1; i < _items.Count; i++)
                    sum += _items[i].anchoredPosition.x - _items[i - 1].anchoredPosition.x;
                _itemSpacing = Mathf.Abs(sum / (_items.Count - 1));
            }
            else
            {
                _itemSpacing = _itemWidth;
            }

            _totalSpan = _itemSpacing * _items.Count;
        }

        /// <summary>将所有子物体排列在视口右侧外部，等距分布，准备从右向左进入。</summary>
        private void PositionItemsAtRight()
        {
            if (_items.Count == 0) return;

            float halfViewport = _viewport.rect.width * 0.5f;
            float halfItem = _itemWidth * 0.5f;
            float baseY = _items[0].anchoredPosition.y;

            // 第一个子物体紧贴视口右边界外侧
            float startX = halfViewport + halfItem;

            for (int i = 0; i < _items.Count; i++)
            {
                var pos = _items[i].anchoredPosition;
                pos.x = startX + i * _itemSpacing;
                pos.y = baseY;
                _items[i].anchoredPosition = pos;
                _items[i].gameObject.SetActive(true);
            }
        }

        /// <summary>开始轮播。</summary>
        public void StartScroll()
        {
            _isRunning = true;
        }

        /// <summary>暂停轮播。</summary>
        public void StopScroll()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning || _items.Count == 0) return;

            float halfViewport = _viewport.rect.width * 0.5f;
            float halfItem = _itemWidth * 0.5f;
            float deltaX = scrollSpeed * Time.deltaTime;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var pos = item.anchoredPosition;
                pos.x -= deltaX;
                item.anchoredPosition = pos;

                // 当物品完全移出视口左边界时，包裹到右侧最远处
                if (pos.x + halfItem < -halfViewport)
                {
                    pos.x += _totalSpan;
                    item.anchoredPosition = pos;
                }
            }
        }
    }
}
