using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageLightingTests
    {
        private GameObject _testObject;
        private Light _light;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestLightObject");
            _light = _testObject.AddComponent<Light>();
            _light.type = LightType.Point;
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

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetLight_Intensity_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = _testObject.name,
                ["intensity"] = 2.5f
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(2.5f, _light.intensity, 0.001f);
        }

        [Test]
        public void SetLight_Color_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = _testObject.name,
                ["color"] = new JObject { ["r"] = 1.0f, ["g"] = 0.5f, ["b"] = 0.0f }
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(1.0f, _light.color.r, 0.001f);
            Assert.AreEqual(0.5f, _light.color.g, 0.001f);
            Assert.AreEqual(0.0f, _light.color.b, 0.001f);
        }

        [Test]
        public void SetLight_Range_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = _testObject.name,
                ["range"] = 20.0f
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(20.0f, _light.range, 0.001f);
        }

        [Test]
        public void SetLight_Shadows_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = _testObject.name,
                ["shadows"] = "soft"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(LightShadows.Soft, _light.shadows);
        }

        [Test]
        public void SetLight_Enabled_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = _testObject.name,
                ["enabled"] = false
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsFalse(_light.enabled);
        }

        [Test]
        public void SetLight_NoLight_ReturnsError()
        {
            var emptyObject = new GameObject("EmptyObject");

            var @params = new JObject
            {
                ["action"] = "set_light",
                ["target"] = emptyObject.name,
                ["intensity"] = 1.0f
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);

            UnityEngine.Object.DestroyImmediate(emptyObject);
        }

        [Test]
        public void GetLightInfo_ValidLight_ReturnsInfo()
        {
            _light.intensity = 3.0f;
            _light.range = 15.0f;

            var @params = new JObject
            {
                ["action"] = "get_light_info",
                ["target"] = _testObject.name
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void CreateLight_PointLight_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create_light",
                ["lightType"] = "point",
                ["name"] = "TestPointLight",
                ["intensity"] = 2.0f,
                ["color"] = new JObject { ["r"] = 1.0f, ["g"] = 1.0f, ["b"] = 0.0f }
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdLight = GameObject.Find("TestPointLight");
            Assert.IsNotNull(createdLight);

            var light = createdLight.GetComponent<Light>();
            Assert.IsNotNull(light);
            Assert.AreEqual(LightType.Point, light.type);
            Assert.AreEqual(2.0f, light.intensity, 0.001f);

            UnityEngine.Object.DestroyImmediate(createdLight);
        }

        [Test]
        public void CreateLight_DirectionalLight_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create_light",
                ["lightType"] = "directional",
                ["name"] = "TestDirectionalLight"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdLight = GameObject.Find("TestDirectionalLight");
            Assert.IsNotNull(createdLight);

            var light = createdLight.GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, light.type);

            UnityEngine.Object.DestroyImmediate(createdLight);
        }

        [Test]
        public void CreateLight_SpotLight_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create_light",
                ["lightType"] = "spot",
                ["name"] = "TestSpotLight",
                ["spotAngle"] = 45.0f
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdLight = GameObject.Find("TestSpotLight");
            Assert.IsNotNull(createdLight);

            var light = createdLight.GetComponent<Light>();
            Assert.AreEqual(LightType.Spot, light.type);
            Assert.AreEqual(45.0f, light.spotAngle, 0.001f);

            UnityEngine.Object.DestroyImmediate(createdLight);
        }

        [Test]
        public void GetBakeStatus_ReturnsStatus()
        {
            var @params = new JObject
            {
                ["action"] = "get_bake_status"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetAmbient_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_ambient"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetAmbient_Color_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_ambient",
                ["mode"] = "flat",
                ["color"] = new JObject { ["r"] = 0.5f, ["g"] = 0.5f, ["b"] = 0.5f }
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetFog_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_fog"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetFog_Enabled_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_fog",
                ["enabled"] = true,
                ["color"] = new JObject { ["r"] = 0.5f, ["g"] = 0.5f, ["b"] = 0.7f }
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetAllLights_ReturnsLights()
        {
            var @params = new JObject
            {
                ["action"] = "get_all_lights"
            };

            var result = ManageLighting.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
