using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageCameraTests
    {
        private GameObject _testObject;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestCameraObject");
            _camera = _testObject.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetProperties_FOV_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["fieldOfView"] = 90.0f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(90.0f, _camera.fieldOfView, 0.001f);
        }

        [Test]
        public void SetProperties_NearClip_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["nearClipPlane"] = 0.5f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(0.5f, _camera.nearClipPlane, 0.001f);
        }

        [Test]
        public void SetProperties_FarClip_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["farClipPlane"] = 500.0f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(500.0f, _camera.farClipPlane, 0.001f);
        }

        [Test]
        public void SetProperties_Orthographic_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["orthographic"] = true,
                ["orthographicSize"] = 10.0f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_camera.orthographic);
            Assert.AreEqual(10.0f, _camera.orthographicSize, 0.001f);
        }

        [Test]
        public void SetProperties_NoCamera_ReturnsError()
        {
            var emptyObject = new GameObject("EmptyObject");

            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = emptyObject.name,
                ["fieldOfView"] = 60.0f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);

            UnityEngine.Object.DestroyImmediate(emptyObject);
        }

        [Test]
        public void GetProperties_ValidCamera_ReturnsInfo()
        {
            _camera.fieldOfView = 75.0f;

            var @params = new JObject
            {
                ["action"] = "get_properties",
                ["target"] = _testObject.name
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void LookAt_ValidTarget_Succeeds()
        {
            var targetObject = new GameObject("LookTarget");
            targetObject.transform.position = new Vector3(10, 0, 10);

            var @params = new JObject
            {
                ["action"] = "look_at",
                ["target"] = _testObject.name,
                ["lookAtTarget"] = targetObject.name
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            UnityEngine.Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void SetTransform_Position_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_transform",
                ["target"] = _testObject.name,
                ["position"] = new JArray { 5, 10, 15 }
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(5.0f, _testObject.transform.position.x, 0.001f);
            Assert.AreEqual(10.0f, _testObject.transform.position.y, 0.001f);
            Assert.AreEqual(15.0f, _testObject.transform.position.z, 0.001f);
        }

        [Test]
        public void SetTransform_Rotation_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_transform",
                ["target"] = _testObject.name,
                ["rotation"] = new JArray { 45, 90, 0 }
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(45.0f, _testObject.transform.eulerAngles.x, 0.1f);
            Assert.AreEqual(90.0f, _testObject.transform.eulerAngles.y, 0.1f);
        }

        [Test]
        public void Create_NewCamera_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create",
                ["name"] = "TestNewCamera",
                ["fieldOfView"] = 80.0f
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdCamera = GameObject.Find("TestNewCamera");
            Assert.IsNotNull(createdCamera);

            var cam = createdCamera.GetComponent<Camera>();
            Assert.IsNotNull(cam);
            Assert.AreEqual(80.0f, cam.fieldOfView, 0.001f);

            UnityEngine.Object.DestroyImmediate(createdCamera);
        }

        [Test]
        public void SetCullingMask_ValidLayer_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_culling_mask",
                ["target"] = _testObject.name,
                ["layers"] = new JArray { "Default" }
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetCullingMask_ReturnsLayers()
        {
            var @params = new JObject
            {
                ["action"] = "get_culling_mask",
                ["target"] = _testObject.name
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void AlignToView_ValidCamera_Succeeds()
        {
            var @params = new JObject
            {
                ["action"] = "align_to_view",
                ["target"] = _testObject.name
            };

            var result = ManageCamera.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
