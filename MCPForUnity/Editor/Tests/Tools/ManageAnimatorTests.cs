using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageAnimatorTests
    {
        private GameObject _testObject;
        private Animator _animator;
        private AnimatorController _controller;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestAnimatorObject");
            _animator = _testObject.AddComponent<Animator>();

            // Create a simple animator controller
            _controller = new AnimatorController();
            _controller.name = "TestController";
            _controller.AddLayer("Base Layer");

            // Add parameters
            _controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            _controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            _controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            // Add a state
            var rootStateMachine = _controller.layers[0].stateMachine;
            var idleState = rootStateMachine.AddState("Idle");

            _animator.runtimeAnimatorController = _controller;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
            if (_controller != null)
            {
                UnityEngine.Object.DestroyImmediate(_controller);
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetParameters_ValidAnimator_ReturnsParameters()
        {
            var @params = new JObject
            {
                ["action"] = "get_parameters",
                ["target"] = _testObject.name
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.IsNotNull(success.Data);
        }

        [Test]
        public void GetParameters_NoAnimator_ReturnsError()
        {
            var emptyObject = new GameObject("EmptyObject");

            var @params = new JObject
            {
                ["action"] = "get_parameters",
                ["target"] = emptyObject.name
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);

            UnityEngine.Object.DestroyImmediate(emptyObject);
        }

        [Test]
        public void SetParameter_FloatParameter_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_parameter",
                ["target"] = _testObject.name,
                ["parameterName"] = "Speed",
                ["value"] = 5.0f
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(5.0f, _animator.GetFloat("Speed"));
        }

        [Test]
        public void SetParameter_BoolParameter_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_parameter",
                ["target"] = _testObject.name,
                ["parameterName"] = "IsRunning",
                ["value"] = true
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_animator.GetBool("IsRunning"));
        }

        [Test]
        public void SetParameter_InvalidParameter_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "set_parameter",
                ["target"] = _testObject.name,
                ["parameterName"] = "NonExistentParam",
                ["value"] = 1.0f
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetState_ValidAnimator_ReturnsState()
        {
            var @params = new JObject
            {
                ["action"] = "get_state",
                ["target"] = _testObject.name,
                ["layerIndex"] = 0
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetState_InvalidLayerIndex_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "get_state",
                ["target"] = _testObject.name,
                ["layerIndex"] = 999
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetSpeed_ValidAnimator_SetsSpeed()
        {
            var @params = new JObject
            {
                ["action"] = "set_speed",
                ["target"] = _testObject.name,
                ["speed"] = 2.0f
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(2.0f, _animator.speed);
        }

        [Test]
        public void GetLayerInfo_ValidAnimator_ReturnsLayers()
        {
            var @params = new JObject
            {
                ["action"] = "get_layer_info",
                ["target"] = _testObject.name
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void PlayState_ValidState_SuccessfullyPlays()
        {
            var @params = new JObject
            {
                ["action"] = "play_state",
                ["target"] = _testObject.name,
                ["stateName"] = "Idle"
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Crossfade_ValidState_SuccessfullyCrossfades()
        {
            var @params = new JObject
            {
                ["action"] = "crossfade",
                ["target"] = _testObject.name,
                ["stateName"] = "Idle",
                ["transitionDuration"] = 0.5f
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetControllerInfo_FromAnimator_ReturnsInfo()
        {
            var @params = new JObject
            {
                ["action"] = "get_controller_info",
                ["target"] = _testObject.name
            };

            var result = ManageAnimator.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.IsNotNull(success.Data);
        }
    }
}
