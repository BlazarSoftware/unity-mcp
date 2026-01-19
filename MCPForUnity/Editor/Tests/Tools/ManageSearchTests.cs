using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageSearchTests
    {
        [Test]
        public void SearchAssets_FindsAssets()
        {
            // Act
            var result = ToJObject(ManageSearch.HandleCommand(new JObject
            {
                ["action"] = "search_assets",
                ["query"] = "Test"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["count"]);
            Assert.IsNotNull(data["results"]);
        }

        [Test]
        public void FindAssetsByType_FindsScripts()
        {
            // Act
            var result = ToJObject(ManageSearch.HandleCommand(new JObject
            {
                ["action"] = "find_by_type",
                ["type"] = "MonoScript"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.Greater(data.Value<int>("count"), 0, "Should find at least one script");
        }

        [Test]
        public void SearchScenes_FindsObjects()
        {
            // Arrange - Create a test object
            GameObject testObj = new GameObject("SearchTestObject");

            try
            {
                // Act
                var result = ToJObject(ManageSearch.HandleCommand(new JObject
                {
                    ["action"] = "search_scenes",
                    ["objectName"] = "SearchTestObject"
                }));

                // Assert
                Assert.IsTrue(result.Value<bool>("success"), result.ToString());
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.Greater(data.Value<int>("count"), 0, "Should find the test object");
            }
            finally
            {
                // Cleanup
                Object.DestroyImmediate(testObj);
            }
        }

        [Test]
        public void FindAssetsByLabel_SearchesByLabel()
        {
            // Act
            var result = ToJObject(ManageSearch.HandleCommand(new JObject
            {
                ["action"] = "find_by_label",
                ["label"] = "test"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["count"]);
        }

        [Test]
        public void SearchAssets_MissingQuery_ReturnsError()
        {
            // Act
            var result = ToJObject(ManageSearch.HandleCommand(new JObject
            {
                ["action"] = "search_assets"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }
    }
}
