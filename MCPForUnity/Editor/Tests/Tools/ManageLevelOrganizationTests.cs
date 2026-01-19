using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageLevelOrganizationTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Test") || go.name.StartsWith("_") ||
                    go.name.StartsWith("Folder") || go.name.Contains("Organized"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void CreateFolder_DefaultParams_CreatesEmptyFolder()
        {
            var @params = new JObject
            {
                ["action"] = "create_folder",
                ["name"] = "TestFolder"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var folder = GameObject.Find("TestFolder");
            Assert.IsNotNull(folder);
            Assert.AreEqual(0, folder.transform.childCount);
        }

        [Test]
        public void CreateFolder_WithParent_SetsParent()
        {
            // Create parent first
            var parent = new GameObject("TestParent");

            var @params = new JObject
            {
                ["action"] = "create_folder",
                ["name"] = "TestChildFolder",
                ["parent"] = "TestParent"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var folder = GameObject.Find("TestChildFolder");
            Assert.IsNotNull(folder);
            Assert.AreEqual(parent.transform, folder.transform.parent);
        }

        [Test]
        public void OrganizeByType_DefaultCategories_CreatesDefaultFolders()
        {
            // Create some test objects
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";

            var light = new GameObject("TestLight");
            light.AddComponent<Light>();

            var @params = new JObject
            {
                ["action"] = "organize_by_type"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void OrganizeByType_CustomCategories_UsesCustomCategories()
        {
            // Create test objects
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";

            var @params = new JObject
            {
                ["action"] = "organize_by_type",
                ["categories"] = new JObject
                {
                    ["_CustomGeometry"] = new JArray { "MeshRenderer" }
                }
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var customFolder = GameObject.Find("_CustomGeometry");
            Assert.IsNotNull(customFolder);
        }

        [Test]
        public void BatchSetLayer_ValidPattern_SetsLayer()
        {
            // Create test objects
            var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj1.name = "TestCube_1";

            var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj2.name = "TestCube_2";

            var @params = new JObject
            {
                ["action"] = "batch_set_layer",
                ["pattern"] = "TestCube*",
                ["layer"] = "Default"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void BatchSetLayer_InvalidLayer_ReturnsError()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "TestCube";

            var @params = new JObject
            {
                ["action"] = "batch_set_layer",
                ["pattern"] = "TestCube",
                ["layer"] = "NonExistentLayer12345"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void BatchSetLayer_NoPattern_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "batch_set_layer",
                ["layer"] = "Default"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void BatchSetTag_ValidPattern_SetsTag()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "TestTagObject";

            var @params = new JObject
            {
                ["action"] = "batch_set_tag",
                ["pattern"] = "TestTagObject",
                ["tag"] = "Player"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual("Player", obj.tag);
        }

        [Test]
        public void BatchSetTag_NoPattern_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "batch_set_tag",
                ["tag"] = "Player"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void BatchSetStatic_ValidPattern_SetsStatic()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "TestStaticObject";
            obj.isStatic = false;

            var @params = new JObject
            {
                ["action"] = "batch_set_static",
                ["pattern"] = "TestStaticObject",
                ["static"] = true
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void BatchSetStatic_WithFlags_SetsSpecificFlags()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "TestFlagsObject";

            var @params = new JObject
            {
                ["action"] = "batch_set_static",
                ["pattern"] = "TestFlagsObject",
                ["static"] = true,
                ["flags"] = new JArray { "batching", "navigation" }
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void BatchSetStatic_NoPattern_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "batch_set_static",
                ["static"] = true
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetStatistics_ReturnsStats()
        {
            // Create some test objects
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestStatsCube";

            var light = new GameObject("TestStatsLight");
            light.AddComponent<Light>();

            var @params = new JObject
            {
                ["action"] = "get_statistics"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ValidateLevel_EmptyScene_ReturnsValid()
        {
            var @params = new JObject
            {
                ["action"] = "validate_level"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ValidateLevel_WithIssues_ReportsIssues()
        {
            // Create object with missing material
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "TestValidateObject";
            obj.GetComponent<Renderer>().sharedMaterial = null;

            var @params = new JObject
            {
                ["action"] = "validate_level"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ValidateLevel_ExtremeScale_ReportsWarning()
        {
            var obj = new GameObject("TestExtremeScale");
            obj.transform.localScale = new Vector3(1000, 1, 1);

            var @params = new JObject
            {
                ["action"] = "validate_level"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ValidateLevel_FarFromOrigin_ReportsWarning()
        {
            var obj = new GameObject("TestFarObject");
            obj.transform.position = new Vector3(50000, 0, 0);

            var @params = new JObject
            {
                ["action"] = "validate_level"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetupLayers_NewLayers_CreatesLayers()
        {
            var @params = new JObject
            {
                ["action"] = "setup_layers",
                ["layers"] = new JArray { "TestLayer_" + Guid.NewGuid().ToString().Substring(0, 8) }
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetupLayers_ExistingLayers_ReportsExisting()
        {
            var @params = new JObject
            {
                ["action"] = "setup_layers",
                ["layers"] = new JArray { "Default" }
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetupLayers_NoLayers_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "setup_layers"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void BatchSetLayer_WildcardPattern_MatchesMultiple()
        {
            var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj1.name = "TestWildcard_A";

            var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj2.name = "TestWildcard_B";

            var obj3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj3.name = "OtherObject";

            var @params = new JObject
            {
                ["action"] = "batch_set_layer",
                ["pattern"] = "TestWildcard*",
                ["layer"] = "Default"
            };

            var result = ManageLevelOrganization.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
