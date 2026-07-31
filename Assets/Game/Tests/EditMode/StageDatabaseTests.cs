using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StageDatabaseTests
    {
        private StageDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<StageDatabase>();
            _database.SetStages(new List<StageDatabase.StageEntry>
            {
                new() { sceneName = "Stage01", displayName = "1" },
                new() { sceneName = "Stage02", displayName = "2" },
                new() { sceneName = "Stage03", displayName = "3" },
            });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_database);

        [Test]
        public void 다음_스테이지를_반환한다()
        {
            Assert.AreEqual("Stage02", _database.GetNextStageScene("Stage01"));
            Assert.AreEqual("Stage03", _database.GetNextStageScene("Stage02"));
        }

        [Test]
        public void 마지막_스테이지의_다음은_null()
        {
            Assert.IsNull(_database.GetNextStageScene("Stage03"));
        }

        [Test]
        public void 목록에_없는_씬의_다음은_null()
        {
            Assert.IsNull(_database.GetNextStageScene("Unknown"));
        }
    }
}
