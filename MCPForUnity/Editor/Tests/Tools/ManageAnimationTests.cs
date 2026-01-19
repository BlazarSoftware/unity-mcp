using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageAnimationTests
    {
        private string testClipPath = "Assets/TestAnimationClip_ManageAnimation.anim";

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(testClipPath))
            {
                AssetDatabase.DeleteAsset(testClipPath);
            }
        }

        [Test]
        public void CreateAnimationClip_CreatesClip()
        {
            // Act
            var result = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "create_clip",
                ["savePath"] = testClipPath,
                ["frameRate"] = 30
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            Assert.IsTrue(File.Exists(testClipPath), "Animation clip file should exist");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(testClipPath);
            Assert.IsNotNull(clip);
            Assert.AreEqual(30f, clip.frameRate);
        }

        [Test]
        public void AddCurve_AddsCurveToClip()
        {
            // Arrange - create a clip first
            var createResult = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "create_clip",
                ["savePath"] = testClipPath
            }));
            Assert.IsTrue(createResult.Value<bool>("success"));

            // Act - add a curve
            var result = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "add_curve",
                ["clipPath"] = testClipPath,
                ["propertyPath"] = "m_LocalPosition.x",
                ["keyframes"] = new JArray
                {
                    new JObject { ["time"] = 0f, ["value"] = 0f },
                    new JObject { ["time"] = 1f, ["value"] = 5f }
                }
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(testClipPath);
            Assert.Greater(clip.length, 0, "Clip should have length after adding curve");
        }

        [Test]
        public void AddAnimationEvent_AddsEventToClip()
        {
            // Arrange - create a clip first
            var createResult = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "create_clip",
                ["savePath"] = testClipPath
            }));
            Assert.IsTrue(createResult.Value<bool>("success"));

            // Act
            var result = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "add_event",
                ["clipPath"] = testClipPath,
                ["functionName"] = "TestFunction",
                ["time"] = 0.5f
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(testClipPath);
            var events = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("TestFunction", events[0].functionName);
        }

        [Test]
        public void GetClipInfo_ReturnsClipInfo()
        {
            // Arrange - create a clip
            var createResult = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "create_clip",
                ["savePath"] = testClipPath
            }));
            Assert.IsTrue(createResult.Value<bool>("success"));

            // Act
            var result = ToJObject(ManageAnimation.HandleCommand(new JObject
            {
                ["action"] = "get_clip_info",
                ["clipPath"] = testClipPath
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["name"]);
            Assert.IsNotNull(data["frameRate"]);
            Assert.IsNotNull(data["length"]);
        }
    }
}
