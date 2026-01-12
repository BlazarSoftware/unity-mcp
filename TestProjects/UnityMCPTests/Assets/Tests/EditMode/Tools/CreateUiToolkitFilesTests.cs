using System.IO;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using MCPForUnityTests.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCPForUnityTests.Editor.Tools
{
    public class CreateUiToolkitFilesTests
    {
        private const string TempRoot = "Assets/Temp/CreateUiToolkitFilesTests";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            if (AssetDatabase.IsValidFolder("Assets/Temp"))
            {
                var dirs = Directory.GetDirectories("Assets/Temp");
                var files = Directory.GetFiles("Assets/Temp");
                if (dirs.Length == 0 && files.Length == 0)
                {
                    AssetDatabase.DeleteAsset("Assets/Temp");
                }
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void HandleCommand_WithNullParameters_ReturnsError()
        {
            var error = AssertError(CreateUiToolkitFiles.HandleCommand(null));
            Assert.AreEqual("Parameters cannot be null.", error.Error);
        }

        [Test]
        public void HandleCommand_WhenBaseNameMissing_ReturnsError()
        {
            var error = AssertError(CreateUiToolkitFiles.HandleCommand(new JObject()));
            Assert.AreEqual("Parameter 'baseName' is required.", error.Error);
        }

        [Test]
        public void HandleCommand_WithInvalidBaseName_ReturnsError()
        {
            var result = CreateUiToolkitFiles.HandleCommand(new JObject
            {
                ["baseName"] = "bad name"
            });

            var error = AssertError(result);
            Assert.AreEqual("baseName must contain only letters, numbers, underscores, or hyphens.", error.Error);
        }

        [Test]
        public void HandleCommand_WithInvalidRootElementName_ReturnsError()
        {
            var result = CreateUiToolkitFiles.HandleCommand(new JObject
            {
                ["baseName"] = "ValidName",
                ["rootElementName"] = "1root"
            });

            var error = AssertError(result);
            Assert.AreEqual("rootElementName must start with a letter or underscore and use letters, numbers, underscores, or hyphens.", error.Error);
        }

        [Test]
        public void HandleCommand_WithFolderOutsideAssets_ReturnsError()
        {
            var result = CreateUiToolkitFiles.HandleCommand(new JObject
            {
                ["baseName"] = "ValidName",
                ["folder"] = "../Outside"
            });

            var error = AssertError(result);
            Assert.AreEqual("Folder must be under Assets/. Provided: '../Outside'", error.Error);
        }

        [Test]
        public void HandleCommand_WhenFilesExistAndOverwriteIsFalse_ReturnsError()
        {
            var folder = $"{TempRoot}/Existing";
            var baseName = "ExistingUI";
            TestUtilities.EnsureFolder(folder);

            var uxmlAssetPath = $"{folder}/{baseName}.uxml";
            var ussAssetPath = $"{folder}/{baseName}.uss";
            File.WriteAllText(ToDiskPath(uxmlAssetPath), "<ui />");
            File.WriteAllText(ToDiskPath(ussAssetPath), "/* existing */");

            var result = CreateUiToolkitFiles.HandleCommand(new JObject
            {
                ["baseName"] = baseName,
                ["folder"] = folder
            });

            var error = AssertError(result);
            Assert.IsTrue(error.Error.Contains("Files already exist"), "Expected overwrite warning");
            StringAssert.Contains("UXML exists: True", error.Error);
            StringAssert.Contains("USS exists: True", error.Error);
        }

        [Test]
        public void HandleCommand_CreatesUiToolkitPairAndImports()
        {
            var folder = $"{TempRoot}/Generated";
            var baseName = "MyDoc";
            var rootElement = "appRoot";

            if (AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.DeleteAsset(folder);
            }

            var result = CreateUiToolkitFiles.HandleCommand(new JObject
            {
                ["baseName"] = baseName,
                ["folder"] = folder,
                ["rootElementName"] = rootElement
            });

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.IsTrue(success.Success);
            Assert.AreEqual("Created UI Toolkit files with import validation.", success.Message);

            var data = TestUtilities.ToJObject(success.Data);
            var uxmlPath = data["uxmlPath"]?.ToString();
            var ussPath = data["ussPath"]?.ToString();

            Assert.AreEqual(rootElement, data["rootElementName"]?.ToString());
            Assert.AreEqual(false, data["overwriteApplied"]?.ToObject<bool>());
            Assert.AreEqual(true, data["uxmlImported"]?.ToObject<bool>());
            Assert.AreEqual(true, data["ussImported"]?.ToObject<bool>());

            Assert.NotNull(uxmlPath);
            Assert.NotNull(ussPath);
            Assert.IsTrue(File.Exists(ToDiskPath(uxmlPath)), "UXML file should be written to disk");
            Assert.IsTrue(File.Exists(ToDiskPath(ussPath)), "USS file should be written to disk");

            var uxmlContents = File.ReadAllText(ToDiskPath(uxmlPath));
            var ussContents = File.ReadAllText(ToDiskPath(ussPath));

            StringAssert.Contains($"{baseName}.uss", uxmlContents);
            StringAssert.Contains($"name=\"{rootElement}\"", uxmlContents);
            StringAssert.Contains(":root", ussContents);
            StringAssert.Contains("flex-grow", ussContents);

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);

            Assert.IsNotNull(uxmlAsset, "UXML asset should import successfully");
            Assert.IsNotNull(ussAsset, "USS asset should import successfully");
        }

        private static ErrorResponse AssertError(object result)
        {
            Assert.IsInstanceOf<ErrorResponse>(result);
            var error = (ErrorResponse)result;
            Assert.IsFalse(error.Success);
            return error;
        }

        private static string ToDiskPath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.Combine(projectRoot, assetPath);
        }
    }
}
