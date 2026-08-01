using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // LickText를 벽돌 배치로 화면에 채우고, 오른쪽에서 왼쪽으로 흘려보낸다.
    // 속도는 캔버스 유닛/초 기준이라 CanvasScaler·프레임레이트와 무관하게 일정하다.
    public class LickTextMarquee : MonoBehaviour
    {
        [SerializeField] private RectTransform lickTextTemplate;
        [SerializeField] private float speed = 60f;
        [SerializeField] private Vector2 spacing = Vector2.zero;

        [SerializeField] private float squashX = 1.28f;
        [SerializeField] private float squashY = 0.72f;
        [SerializeField] private float stretchX = 0.88f;
        [SerializeField] private float stretchY = 1.18f;
        [SerializeField] private float squashDuration = 0.08f;
        [SerializeField] private float stretchDuration = 0.1f;
        [SerializeField] private float settleDuration = 0.1f;
        [SerializeField] private float boingIntervalMin = 0.35f;
        [SerializeField] private float boingIntervalMax = 1.0f;
        [SerializeField] private int maxBoingCount = 3;

        private RectTransform _container;
        private readonly List<RectTransform> _active = new();
        private readonly HashSet<RectTransform> _boinging = new();
        private readonly List<float> _rowYs = new();
        private float _cellW;
        private float _cellH;
        private float _halfW;
        private float _halfH;
        private float _leftBound;
        private float _rightBound;
        private float _bottomBound;
        private float _topBound;
        private float _nextBoingTime;

        private void Awake()
        {
            _container = (RectTransform)transform;
            lickTextTemplate.gameObject.SetActive(false);

            var image = lickTextTemplate.GetComponent<Image>();
            var color = image.color;
            color.a = 0.5f;
            image.color = color;

            _cellW = lickTextTemplate.rect.width + spacing.x;
            _cellH = lickTextTemplate.rect.height + spacing.y;
            _halfW = _cellW * 0.5f;
            _halfH = _cellH * 0.5f;
        }

        private void OnEnable()
        {
            ClearAll();
            Canvas.ForceUpdateCanvases();
            CacheBounds();
            FillInitial();
            ScheduleNextBoing();
        }

        private void OnDisable() => ClearAll();

        private void Update()
        {
            float dx = speed * Time.deltaTime;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var rt = _active[i];
                var pos = rt.anchoredPosition;
                pos.x -= dx;
                rt.anchoredPosition = pos;

                // 화면 왼쪽 밖으로 완전히 벗어나면 제거
                if (pos.x + _halfW < _leftBound)
                {
                    StopBoing(rt);
                    _active.RemoveAt(i);
                    Destroy(rt.gameObject);
                }
            }

            EnsureRightFilled();
            TryStartBoing();
        }

        private void CacheBounds()
        {
            var rect = _container.rect;
            _leftBound = rect.xMin;
            _rightBound = rect.xMax;
            _bottomBound = rect.yMin;
            _topBound = rect.yMax;
        }

        private void FillInitial()
        {
            _rowYs.Clear();

            var rect = _container.rect;
            int rowCount = Mathf.CeilToInt(rect.height / _cellH) + 1;
            float startY = rect.yMin + _cellH * 0.5f;

            for (int row = 0; row < rowCount; row++)
            {
                float y = startY + row * _cellH;
                if (y - _cellH * 0.5f > rect.yMax) break;

                _rowYs.Add(y);
                float offsetX = (row % 2 == 1) ? _halfW : 0f;
                float x = _leftBound - _cellW + offsetX;

                while (x - _halfW < _rightBound + _cellW)
                {
                    Spawn(x, y);
                    x += _cellW;
                }
            }
        }

        // 행마다 오른쪽이 비지 않도록 새 타일을 이어 붙인다.
        private void EnsureRightFilled()
        {
            for (int row = 0; row < _rowYs.Count; row++)
            {
                float y = _rowYs[row];
                float offsetX = (row % 2 == 1) ? _halfW : 0f;
                float rightmost = float.NegativeInfinity;

                for (int i = 0; i < _active.Count; i++)
                {
                    var pos = _active[i].anchoredPosition;
                    if (Mathf.Abs(pos.y - y) > 0.01f) continue;
                    if (pos.x > rightmost) rightmost = pos.x;
                }

                if (float.IsNegativeInfinity(rightmost))
                    rightmost = _rightBound + offsetX - _cellW;

                while (rightmost + _halfW < _rightBound + _cellW)
                {
                    rightmost += _cellW;
                    Spawn(rightmost, y);
                }
            }
        }

        private void Spawn(float x, float y)
        {
            var clone = Instantiate(lickTextTemplate, _container);
            clone.gameObject.SetActive(true);
            clone.localScale = Vector3.one;
            clone.anchoredPosition = new Vector2(x, y);
            clone.SetAsFirstSibling();
            _active.Add(clone);
        }

        private void TryStartBoing()
        {
            if (_boinging.Count >= maxBoingCount) return;
            if (Time.time < _nextBoingTime) return;
            if (_active.Count == 0) return;

            // 화면에 보이는 타일 중에서 아직 뽀잉 중이 아닌 것을 고른다.
            int start = Random.Range(0, _active.Count);
            for (int n = 0; n < _active.Count; n++)
            {
                var rt = _active[(start + n) % _active.Count];
                if (_boinging.Contains(rt)) continue;
                if (!IsVisible(rt)) continue;

                PlayBoing(rt);
                ScheduleNextBoing();
                return;
            }

            ScheduleNextBoing();
        }

        // 중심이 화면 안쪽에 충분히 들어와 있는 타일만 뽀잉 대상으로 한다.
        private bool IsVisible(RectTransform rt)
        {
            var pos = rt.anchoredPosition;
            return pos.x > _leftBound + _halfW
                && pos.x < _rightBound - _halfW
                && pos.y > _bottomBound + _halfH
                && pos.y < _topBound - _halfH;
        }

        private void PlayBoing(RectTransform rt)
        {
            _boinging.Add(rt);
            rt.localScale = Vector3.one;

            // 가로로 늘어나고 세로로 눌린 뒤, 반대로 튕겼다가 원래 크기로 돌아온다.
            DOTween.Sequence()
                .SetTarget(rt)
                .Append(rt.DOScale(new Vector3(squashX, squashY, 1f), squashDuration).SetEase(Ease.OutQuad))
                .Append(rt.DOScale(new Vector3(stretchX, stretchY, 1f), stretchDuration).SetEase(Ease.OutBack))
                .Append(rt.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutBack))
                .OnKill(() =>
                {
                    _boinging.Remove(rt);
                    if (rt != null) rt.localScale = Vector3.one;
                });
        }

        private void StopBoing(RectTransform rt)
        {
            if (!_boinging.Remove(rt)) return;
            rt.DOKill();
            rt.localScale = Vector3.one;
        }

        private void ScheduleNextBoing()
        {
            _nextBoingTime = Time.time + Random.Range(boingIntervalMin, boingIntervalMax);
        }

        private void ClearAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].DOKill();
                Destroy(_active[i].gameObject);
            }
            _active.Clear();
            _boinging.Clear();
            _rowYs.Clear();
        }
    }
}
