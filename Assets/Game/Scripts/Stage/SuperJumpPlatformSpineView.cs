using Spine.Unity;
using UnityEngine;

namespace Game
{
    // 슈퍼 점프 발판 연출. 애니메이션 하나가 "눌림 → 튕김 → 복귀" 전체를 담고 있다.
    // 평소에는 아무것도 재생하지 않아 셋업 포즈(=0프레임, 스프링이 펴진 상태)로 서 있는다.
    public class SuperJumpPlatformSpineView : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeleton;
        [Tooltip("발판이 눌렸다 튕기는 애니메이션")]
        [SerializeField] private string launchAnimation = "animation";
        [Tooltip("재생을 시작할 지점(초). 플레이어는 접촉 즉시 튀어 오르므로, 눌리는 구간을 건너뛰고 펴지는 순간부터 맞춘다")]
        [SerializeField] private float launchStartTime = 0.1667f;

        private bool HasSkeleton => skeleton != null && skeleton.AnimationState != null;

        // 튕기는 순간 호출한다. 연타로 들어와도 매번 처음부터 다시 재생한다.
        public void PlayLaunch()
        {
            if (!HasSkeleton || string.IsNullOrEmpty(launchAnimation)) return;

            var entry = skeleton.AnimationState.SetAnimation(0, launchAnimation, false);
            if (entry == null) return;

            // 논루프 애니메이션은 끝나도 트랙에 남는다. 스켈레톤 기본 믹스(0.2초)가 그대로 걸리면
            // 두 번째부터는 쉬는 자세에서 블렌딩되며 눌림 구간이 뭉개진다 — 매번 첫 재생과 같게 맞춘다.
            entry.MixDuration = 0f;
            entry.TrackTime = launchStartTime;
        }
    }
}
