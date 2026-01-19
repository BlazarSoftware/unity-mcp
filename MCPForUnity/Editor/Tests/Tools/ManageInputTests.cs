using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageInputTests
    {
        [Test]
        public void CheckInputSystemPackage_ReturnsPackageStatus()
        {
            // Act
            var result = ToJObject(ManageInput.HandleCommand(new JObject
            {
                ["action"] = "check_package"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["hasNewInputSystem"]);
            Assert.IsNotNull(data["hasLegacyInputManager"]);
        }

        [Test]
        public void GetInputSettings_ReturnsSettings()
        {
            // Act
            var result = ToJObject(ManageInput.HandleCommand(new JObject
            {
                ["action"] = "get_input_settings"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["touchSupport"]);
            Assert.IsNotNull(data["multiTouchEnabled"]);
        }

        [Test]
        public void SetInputSettings_UpdatesSettings()
        {
            // Arrange
            bool original = Input.multiTouchEnabled;

            // Act
            var result = ToJObject(ManageInput.HandleCommand(new JObject
            {
                ["action"] = "set_input_settings",
                ["multiTouchEnabled"] = !original
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.AreEqual(!original, Input.multiTouchEnabled);

            // Cleanup
            Input.multiTouchEnabled = original;
        }
    }
}
