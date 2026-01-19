using System.Collections;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageProfilerTests
    {
        [TearDown]
        public void TearDown()
        {
            // Ensure profiler is stopped after tests
            ProfilerDriver.enabled = false;
            ProfilerDriver.deepProfiling = false;
        }

        [Test]
        public void GetState_ReturnsProfilerState()
        {
            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "get_state"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["enabled"]);
            Assert.IsNotNull(data["totalReservedMemory"]);
            Assert.IsNotNull(data["monoHeapSize"]);
        }

        [Test]
        public void StartProfiling_EnablesProfiler()
        {
            // Arrange
            ProfilerDriver.enabled = false;

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "start"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsTrue(ProfilerDriver.enabled);
        }

        [Test]
        public void StartProfiling_WithDeepProfiling_EnablesDeepProfiling()
        {
            // Arrange
            ProfilerDriver.deepProfiling = false;

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "start",
                ["deepProfiling"] = true
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsTrue(ProfilerDriver.deepProfiling);
        }

        [Test]
        public void StopProfiling_DisablesProfiler()
        {
            // Arrange
            ProfilerDriver.enabled = true;

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "stop"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsFalse(ProfilerDriver.enabled);
        }

        [Test]
        public void GetMemorySnapshot_ReturnsMemoryData()
        {
            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "get_memory"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["totalReservedMemory"]);
            Assert.IsNotNull(data["totalAllocatedMemory"]);
            Assert.IsNotNull(data["monoHeapSize"]);
            Assert.IsNotNull(data["monoUsedSize"]);
        }

        [UnityTest]
        public IEnumerator GetProfilerData_WhenEnabled_ReturnsData()
        {
            // Arrange
            ProfilerDriver.enabled = true;
            ProfilerDriver.ClearAllFrames();

            // Enter play mode to generate some profiler data
            EditorApplication.isPlaying = true;
            yield return new WaitWhile(() => !EditorApplication.isPlaying);
            yield return null; // Wait a frame
            yield return null; // Wait another frame

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "get_data"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["frameIndex"]);
            Assert.IsNotNull(data["totalReservedMemory"]);

            // Cleanup
            EditorApplication.isPlaying = false;
            yield return new WaitWhile(() => EditorApplication.isPlaying);
            ProfilerDriver.enabled = false;
        }

        [Test]
        public void GetProfilerData_WhenNotEnabled_ReturnsError()
        {
            // Arrange
            ProfilerDriver.enabled = false;

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "get_data"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }

        [Test]
        public void ClearProfilerData_ClearsFrames()
        {
            // Arrange
            ProfilerDriver.enabled = true;

            // Act
            var result = ToJObject(ManageProfiler.HandleCommand(new JObject
            {
                ["action"] = "clear"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());

            // Cleanup
            ProfilerDriver.enabled = false;
        }
    }
}
