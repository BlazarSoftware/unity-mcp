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
    public class ManageTerrainTests
    {
        private Terrain _testTerrain;
        private TerrainData _testTerrainData;

        [SetUp]
        public void SetUp()
        {
            // Create a test terrain
            _testTerrainData = new TerrainData();
            _testTerrainData.heightmapResolution = 65;
            _testTerrainData.size = new Vector3(100, 50, 100);

            GameObject terrainObj = Terrain.CreateTerrainGameObject(_testTerrainData);
            terrainObj.name = "TestTerrain";
            _testTerrain = terrainObj.GetComponent<Terrain>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up terrain objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.Contains("Terrain") || go.name.StartsWith("Test"))
                {
                    var terrain = go.GetComponent<Terrain>();
                    if (terrain != null && terrain.terrainData != null)
                    {
                        // Don't destroy asset terrain data
                        if (!AssetDatabase.Contains(terrain.terrainData))
                        {
                            UnityEngine.Object.DestroyImmediate(terrain.terrainData);
                        }
                    }
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Clean up terrain data assets
            string[] assetPaths = AssetDatabase.FindAssets("t:TerrainData Test");
            foreach (var guid in assetPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Test"))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetHeight_ValidPosition_ReturnsHeight()
        {
            var @params = new JObject
            {
                ["action"] = "get_height",
                ["position"] = new JArray { 50, 0, 50 },
                ["target"] = _testTerrain.gameObject.name
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void GetTerrainInfo_ValidTerrain_ReturnsInfo()
        {
            var @params = new JObject
            {
                ["action"] = "get_info",
                ["target"] = _testTerrain.gameObject.name
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetHeight_ValidPosition_SetsHeight()
        {
            var @params = new JObject
            {
                ["action"] = "set_height",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 10f,
                ["radius"] = 5f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Flatten_ValidArea_FlattensTerrain()
        {
            var @params = new JObject
            {
                ["action"] = "flatten",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 5f,
                ["radius"] = 10f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void RaiseLower_ValidArea_ModifiesHeight()
        {
            var @params = new JObject
            {
                ["action"] = "raise_lower",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["delta"] = 5f,
                ["radius"] = 10f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Smooth_ValidArea_SmoothsTerrain()
        {
            // First create some variation
            ManageTerrain.HandleCommand(new JObject
            {
                ["action"] = "set_height",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 20f,
                ["radius"] = 2f
            });

            var @params = new JObject
            {
                ["action"] = "smooth",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["radius"] = 10f,
                ["strength"] = 0.5f,
                ["iterations"] = 2
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void PaintTexture_NoLayers_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "paint_texture",
                ["target"] = _testTerrain.gameObject.name,
                ["layerIndex"] = 0,
                ["position"] = new JArray { 50, 0, 50 },
                ["radius"] = 10f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void AddTree_NoPrototypes_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add_tree",
                ["target"] = _testTerrain.gameObject.name,
                ["prototypeIndex"] = 0,
                ["position"] = new JArray { 50, 0, 50 }
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void AddDetail_NoPrototypes_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add_detail",
                ["target"] = _testTerrain.gameObject.name,
                ["prototypeIndex"] = 0,
                ["position"] = new JArray { 50, 0, 50 }
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void GetTerrainInfo_NoTerrain_ReturnsError()
        {
            // Destroy test terrain first
            UnityEngine.Object.DestroyImmediate(_testTerrain.gameObject);
            _testTerrain = null;

            var @params = new JObject
            {
                ["action"] = "get_info",
                ["target"] = "NonExistentTerrain"
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetHeight_NoTerrain_ReturnsError()
        {
            UnityEngine.Object.DestroyImmediate(_testTerrain.gameObject);
            _testTerrain = null;

            var @params = new JObject
            {
                ["action"] = "set_height",
                ["target"] = "NonExistentTerrain",
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 10f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetHeight_SinglePoint_SetsHeight()
        {
            var @params = new JObject
            {
                ["action"] = "set_height",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 25f,
                ["radius"] = 0f // Single point
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void RaiseLower_NegativeDelta_LowersTerrain()
        {
            // First raise the terrain
            ManageTerrain.HandleCommand(new JObject
            {
                ["action"] = "set_height",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["targetHeight"] = 20f,
                ["radius"] = 10f
            });

            var @params = new JObject
            {
                ["action"] = "raise_lower",
                ["target"] = _testTerrain.gameObject.name,
                ["position"] = new JArray { 50, 0, 50 },
                ["delta"] = -5f,
                ["radius"] = 10f
            };

            var result = ManageTerrain.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
