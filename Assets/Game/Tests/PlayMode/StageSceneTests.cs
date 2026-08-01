using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 실제 스테이지 씬(Stage01)을 로드해 바닥 충돌이 동작하는지 검증한다.
    // (씬의 컴포지트 콜라이더 지오메트리 누락 회귀 방지)
    public class StageSceneTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // 다음 테스트에 씬 잔재가 영향을 주지 않도록 정리
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                Object.Destroy(go);
            var systems = GameObject.Find("Systems");
            if (systems != null) Object.Destroy(systems);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 스테이지_씬에서_공이_바닥을_뚫지_않는다()
        {
            SceneManager.LoadScene("Stage01");
            yield return null;

            var systems = GameObject.Find("Systems");
            if (systems != null) Object.Destroy(systems);

            var stage = Object.FindFirstObjectByType<StageController>();
            Assert.IsNotNull(stage, "StageController가 없습니다.");

            // 플레이어 스폰 대기
            Player player = null;
            float deadline = Time.time + 2f;
            while (player == null && Time.time < deadline)
            {
                player = Object.FindFirstObjectByType<Player>();
                yield return null;
            }
            Assert.IsNotNull(player, "플레이어가 스폰되지 않았습니다.");

            // 4초 동안 낙사선 아래로 떨어지지 않아야 한다
            deadline = Time.time + 4f;
            while (Time.time < deadline)
            {
                Assert.Greater(player.transform.position.y, stage.StageFallLimitY - 1f,
                    "공이 바닥을 뚫고 낙사선 아래로 떨어졌습니다.");
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
