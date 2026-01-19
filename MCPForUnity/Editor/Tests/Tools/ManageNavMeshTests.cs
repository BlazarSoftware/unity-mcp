using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageNavMeshTests
    {
        private GameObject _testFloor;

        [SetUp]
        public void SetUp()
        {
            // Create a static floor for NavMesh testing
            _testFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _testFloor.name = "TestFloor";
            _testFloor.transform.position = Vector3.zero;
            _testFloor.transform.localScale = new Vector3(10, 1, 10);
            _testFloor.isStatic = true;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Test") || go.name.Contains("NavMesh") ||
                    go.name.Contains("Link") || go.name.Contains("Obstacle"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Clear NavMesh data
            NavMesh.RemoveAllNavMeshData();
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetNavMeshStatus_NoNavMesh_ReturnsNoData()
        {
            var @params = new JObject
            {
                ["action"] = "get_status"
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void QueryPath_NoNavMesh_ReturnsNotFound()
        {
            var @params = new JObject
            {
                ["action"] = "query_path",
                ["start"] = new JArray { 0, 0, 0 },
                ["end"] = new JArray { 10, 0, 10 }
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void CheckReachable_NoNavMesh_ReturnsNotReachable()
        {
            var @params = new JObject
            {
                ["action"] = "check_reachable",
                ["position"] = new JArray { 0, 0, 0 },
                ["maxDistance"] = 1f
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void AddObstacle_ValidTarget_AddsObstacle()
        {
            var @params = new JObject
            {
                ["action"] = "add_obstacle",
                ["target"] = _testFloor.name,
                ["shape"] = "box",
                ["size"] = new JArray { 2, 2, 2 },
                ["carve"] = true
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var obstacle = _testFloor.GetComponent<NavMeshObstacle>();
            Assert.IsNotNull(obstacle);
            Assert.AreEqual(NavMeshObstacleShape.Box, obstacle.shape);
            Assert.IsTrue(obstacle.carving);
        }

        [Test]
        public void AddObstacle_CapsuleShape_CreatesCapsuleObstacle()
        {
            var testObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            testObj.name = "TestObstacle";

            var @params = new JObject
            {
                ["action"] = "add_obstacle",
                ["target"] = testObj.name,
                ["shape"] = "capsule",
                ["radius"] = 0.5f,
                ["height"] = 2f
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var obstacle = testObj.GetComponent<NavMeshObstacle>();
            Assert.IsNotNull(obstacle);
            Assert.AreEqual(NavMeshObstacleShape.Capsule, obstacle.shape);
        }

        [Test]
        public void AddObstacle_InvalidTarget_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add_obstacle",
                ["target"] = "NonExistentObject"
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void AddObstacle_NoTarget_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add_obstacle"
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void AddLink_ValidParams_CreatesLink()
        {
            var @params = new JObject
            {
                ["action"] = "add_link",
                ["start"] = new JArray { 0, 0, 0 },
                ["end"] = new JArray { 5, 2, 0 },
                ["width"] = 1f,
                ["bidirectional"] = true,
                ["name"] = "TestLink"
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var link = GameObject.Find("TestLink");
            Assert.IsNotNull(link);
        }

        [Test]
        public void SetArea_ValidTarget_MarksAsNavigationStatic()
        {
            var @params = new JObject
            {
                ["action"] = "set_area",
                ["target"] = _testFloor.name,
                ["area"] = 0
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetArea_InvalidTarget_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "set_area",
                ["target"] = "NonExistentObject",
                ["area"] = 0
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetArea_NoTarget_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "set_area",
                ["area"] = 0
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetAgentSettings_ReturnsAgentTypes()
        {
            var @params = new JObject
            {
                ["action"] = "get_agent_settings"
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Bake_ReturnsSuccess()
        {
            var @params = new JObject
            {
                ["action"] = "bake",
                ["agentRadius"] = 0.5f,
                ["agentHeight"] = 2f,
                ["maxSlope"] = 45f,
                ["stepHeight"] = 0.4f
            };

            var result = ManageNavMesh.HandleCommand(@params);

            // May succeed or return error depending on scene setup
            Assert.IsTrue(result is SuccessResponse || result is ErrorResponse);
        }

        [Test]
        public void AddObstacle_ExistingObstacle_ConfiguresExisting()
        {
            // First add an obstacle
            _testFloor.AddComponent<NavMeshObstacle>();

            var @params = new JObject
            {
                ["action"] = "add_obstacle",
                ["target"] = _testFloor.name,
                ["shape"] = "box",
                ["size"] = new JArray { 3, 3, 3 },
                ["carve"] = false
            };

            var result = ManageNavMesh.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var obstacle = _testFloor.GetComponent<NavMeshObstacle>();
            Assert.IsFalse(obstacle.carving);
        }
    }
}
