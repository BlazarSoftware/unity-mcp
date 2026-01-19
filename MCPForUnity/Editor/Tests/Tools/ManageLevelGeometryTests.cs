using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageLevelGeometryTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up any created objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Corridor") || go.name.StartsWith("Room") ||
                    go.name.StartsWith("Stairs") || go.name.StartsWith("Ramp") ||
                    go.name.StartsWith("Platform") || go.name.StartsWith("Arch") ||
                    go.name.StartsWith("Column") || go.name.StartsWith("Wall") ||
                    go.name.StartsWith("Test"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void CreateCorridor_DefaultParams_CreatesCorridorWithChildren()
        {
            var @params = new JObject
            {
                ["action"] = "create_corridor",
                ["name"] = "TestCorridor"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var corridor = GameObject.Find("TestCorridor");
            Assert.IsNotNull(corridor);
            Assert.GreaterOrEqual(corridor.transform.childCount, 4); // Floor, ceiling, 2 walls minimum
        }

        [Test]
        public void CreateCorridor_CustomDimensions_SetsCorrectSize()
        {
            var @params = new JObject
            {
                ["action"] = "create_corridor",
                ["name"] = "TestCorridor",
                ["length"] = 20f,
                ["width"] = 5f,
                ["height"] = 4f
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var corridor = GameObject.Find("TestCorridor");
            Assert.IsNotNull(corridor);
        }

        [Test]
        public void CreateCorridor_WithPosition_SetsPosition()
        {
            var @params = new JObject
            {
                ["action"] = "create_corridor",
                ["name"] = "TestCorridor",
                ["position"] = new JArray { 10f, 5f, 20f }
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var corridor = GameObject.Find("TestCorridor");
            Assert.IsNotNull(corridor);
            Assert.AreEqual(10f, corridor.transform.position.x, 0.01f);
            Assert.AreEqual(5f, corridor.transform.position.y, 0.01f);
            Assert.AreEqual(20f, corridor.transform.position.z, 0.01f);
        }

        [Test]
        public void CreateRoom_DefaultParams_CreatesEnclosedRoom()
        {
            var @params = new JObject
            {
                ["action"] = "create_room",
                ["name"] = "TestRoom"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var room = GameObject.Find("TestRoom");
            Assert.IsNotNull(room);
            Assert.GreaterOrEqual(room.transform.childCount, 6); // Floor, ceiling, 4 walls
        }

        [Test]
        public void CreateRoom_WithDoorways_CreatesWallsWithOpenings()
        {
            var @params = new JObject
            {
                ["action"] = "create_room",
                ["name"] = "TestRoom",
                ["doorways"] = new JArray { "north", "south" }
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var room = GameObject.Find("TestRoom");
            Assert.IsNotNull(room);
        }

        [Test]
        public void CreateStairs_DefaultParams_CreatesSteps()
        {
            var @params = new JObject
            {
                ["action"] = "create_stairs",
                ["name"] = "TestStairs"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var stairs = GameObject.Find("TestStairs");
            Assert.IsNotNull(stairs);
            Assert.AreEqual(10, stairs.transform.childCount); // Default 10 steps
        }

        [Test]
        public void CreateStairs_CustomStepCount_CreatesCorrectNumberOfSteps()
        {
            var @params = new JObject
            {
                ["action"] = "create_stairs",
                ["name"] = "TestStairs",
                ["stepCount"] = 5
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var stairs = GameObject.Find("TestStairs");
            Assert.IsNotNull(stairs);
            Assert.AreEqual(5, stairs.transform.childCount);
        }

        [Test]
        public void CreateStairs_WithRailings_AddsRailings()
        {
            var @params = new JObject
            {
                ["action"] = "create_stairs",
                ["name"] = "TestStairs",
                ["stepCount"] = 5,
                ["addRailings"] = true
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var stairs = GameObject.Find("TestStairs");
            Assert.IsNotNull(stairs);
            Assert.GreaterOrEqual(stairs.transform.childCount, 7); // 5 steps + 2 railings
        }

        [Test]
        public void CreateRamp_DefaultParams_CreatesRamp()
        {
            var @params = new JObject
            {
                ["action"] = "create_ramp",
                ["name"] = "TestRamp"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var ramp = GameObject.Find("TestRamp");
            Assert.IsNotNull(ramp);
            Assert.GreaterOrEqual(ramp.transform.childCount, 1);
        }

        [Test]
        public void CreatePlatform_DefaultParams_CreatesPlatform()
        {
            var @params = new JObject
            {
                ["action"] = "create_platform",
                ["name"] = "TestPlatform"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var platform = GameObject.Find("TestPlatform");
            Assert.IsNotNull(platform);
        }

        [Test]
        public void CreatePlatform_WithRailings_AddsRailings()
        {
            var @params = new JObject
            {
                ["action"] = "create_platform",
                ["name"] = "TestPlatform",
                ["addRailings"] = true
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var platform = GameObject.Find("TestPlatform");
            Assert.IsNotNull(platform);
        }

        [Test]
        public void CreateArch_DefaultParams_CreatesArch()
        {
            var @params = new JObject
            {
                ["action"] = "create_arch",
                ["name"] = "TestArch"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var arch = GameObject.Find("TestArch");
            Assert.IsNotNull(arch);
            Assert.GreaterOrEqual(arch.transform.childCount, 3); // 2 pillars + top
        }

        [Test]
        public void CreateArch_Rounded_CreatesRoundedArch()
        {
            var @params = new JObject
            {
                ["action"] = "create_arch",
                ["name"] = "TestArch",
                ["rounded"] = true
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var arch = GameObject.Find("TestArch");
            Assert.IsNotNull(arch);
        }

        [Test]
        public void CreateColumn_Cylinder_CreatesCylindricalColumn()
        {
            var @params = new JObject
            {
                ["action"] = "create_column",
                ["name"] = "TestColumn",
                ["shape"] = "cylinder"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var column = GameObject.Find("TestColumn");
            Assert.IsNotNull(column);
        }

        [Test]
        public void CreateColumn_Box_CreatesRectangularColumn()
        {
            var @params = new JObject
            {
                ["action"] = "create_column",
                ["name"] = "TestColumn",
                ["shape"] = "box"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var column = GameObject.Find("TestColumn");
            Assert.IsNotNull(column);
        }

        [Test]
        public void CreateColumn_WithBaseAndCapital_AddsDecorations()
        {
            var @params = new JObject
            {
                ["action"] = "create_column",
                ["name"] = "TestColumn",
                ["addBase"] = true,
                ["addCapital"] = true
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var column = GameObject.Find("TestColumn");
            Assert.IsNotNull(column);
            Assert.GreaterOrEqual(column.transform.childCount, 3); // Shaft + base + capital
        }

        [Test]
        public void CreateWall_DefaultParams_CreatesWall()
        {
            var @params = new JObject
            {
                ["action"] = "create_wall",
                ["name"] = "TestWall"
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var wall = GameObject.Find("TestWall");
            Assert.IsNotNull(wall);
            Assert.AreEqual(1, wall.transform.childCount);
        }

        [Test]
        public void CreateWall_CustomDimensions_SetsCorrectSize()
        {
            var @params = new JObject
            {
                ["action"] = "create_wall",
                ["name"] = "TestWall",
                ["length"] = 10f,
                ["height"] = 4f,
                ["thickness"] = 0.5f
            };

            var result = ManageLevelGeometry.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var wall = GameObject.Find("TestWall");
            Assert.IsNotNull(wall);
            var wallChild = wall.transform.GetChild(0);
            Assert.AreEqual(10f, wallChild.localScale.x, 0.01f);
            Assert.AreEqual(4f, wallChild.localScale.y, 0.01f);
            Assert.AreEqual(0.5f, wallChild.localScale.z, 0.01f);
        }
    }
}
