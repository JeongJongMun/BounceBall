using Spine.Unity;
using UnityEngine;

namespace Game
{
    // 체크포인트 깃발 연출. 애니메이션 하나(t=0 기둥만 → t=끝 깃발 펴짐)로 두 상태를 표현한다.
    // 비활성은 0프레임에 멈춰 두고, 활성화하면 1회 재생해 마지막 프레임을 유지한다.
    public class CheckpointSpineView : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeleton;
        [Tooltip("깃발이 올라가는 애니메이션")]
        [SerializeField] private string raiseAnimation = "Flag_Aniver3";

        private bool _applied;
        private bool _lastActivated;

        private bool HasSkeleton => skeleton != null && skeleton.AnimationState != null;

        // 스켈레톤이 Start에서 초기화되므로, 그 전에 들어온 상태는 여기서 한 번 더 반영한다.
        private void Start() => Apply(_lastActivated, false);

        // 활성화 순간에만 애니메이션을 재생하고, 그 외에는 해당 프레임에 정지시킨다.
        public void SetActivated(bool activated)
        {
            // 같은 상태를 다시 받으면 재생하지 않는다 — 복구 시 깃발이 다시 올라가 보이면 어색하다.
            if (_applied && _lastActivated == activated) return;
            Apply(activated, animate: activated);
        }

        private void Apply(bool activated, bool animate)
        {
            _lastActivated = activated;
            if (!HasSkeleton) return;
            _applied = true;

            if (string.IsNullOrEmpty(raiseAnimation)) return;

            var entry = skeleton.AnimationState.SetAnimation(0, raiseAnimation, false);
            if (animate)
            {
                entry.TimeScale = 1f;
                return;
            }

            // 연출 없이 상태만 맞춘다: 비활성은 첫 프레임, 활성은 마지막 프레임에 정지.
            entry.TimeScale = 0f;
            entry.TrackTime = activated && entry.Animation != null ? entry.Animation.Duration : 0f;
        }
    }
}
