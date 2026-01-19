using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageQualitySettingsTests
    {
        private int _originalQualityLevel;

        [SetUp]
        public void SetUp()
        {
            _originalQualityLevel = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void TearDown()
        {
            QualitySettings.SetQualityLevel(_originalQualityLevel);
        }

        [Test]
        public void GetQualityLevels_ReturnsLevels()
        {
            // Act
            var result = ToJObject(ManageQualitySettings.HandleCommand(new JObject
            {
                ["action"] = "get_levels"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.Greater(data.Value<int>("count"), 0);
            Assert.IsNotNull(data["levels"]);
        }

        [Test]
        public void GetCurrentLevel_ReturnsCurrentLevel()
        {
            // Act
            var result = ToJObject(ManageQualitySettings.HandleCommand(new JObject
            {
                ["action"] = "get_current"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["level"]);
            Assert.IsNotNull(data["name"]);
        }

        [Test]
        public void SetQualityLevel_ByIndex_SetsLevel()
        {
            // Arrange
            int targetLevel = 0; // Lowest quality

            // Act
            var result = ToJObject(ManageQualitySettings.HandleCommand(new JObject
            {
                ["action"] = "set_level",
                ["level"] = targetLevel
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.AreEqual(targetLevel, QualitySettings.GetQualityLevel());
        }

        [Test]
        public void GetQualitySettings_ReturnsSettings()
        {
            // Act
            var result = ToJObject(ManageQualitySettings.HandleCommand(new JObject
            {
                ["action"] = "get_settings"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["pixelLightCount"]);
            Assert.IsNotNull(data["shadows"]);
            Assert.IsNotNull(data["vSyncCount"]);
        }

        [Test]
        public void SetQualitySettings_UpdatesSettings()
        {
            // Arrange
            int originalVSync = QualitySettings.vSyncCount;

            // Act
            var result = ToJObject(ManageQualitySettings.HandleCommand(new JObject
            {
                ["action"] = "set_settings",
                ["vSyncCount"] = 1
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.AreEqual(1, QualitySettings.vSyncCount);

            // Cleanup
            QualitySettings.vSyncCount = originalVSync;
        }
    }
}
