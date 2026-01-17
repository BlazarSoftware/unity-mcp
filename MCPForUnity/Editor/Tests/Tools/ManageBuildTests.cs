using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageBuildTests
    {
        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetBuildSettings_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_build_settings"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.IsNotNull(success.Data);
        }

        [Test]
        public void GetPlayerSettings_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_player_settings"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.IsNotNull(success.Data);
        }

        [Test]
        public void GetScriptingDefines_ReturnsDefines()
        {
            var @params = new JObject
            {
                ["action"] = "get_scripting_defines"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetScenes_ValidScenes_SetsSuccessfully()
        {
            // Get current scenes to restore later
            var originalScenes = EditorBuildSettings.scenes;

            var @params = new JObject
            {
                ["action"] = "set_scenes",
                ["scenes"] = new JArray()
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            // Restore original scenes
            EditorBuildSettings.scenes = originalScenes;
        }

        [Test]
        public void BuildPlayer_MissingOutputPath_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "build_player",
                ["target"] = "StandaloneWindows64"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void BuildPlayer_MissingTarget_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "build_player",
                ["outputPath"] = "/tmp/build"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetPlatforms_ReturnsSupportedPlatforms()
        {
            var @params = new JObject
            {
                ["action"] = "get_platforms"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SwitchPlatform_InvalidPlatform_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "switch_platform",
                ["target"] = "InvalidPlatform"
            };

            var result = ManageBuild.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }
    }
}
