using System;
using System.IO;
using System.Text.RegularExpressions;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Generates paired UXML/USS files under Assets/ and validates Unity imports.
    /// </summary>
    [McpForUnityTool(
        "create_ui_toolkit_files",
        Description = "Generate a UXML + USS pair under Assets/ and validate import via Unity.",
        AutoRegister = true,
        StructuredOutput = true
    )]
    public static class CreateUiToolkitFiles
    {
        public class Parameters
        {
            [ToolParameter("Target folder under Assets/ for the generated files (default: Assets/UI).", Required = false, DefaultValue = "Assets/UI")]
            public string folder { get; set; }

            [ToolParameter("Base file name (without extension) for the UXML/USS pair.")]
            public string baseName { get; set; }

            [ToolParameter("Set true to overwrite existing files.", Required = false, DefaultValue = "false")]
            public bool? overwrite { get; set; }

            [ToolParameter("Name assigned to the root VisualElement in the generated UXML (default: root).", Required = false, DefaultValue = "root")]
            public string rootElementName { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            string folderInput = @params["folder"]?.ToString();
            string baseName = @params["baseName"]?.ToString();
            string rootElementName = @params["rootElementName"]?.ToString();
            bool overwrite = @params["overwrite"]?.ToObject<bool?>() ?? false;

            if (string.IsNullOrWhiteSpace(baseName))
            {
                return new ErrorResponse("Parameter 'baseName' is required.");
            }

            if (!Regex.IsMatch(baseName, @"^[A-Za-z0-9_-]+$"))
            {
                return new ErrorResponse("baseName must contain only letters, numbers, underscores, or hyphens.");
            }

            rootElementName = string.IsNullOrWhiteSpace(rootElementName) ? "root" : rootElementName.Trim();
            if (!Regex.IsMatch(rootElementName, @"^[A-Za-z_][A-Za-z0-9_-]*$"))
            {
                return new ErrorResponse("rootElementName must start with a letter or underscore and use letters, numbers, underscores, or hyphens.");
            }

            string folder = string.IsNullOrWhiteSpace(folderInput) ? "Assets/UI" : folderInput.Trim();
            if (!TryResolveUnderAssets(folder, out string fullFolderPath, out string assetFolderPath))
            {
                return new ErrorResponse($"Folder must be under Assets/. Provided: '{folder}'");
            }

            try
            {
                Directory.CreateDirectory(fullFolderPath);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Could not create target folder '{fullFolderPath}': {ex.Message}");
            }

            string uxmlFile = Path.Combine(fullFolderPath, $"{baseName}.uxml");
            string ussFile = Path.Combine(fullFolderPath, $"{baseName}.uss");
            string uxmlAssetPath = $"{assetFolderPath}/{baseName}.uxml".Replace('\\', '/');
            string ussAssetPath = $"{assetFolderPath}/{baseName}.uss".Replace('\\', '/');

            if (!overwrite && (File.Exists(uxmlFile) || File.Exists(ussFile)))
            {
                return new ErrorResponse($"Files already exist. Set overwrite=true to replace. UXML exists: {File.Exists(uxmlFile)}, USS exists: {File.Exists(ussFile)}");
            }

            try
            {
                File.WriteAllText(ussFile, BuildUss());
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to write USS file: {ex.Message}");
            }

            AssetDatabase.ImportAsset(ussAssetPath, ImportAssetOptions.ForceUpdate);
            string ussGuid = AssetDatabase.AssetPathToGUID(ussAssetPath);
            string styleSrc = BuildStyleSrc(ussAssetPath, ussGuid);

            try
            {
                File.WriteAllText(uxmlFile, BuildUxml(styleSrc, rootElementName));
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to write UXML file: {ex.Message}");
            }

            AssetDatabase.ImportAsset(uxmlAssetPath, ImportAssetOptions.ForceUpdate);

            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussAssetPath);
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);

            var data = new
            {
                uxmlPath = uxmlAssetPath,
                ussPath = ussAssetPath,
                rootElementName,
                overwriteApplied = overwrite,
                uxmlImported = uxmlAsset != null,
                ussImported = ussAsset != null
            };

            if (uxmlAsset == null || ussAsset == null)
            {
                return new ErrorResponse(
                    "Unity could not import the generated UI Toolkit files. Check the Console for importer errors.",
                    data
                );
            }

            return new SuccessResponse("Created UI Toolkit files with import validation.", data);
        }

        private static bool TryResolveUnderAssets(string inputPath, out string fullPathDir, out string assetPath)
        {
            string assetsRoot = Application.dataPath.Replace('\\', '/');
            string relative = (inputPath ?? string.Empty).Replace('\\', '/').Trim();

            if (relative.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                relative = string.Empty;
            }
            else if (relative.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring("Assets/".Length);
            }

            string combined = Path.Combine(assetsRoot, relative).Replace('\\', '/');
            string normalized = Path.GetFullPath(combined).Replace('\\', '/');

            bool underAssets = normalized.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
            if (!underAssets)
            {
                fullPathDir = null;
                assetPath = null;
                return false;
            }

            try
            {
                var directoryInfo = new DirectoryInfo(normalized);
                while (directoryInfo != null)
                {
                    if (directoryInfo.Exists && (directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        fullPathDir = null;
                        assetPath = null;
                        return false;
                    }

                    if (string.Equals(directoryInfo.FullName.Replace('\\', '/'), assetsRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    directoryInfo = directoryInfo.Parent;
                }
            }
            catch
            {
                // best-effort symlink guard; ignore errors
            }

            fullPathDir = normalized;
            string tail = normalized.Length > assetsRoot.Length ? normalized.Substring(assetsRoot.Length).TrimStart('/') : string.Empty;
            assetPath = ("Assets/" + tail).TrimEnd('/');
            return true;
        }

        private static string BuildStyleSrc(string ussAssetPath, string ussGuid)
        {
            if (!string.IsNullOrEmpty(ussGuid))
            {
                // Standard StyleSheet fileID used by Unity for UXML references
                const string styleSheetFileId = "7433441132597879392";
                var reference = $"project://database/{ussAssetPath}?fileID={styleSheetFileId}&guid={ussGuid}&type=3#StyleSheet";
                return EscapeXmlAttribute(reference);
            }

            // Fallback to relative filename if no guid is available (should be rare)
            return EscapeXmlAttribute(Path.GetFileName(ussAssetPath));
        }

        private static string EscapeXmlAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static string BuildUxml(string styleSrc, string rootElementName)
        {
            return
$@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"" xmlns:uie=""UnityEditor.UIElements"" editor-extension-mode=""False"">
    <Style src=""{styleSrc}"" />
    <ui:VisualElement name=""{rootElementName}"" picking-mode=""Position"" />
</ui:UXML>
";
        }

        private static string BuildUss()
        {
            return
@":root {
    flex-grow: 1;
    /* Add layout and typography rules here */
}
";
        }
    }
}
