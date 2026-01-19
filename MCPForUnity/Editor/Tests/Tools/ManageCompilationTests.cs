using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageCompilationTests
    {
        [Test]
        public void GetStatus_ReturnsCompilationStatus()
        {
            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "get_status"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["isCompiling"]);
            Assert.IsNotNull(data["isUpdating"]);
        }

        [Test]
        public void GetAssemblies_ReturnsAssemblyList()
        {
            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "get_assemblies"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["count"]);
            Assert.IsNotNull(data["assemblies"]);

            var assemblies = data["assemblies"] as JArray;
            Assert.IsNotNull(assemblies);
            Assert.Greater(assemblies.Count, 0, "Should have at least one assembly");
        }

        [Test]
        public void TriggerRecompile_WhenNotCompiling_Succeeds()
        {
            // Wait for any ongoing compilation to finish
            if (EditorApplication.isCompiling)
            {
                return; // Skip test if already compiling
            }

            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "trigger_recompile"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }

        [Test]
        public void GetCompilationErrors_ReturnsGuidance()
        {
            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "get_errors"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["recommendation"]);
        }

        [Test]
        public void WaitForCompilation_WhenNotCompiling_ReturnsImmediately()
        {
            // Wait for any compilation to finish first
            if (EditorApplication.isCompiling)
            {
                return; // Skip test
            }

            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "wait_for_compilation"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsFalse(data.Value<bool>("isCompiling"));
        }

        [Test]
        public void InvalidAction_ReturnsError()
        {
            // Act
            var result = ToJObject(ManageCompilation.HandleCommand(new JObject
            {
                ["action"] = "invalid_action"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }
    }
}
