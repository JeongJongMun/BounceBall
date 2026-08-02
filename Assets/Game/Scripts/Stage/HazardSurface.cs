using UnityEngine;

namespace Game
{
    // 프리팹 기믹의 사망 콜라이더 표식. 사망 판정을 원하는 콜라이더에만 붙인다.
    // 타일 사망 발판과 달리 콜라이더 모양 그대로 판정되므로, 한 프리팹 안에서
    // 흙 블록(일반 콜라이더)과 가시(이 컴포넌트)를 물리적으로 분리할 수 있다.
    // 면제 성질·등면 안전 규칙은 SpecialTile과 동일한 표를 쓴다.
    [RequireComponent(typeof(Collider2D))]
    public class HazardSurface : MonoBehaviour
    {
        [Label("발판 성질")]
        [Tooltip("이 성질의 플레이어는 사망을 면제받는다 (기본 발판은 면제 없음)")]
        [SerializeField] private TilePropertyType tileProperty = TilePropertyType.Default;

        [Label("전방향 사망")]
        [Tooltip("켜면 등면(가시 반대쪽)을 포함한 전방향에서 사망한다")]
        [SerializeField] private bool lethalAllDirections;

        [Label("면제 성질에 표면 효과 적용")]
        [Tooltip("사망을 면제받은 성질(젤리 가시 × 젤리 등)에 부착·미끄러짐을 그대로 적용할지 (기믹 문서 §4.3, §4.4)")]
        [SerializeField] private bool applySurfaceEffectWhenSafe = true;

        [Label("사망 영역 표시")]
        [Tooltip("씬 화면에 사망 콜라이더 윤곽을 빨간색으로 그린다. 빌드에는 영향이 없다")]
        [SerializeField] private bool showGizmo = true;

        public TilePropertyType TileProperty => tileProperty;
        public bool ApplySurfaceEffectWhenSafe => applySurfaceEffectWhenSafe;

        // 가시 방향 = 오브젝트의 위쪽. 프리팹을 회전해 배치하면 그대로 따라간다.
        public Vector2 SpikeDirection => transform.up;

        public bool IsLethalOnContact(PlayerPropertyType property, Vector2 contactNormal)
        {
            if (SpecialTile.IsSafeCombination(tileProperty, property)) return false;
            return lethalAllDirections || !SpecialTile.IsBackContact(contactNormal, SpikeDirection);
        }

        // 기획자가 사망 범위를 눈으로 확인할 수 있게 콜라이더 윤곽을 항상 그린다.
        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);

            var polygon = GetComponent<PolygonCollider2D>();
            if (polygon != null)
            {
                for (int p = 0; p < polygon.pathCount; p++)
                {
                    var path = polygon.GetPath(p);
                    for (int i = 0; i < path.Length; i++)
                    {
                        var from = transform.TransformPoint(path[i] + polygon.offset);
                        var to = transform.TransformPoint(path[(i + 1) % path.Length] + polygon.offset);
                        Gizmos.DrawLine(from, to);
                    }
                }
                return;
            }

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                var previous = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.matrix = previous;
            }
        }
    }
}
