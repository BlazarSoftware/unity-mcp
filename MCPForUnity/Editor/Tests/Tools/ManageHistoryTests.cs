using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageHistoryTests
    {
        [Test]
        public void GetHistoryStatus_ReturnsStatus()
        {
            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "get_history"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["currentGroup"]);
        }

        [Test]
        public void CreateUndoGroup_CreatesGroup()
        {
            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "create_group",
                ["name"] = "Test Group"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.AreEqual("Test Group", data.Value<string>("name"));
        }

        [Test]
        public void PerformUndo_ExecutesUndo()
        {
            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "undo"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }

        [Test]
        public void PerformRedo_ExecutesRedo()
        {
            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "redo"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }

        [Test]
        public void ClearAll_ClearsHistory()
        {
            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "clear_all"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }

        [Test]
        public void CollapseUndoGroup_CollapsesGroup()
        {
            // Arrange
            int groupIndex = Undo.GetCurrentGroup();

            // Act
            var result = ToJObject(ManageHistory.HandleCommand(new JObject
            {
                ["action"] = "collapse_group",
                ["groupIndex"] = groupIndex
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }
    }
}
