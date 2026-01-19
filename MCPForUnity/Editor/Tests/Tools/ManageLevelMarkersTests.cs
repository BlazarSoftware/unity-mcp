using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using MCPForUnity.Runtime.Components;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageLevelMarkersTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up marker parent and all markers
            var markersParent = GameObject.Find("_LevelMarkers");
            if (markersParent != null)
            {
                UnityEngine.Object.DestroyImmediate(markersParent);
            }

            // Clean up any stray marker objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.Contains("Spawn") || go.name.Contains("Waypoint") ||
                    go.name.Contains("Trigger") || go.name.Contains("Checkpoint") ||
                    go.name.Contains("Objective") || go.name.StartsWith("Test"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void CreateSpawnPoint_DefaultParams_CreatesPlayerSpawn()
        {
            var @params = new JObject
            {
                ["action"] = "create_spawn_point",
                ["name"] = "TestSpawn"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnPoint>();
            Assert.IsNotNull(spawn);
            Assert.AreEqual(SpawnPoint.SpawnType.Player, spawn.Type);
        }

        [Test]
        public void CreateSpawnPoint_EnemyType_CreatesEnemySpawn()
        {
            var @params = new JObject
            {
                ["action"] = "create_spawn_point",
                ["name"] = "TestEnemySpawn",
                ["spawnType"] = "enemy",
                ["team"] = 2
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnPoint>();
            Assert.IsNotNull(spawn);
            Assert.AreEqual(SpawnPoint.SpawnType.Enemy, spawn.Type);
            Assert.AreEqual(2, spawn.Team);
        }

        [Test]
        public void CreateSpawnPoint_WithPosition_SetsPosition()
        {
            var @params = new JObject
            {
                ["action"] = "create_spawn_point",
                ["name"] = "TestSpawn",
                ["position"] = new JArray { 10f, 0f, 20f }
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnPoint>();
            Assert.IsNotNull(spawn);
            Assert.AreEqual(10f, spawn.transform.position.x, 0.01f);
            Assert.AreEqual(20f, spawn.transform.position.z, 0.01f);
        }

        [Test]
        public void CreateWaypoint_DefaultParams_CreatesWaypoint()
        {
            var @params = new JObject
            {
                ["action"] = "create_waypoint",
                ["name"] = "TestWaypoint"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var waypoint = UnityEngine.Object.FindFirstObjectByType<Waypoint>();
            Assert.IsNotNull(waypoint);
        }

        [Test]
        public void CreateWaypoint_WithAction_SetsAction()
        {
            var @params = new JObject
            {
                ["action"] = "create_waypoint",
                ["name"] = "TestWaypoint",
                ["waypointAction"] = "patrol",
                ["waitTime"] = 2f
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var waypoint = UnityEngine.Object.FindFirstObjectByType<Waypoint>();
            Assert.IsNotNull(waypoint);
            Assert.AreEqual(Waypoint.WaypointAction.Patrol, waypoint.Action);
            Assert.AreEqual(2f, waypoint.WaitTime, 0.01f);
        }

        [Test]
        public void CreateTriggerVolume_BoxShape_CreatesBoxTrigger()
        {
            var @params = new JObject
            {
                ["action"] = "create_trigger_volume",
                ["name"] = "TestTrigger",
                ["eventName"] = "OnTestTrigger",
                ["shape"] = "box",
                ["size"] = new JArray { 3f, 2f, 3f }
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var trigger = UnityEngine.Object.FindFirstObjectByType<TriggerVolume>();
            Assert.IsNotNull(trigger);
            Assert.AreEqual("OnTestTrigger", trigger.EventName);
            var boxCollider = trigger.GetComponent<BoxCollider>();
            Assert.IsNotNull(boxCollider);
            Assert.IsTrue(boxCollider.isTrigger);
        }

        [Test]
        public void CreateTriggerVolume_SphereShape_CreatesSphereCollider()
        {
            var @params = new JObject
            {
                ["action"] = "create_trigger_volume",
                ["name"] = "TestTrigger",
                ["shape"] = "sphere",
                ["radius"] = 5f
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var trigger = UnityEngine.Object.FindFirstObjectByType<TriggerVolume>();
            Assert.IsNotNull(trigger);
            var sphereCollider = trigger.GetComponent<SphereCollider>();
            Assert.IsNotNull(sphereCollider);
            Assert.AreEqual(5f, sphereCollider.radius, 0.01f);
        }

        [Test]
        public void CreateCheckpoint_DefaultParams_CreatesCheckpoint()
        {
            var @params = new JObject
            {
                ["action"] = "create_checkpoint",
                ["name"] = "TestCheckpoint",
                ["sequenceOrder"] = 1
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var checkpoint = UnityEngine.Object.FindFirstObjectByType<Checkpoint>();
            Assert.IsNotNull(checkpoint);
            Assert.AreEqual(1, checkpoint.SequenceOrder);
        }

        [Test]
        public void CreateObjectiveMarker_DefaultParams_CreatesObjective()
        {
            var @params = new JObject
            {
                ["action"] = "create_objective_marker",
                ["name"] = "TestObjective",
                ["displayName"] = "Find the Key",
                ["objectiveType"] = "primary",
                ["pointValue"] = 500
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var objective = UnityEngine.Object.FindFirstObjectByType<ObjectiveMarker>();
            Assert.IsNotNull(objective);
            Assert.AreEqual("Find the Key", objective.DisplayName);
            Assert.AreEqual(ObjectiveMarker.ObjectiveType.Primary, objective.Type);
            Assert.AreEqual(500, objective.PointValue);
        }

        [Test]
        public void GetMarkers_ByType_ReturnsCorrectMarkers()
        {
            // Create some markers first
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_spawn_point", ["name"] = "Spawn1" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_spawn_point", ["name"] = "Spawn2" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WP1" });

            var @params = new JObject
            {
                ["action"] = "get_markers",
                ["markerType"] = "spawn"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetMarkers_All_ReturnsAllMarkers()
        {
            // Create markers of different types
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_spawn_point", ["name"] = "Spawn1" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WP1" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_checkpoint", ["name"] = "CP1" });

            var @params = new JObject
            {
                ["action"] = "get_markers",
                ["markerType"] = "all"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ConnectWaypoints_ValidWaypoints_CreatesConnections()
        {
            // Create waypoints
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WP1", ["position"] = new JArray { 0, 0, 0 } });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WP2", ["position"] = new JArray { 5, 0, 0 } });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WP3", ["position"] = new JArray { 10, 0, 0 } });

            var @params = new JObject
            {
                ["action"] = "connect_waypoints",
                ["waypoints"] = new JArray { "WP1", "WP2", "WP3" },
                ["loop"] = false
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var waypoints = UnityEngine.Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
            var wp1 = waypoints.FirstOrDefault(w => w.gameObject.name == "WP1");
            Assert.IsNotNull(wp1);
            Assert.GreaterOrEqual(wp1.Connections.Count, 1);
        }

        [Test]
        public void ConnectWaypoints_WithLoop_CreatesCircularPath()
        {
            // Create waypoints
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WPLoop1" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WPLoop2" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "WPLoop3" });

            var @params = new JObject
            {
                ["action"] = "connect_waypoints",
                ["waypoints"] = new JArray { "WPLoop1", "WPLoop2", "WPLoop3" },
                ["loop"] = true
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ConnectWaypoints_InsufficientWaypoints_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "connect_waypoints",
                ["waypoints"] = new JArray { "WP1" }
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void ValidateMarkers_NoMarkers_ReturnsWarnings()
        {
            var @params = new JObject
            {
                ["action"] = "validate_markers"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ValidateMarkers_WithValidMarkers_ReturnsValid()
        {
            // Create a valid set of markers
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_spawn_point", ["name"] = "PlayerSpawn", ["spawnType"] = "player" });

            var @params = new JObject
            {
                ["action"] = "validate_markers"
            };

            var result = ManageLevelMarkers.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void MarkersOrganizedUnderParent_CreatesHierarchy()
        {
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_spawn_point", ["name"] = "TestSpawn" });
            ManageLevelMarkers.HandleCommand(new JObject { ["action"] = "create_waypoint", ["name"] = "TestWP" });

            var markersParent = GameObject.Find("_LevelMarkers");
            Assert.IsNotNull(markersParent);
            Assert.GreaterOrEqual(markersParent.transform.childCount, 2); // SpawnPoints and Waypoints folders
        }
    }
}
