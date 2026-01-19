using System.Collections;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManagePlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            // Ensure we exit play mode after each test
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            EditorApplication.isPaused = false;
            Time.timeScale = 1f;
        }

        [Test]
        public void GetState_ReturnsCurrentState()
        {
            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "get_state"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["isPlaying"]);
            Assert.IsNotNull(data["isPaused"]);
            Assert.IsNotNull(data["timeScale"]);
        }

        [UnityTest]
        public IEnumerator EnterPlayMode_EntersPlayMode()
        {
            // Ensure we're in edit mode
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                yield return new WaitWhile(() => EditorApplication.isPlaying);
            }

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "enter"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());

            // Wait for play mode to actually enter
            yield return new WaitWhile(() => !EditorApplication.isPlaying);

            Assert.IsTrue(EditorApplication.isPlaying);

            // Cleanup
            EditorApplication.isPlaying = false;
            yield return new WaitWhile(() => EditorApplication.isPlaying);
        }

        [UnityTest]
        public IEnumerator ExitPlayMode_ExitsPlayMode()
        {
            // Arrange - enter play mode first
            EditorApplication.isPlaying = true;
            yield return new WaitWhile(() => !EditorApplication.isPlaying);

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "exit"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());

            // Wait for play mode to exit
            yield return new WaitWhile(() => EditorApplication.isPlaying);

            Assert.IsFalse(EditorApplication.isPlaying);
        }

        [UnityTest]
        public IEnumerator PausePlayMode_PausesGame()
        {
            // Arrange - enter play mode
            EditorApplication.isPlaying = true;
            yield return new WaitWhile(() => !EditorApplication.isPlaying);

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "pause"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsTrue(EditorApplication.isPaused);

            // Cleanup
            EditorApplication.isPlaying = false;
            yield return new WaitWhile(() => EditorApplication.isPlaying);
        }

        [UnityTest]
        public IEnumerator ResumePlayMode_ResumesGame()
        {
            // Arrange - enter and pause
            EditorApplication.isPlaying = true;
            yield return new WaitWhile(() => !EditorApplication.isPlaying);
            EditorApplication.isPaused = true;

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "resume"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsFalse(EditorApplication.isPaused);

            // Cleanup
            EditorApplication.isPlaying = false;
            yield return new WaitWhile(() => EditorApplication.isPlaying);
        }

        [UnityTest]
        public IEnumerator StepFrame_AdvancesOneFrame()
        {
            // Arrange - enter play mode and pause
            EditorApplication.isPlaying = true;
            yield return new WaitWhile(() => !EditorApplication.isPlaying);
            EditorApplication.isPaused = true;
            yield return null; // Wait one frame

            int frameBeforeStep = Time.frameCount;

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "step"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            yield return null; // Wait for step to complete

            // Frame should have advanced
            Assert.Greater(Time.frameCount, frameBeforeStep);

            // Cleanup
            EditorApplication.isPlaying = false;
            yield return new WaitWhile(() => EditorApplication.isPlaying);
        }

        [Test]
        public void SetTimeScale_SetsTimeScale()
        {
            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "set_timescale",
                ["timeScale"] = 0.5f
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.AreEqual(0.5f, Time.timeScale, 0.01f);

            // Cleanup
            Time.timeScale = 1f;
        }

        [Test]
        public void SetTimeScale_NegativeValue_ReturnsError()
        {
            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "set_timescale",
                ["timeScale"] = -1f
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }

        [Test]
        public void PausePlayMode_WhenNotPlaying_ReturnsError()
        {
            // Ensure we're not playing
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            // Act
            var result = ToJObject(ManagePlayMode.HandleCommand(new JObject
            {
                ["action"] = "pause"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }
    }
}
