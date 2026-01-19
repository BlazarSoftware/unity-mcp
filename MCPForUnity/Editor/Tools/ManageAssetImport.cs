using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages asset importing - reimport assets, get import status, configure import settings.
    /// </summary>
    [McpForUnityTool("manage_asset_import", AutoRegister = false, Description = "Control asset import: reimport, get status, configure import settings.")]
    public static class ManageAssetImport
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: import, reimport, get_status, get_settings, set_texture_settings, get_dependencies");
            }

            try
            {
                switch (action)
                {
                    case "import":
                    case "reimport":
                        return ReimportAsset(@params);

                    case "get_status":
                        return GetImportStatus();

                    case "get_settings":
                        return GetImportSettings(@params);

                    case "set_texture_settings":
                        return SetTextureImportSettings(@params);

                    case "set_model_settings":
                        return SetModelImportSettings(@params);

                    case "set_audio_settings":
                        return SetAudioImportSettings(@params);

                    case "get_dependencies":
                        return GetAssetDependencies(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: import, reimport, get_status, get_settings, set_texture_settings, set_model_settings, set_audio_settings, get_dependencies");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object ReimportAsset(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
            {
                return new ErrorResponse($"Asset not found: {assetPath}");
            }

            ImportAssetOptions options = ImportAssetOptions.Default;
            if (@params["forceUpdate"]?.ToObject<bool>() ?? false)
            {
                options |= ImportAssetOptions.ForceUpdate;
            }
            if (@params["forceSynchronousImport"]?.ToObject<bool>() ?? false)
            {
                options |= ImportAssetOptions.ForceSynchronousImport;
            }
            if (@params["importRecursive"]?.ToObject<bool>() ?? false)
            {
                options |= ImportAssetOptions.ImportRecursive;
            }

            AssetDatabase.ImportAsset(assetPath, options);

            return new SuccessResponse($"Asset imported: {assetPath}", new
            {
                assetPath = assetPath,
                options = options.ToString()
            });
        }

        private static object GetImportStatus()
        {
            bool isImporting = AssetDatabase.IsAssetImportWorkerProcess();

            return new SuccessResponse("Retrieved import status", new
            {
                isImporting = isImporting,
                canOpenAssetInEditor = !EditorApplication.isCompiling && !EditorApplication.isUpdating
            });
        }

        private static object GetImportSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return new ErrorResponse($"No importer found for: {assetPath}");
            }

            var settings = new Dictionary<string, object>
            {
                ["assetPath"] = assetPath,
                ["importerType"] = importer.GetType().Name,
                ["assetBundleName"] = importer.assetBundleName,
                ["userData"] = importer.userData
            };

            // Type-specific settings
            if (importer is TextureImporter texImporter)
            {
                settings["textureType"] = texImporter.textureType.ToString();
                settings["maxTextureSize"] = texImporter.maxTextureSize;
                settings["textureCompression"] = texImporter.textureCompression.ToString();
                settings["isReadable"] = texImporter.isReadable;
                settings["mipmapEnabled"] = texImporter.mipmapEnabled;
            }
            else if (importer is ModelImporter modelImporter)
            {
                settings["importAnimation"] = modelImporter.importAnimation;
                settings["materialImportMode"] = modelImporter.materialImportMode.ToString();
                settings["globalScale"] = modelImporter.globalScale;
                settings["meshCompression"] = modelImporter.meshCompression.ToString();
            }
            else if (importer is AudioImporter audioImporter)
            {
                settings["loadInBackground"] = audioImporter.loadInBackground;
                // preloadAudioData is now in SampleSettings, skip for simplicity
            }

            return new SuccessResponse("Retrieved import settings", settings);
        }

        private static object SetTextureImportSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a texture asset: {assetPath}");
            }

            var changes = new List<string>();

            if (@params["textureType"] != null)
            {
                if (Enum.TryParse<TextureImporterType>(@params["textureType"].ToString(), true, out var textureType))
                {
                    importer.textureType = textureType;
                    changes.Add($"textureType={textureType}");
                }
            }

            if (@params["maxTextureSize"] != null)
            {
                importer.maxTextureSize = @params["maxTextureSize"].ToObject<int>();
                changes.Add($"maxTextureSize={importer.maxTextureSize}");
            }

            if (@params["isReadable"] != null)
            {
                importer.isReadable = @params["isReadable"].ToObject<bool>();
                changes.Add($"isReadable={importer.isReadable}");
            }

            if (@params["mipmapEnabled"] != null)
            {
                importer.mipmapEnabled = @params["mipmapEnabled"].ToObject<bool>();
                changes.Add($"mipmapEnabled={importer.mipmapEnabled}");
            }

            if (@params["textureCompression"] != null)
            {
                if (Enum.TryParse<TextureImporterCompression>(@params["textureCompression"].ToString(), true, out var compression))
                {
                    importer.textureCompression = compression;
                    changes.Add($"textureCompression={compression}");
                }
            }

            if (changes.Count > 0)
            {
                importer.SaveAndReimport();
            }

            return new SuccessResponse($"Updated texture import settings: {string.Join(", ", changes)}", new
            {
                assetPath = assetPath,
                changes = changes
            });
        }

        private static object SetModelImportSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset: {assetPath}");
            }

            var changes = new List<string>();

            if (@params["globalScale"] != null)
            {
                importer.globalScale = @params["globalScale"].ToObject<float>();
                changes.Add($"globalScale={importer.globalScale}");
            }

            if (@params["importAnimation"] != null)
            {
                importer.importAnimation = @params["importAnimation"].ToObject<bool>();
                changes.Add($"importAnimation={importer.importAnimation}");
            }

            if (@params["materialImportMode"] != null)
            {
                if (Enum.TryParse<ModelImporterMaterialImportMode>(@params["materialImportMode"].ToString(), true, out var matImportMode))
                {
                    importer.materialImportMode = matImportMode;
                    changes.Add($"materialImportMode={matImportMode}");
                }
            }

            if (@params["meshCompression"] != null)
            {
                if (Enum.TryParse<ModelImporterMeshCompression>(@params["meshCompression"].ToString(), true, out var compression))
                {
                    importer.meshCompression = compression;
                    changes.Add($"meshCompression={compression}");
                }
            }

            if (changes.Count > 0)
            {
                importer.SaveAndReimport();
            }

            return new SuccessResponse($"Updated model import settings: {string.Join(", ", changes)}", new
            {
                assetPath = assetPath,
                changes = changes
            });
        }

        private static object SetAudioImportSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not an audio asset: {assetPath}");
            }

            var changes = new List<string>();

            if (@params["loadInBackground"] != null)
            {
                importer.loadInBackground = @params["loadInBackground"].ToObject<bool>();
                changes.Add($"loadInBackground={importer.loadInBackground}");
            }

            // preloadAudioData is obsolete - now in SampleSettings per platform
            // Skipping for backward compatibility

            if (changes.Count > 0)
            {
                importer.SaveAndReimport();
            }

            return new SuccessResponse($"Updated audio import settings: {string.Join(", ", changes)}", new
            {
                assetPath = assetPath,
                changes = changes
            });
        }

        private static object GetAssetDependencies(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = "Assets/" + assetPath;
            }

            bool recursive = @params["recursive"]?.ToObject<bool>() ?? false;

            string[] dependencies = recursive
                ? AssetDatabase.GetDependencies(assetPath, true)
                : AssetDatabase.GetDependencies(assetPath, false);

            return new SuccessResponse($"Found {dependencies.Length} dependencies", new
            {
                assetPath = assetPath,
                recursive = recursive,
                count = dependencies.Length,
                dependencies = dependencies
            });
        }
    }
}
