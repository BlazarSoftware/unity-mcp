using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageAssetImportTests
    {
        private string testTexturePath = "Assets/TestTexture_ManageAssetImport.png";

        [SetUp]
        public void SetUp()
        {
            // Create a test texture
            Texture2D tex = new Texture2D(64, 64);
            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(testTexturePath, pngData);
            AssetDatabase.ImportAsset(testTexturePath);
            Object.DestroyImmediate(tex);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test asset
            if (File.Exists(testTexturePath))
            {
                AssetDatabase.DeleteAsset(testTexturePath);
            }
        }

        [Test]
        public void GetImportStatus_ReturnsStatus()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "get_status"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["isImporting"]);
        }

        [Test]
        public void ReimportAsset_ReimportsTexture()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "reimport",
                ["assetPath"] = testTexturePath
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.AreEqual(testTexturePath, data.Value<string>("assetPath"));
        }

        [Test]
        public void GetImportSettings_ReturnsTextureSettings()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "get_settings",
                ["assetPath"] = testTexturePath
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.AreEqual("TextureImporter", data.Value<string>("importerType"));
            Assert.IsNotNull(data["textureType"]);
            Assert.IsNotNull(data["maxTextureSize"]);
        }

        [Test]
        public void SetTextureImportSettings_UpdatesSettings()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "set_texture_settings",
                ["assetPath"] = testTexturePath,
                ["maxTextureSize"] = 512,
                ["isReadable"] = true
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);

            // Verify settings were applied
            TextureImporter importer = AssetImporter.GetAtPath(testTexturePath) as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.AreEqual(512, importer.maxTextureSize);
            Assert.IsTrue(importer.isReadable);
        }

        [Test]
        public void GetAssetDependencies_ReturnsDependencies()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "get_dependencies",
                ["assetPath"] = testTexturePath,
                ["recursive"] = false
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["dependencies"]);
            Assert.AreEqual(testTexturePath, data.Value<string>("assetPath"));
        }

        [Test]
        public void ReimportAsset_InvalidPath_ReturnsError()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "reimport",
                ["assetPath"] = "Assets/NonExistent.png"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }

        [Test]
        public void GetImportSettings_MissingAssetPath_ReturnsError()
        {
            // Act
            var result = ToJObject(ManageAssetImport.HandleCommand(new JObject
            {
                ["action"] = "get_settings"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }
    }
}
