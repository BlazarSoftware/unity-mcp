using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManagePhysicsTests
    {
        private GameObject _testObject;
        private Rigidbody _rigidbody;
        private BoxCollider _collider;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestPhysicsObject");
            _rigidbody = _testObject.AddComponent<Rigidbody>();
            _collider = _testObject.AddComponent<BoxCollider>();
            _rigidbody.useGravity = false; // Disable gravity for predictable tests
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

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetRigidbody_Mass_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_rigidbody",
                ["target"] = _testObject.name,
                ["mass"] = 10.0f
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(10.0f, _rigidbody.mass, 0.001f);
        }

        [Test]
        public void SetRigidbody_UseGravity_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_rigidbody",
                ["target"] = _testObject.name,
                ["useGravity"] = true
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_rigidbody.useGravity);
        }

        [Test]
        public void SetRigidbody_IsKinematic_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_rigidbody",
                ["target"] = _testObject.name,
                ["isKinematic"] = true
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_rigidbody.isKinematic);
        }

        [Test]
        public void SetRigidbody_CreateIfMissing_CreatesRigidbody()
        {
            var newObject = new GameObject("NewPhysicsObject");

            var @params = new JObject
            {
                ["action"] = "set_rigidbody",
                ["target"] = newObject.name,
                ["createIfMissing"] = true,
                ["mass"] = 5.0f
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var rb = newObject.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.AreEqual(5.0f, rb.mass, 0.001f);

            UnityEngine.Object.DestroyImmediate(newObject);
        }

        [Test]
        public void GetRigidbodyInfo_ValidRigidbody_ReturnsInfo()
        {
            _rigidbody.mass = 15.0f;

            var @params = new JObject
            {
                ["action"] = "get_rigidbody_info",
                ["target"] = _testObject.name
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetRigidbodyInfo_NoRigidbody_ReturnsError()
        {
            var emptyObject = new GameObject("EmptyObject");

            var @params = new JObject
            {
                ["action"] = "get_rigidbody_info",
                ["target"] = emptyObject.name
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);

            UnityEngine.Object.DestroyImmediate(emptyObject);
        }

        [Test]
        public void ConfigureCollider_IsTrigger_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "configure_collider",
                ["target"] = _testObject.name,
                ["isTrigger"] = true
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_collider.isTrigger);
        }

        [Test]
        public void ConfigureCollider_Enabled_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "configure_collider",
                ["target"] = _testObject.name,
                ["enabled"] = false
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsFalse(_collider.enabled);
        }

        [Test]
        public void GetColliderInfo_ValidCollider_ReturnsInfo()
        {
            var @params = new JObject
            {
                ["action"] = "get_collider_info",
                ["target"] = _testObject.name
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetPhysicsSettings_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_physics_settings"
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Raycast_NoHit_ReturnsNoHit()
        {
            // Raycast in empty direction
            var @params = new JObject
            {
                ["action"] = "raycast",
                ["origin"] = new JArray { 0, 1000, 0 },
                ["direction"] = new JArray { 0, 1, 0 },
                ["maxDistance"] = 1.0f
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Raycast_ZeroDirection_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "raycast",
                ["origin"] = new JArray { 0, 0, 0 },
                ["direction"] = new JArray { 0, 0, 0 }
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void OverlapSphere_ReturnsResults()
        {
            // Position test object at origin
            _testObject.transform.position = Vector3.zero;

            var @params = new JObject
            {
                ["action"] = "overlap_sphere",
                ["center"] = new JArray { 0, 0, 0 },
                ["radius"] = 10.0f
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void OverlapBox_ReturnsResults()
        {
            _testObject.transform.position = Vector3.zero;

            var @params = new JObject
            {
                ["action"] = "overlap_box",
                ["center"] = new JArray { 0, 0, 0 },
                ["halfExtents"] = new JArray { 5, 5, 5 }
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void CheckSphere_ReturnsResult()
        {
            _testObject.transform.position = Vector3.zero;

            var @params = new JObject
            {
                ["action"] = "check_sphere",
                ["position"] = new JArray { 0, 0, 0 },
                ["radius"] = 10.0f
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Linecast_ReturnsResult()
        {
            var @params = new JObject
            {
                ["action"] = "linecast",
                ["start"] = new JArray { 0, 0, 0 },
                ["end"] = new JArray { 0, 100, 0 }
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void AddForce_ValidRigidbody_AppliesForce()
        {
            var @params = new JObject
            {
                ["action"] = "add_force",
                ["target"] = _testObject.name,
                ["force"] = new JArray { 10, 0, 0 },
                ["mode"] = "impulse"
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void AddTorque_ValidRigidbody_AppliesTorque()
        {
            var @params = new JObject
            {
                ["action"] = "add_torque",
                ["target"] = _testObject.name,
                ["torque"] = new JArray { 0, 10, 0 },
                ["mode"] = "force"
            };

            var result = ManagePhysics.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
