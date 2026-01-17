using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles build automation operations including player builds, build settings, and player settings.
    /// </summary>
    [McpForUnityTool("manage_build", AutoRegister = false, Description = "Manage build settings, switch platforms, and build the player.")]
    public static class ManageBuild
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required");
            }

            try
            {
                switch (action)
                {
                    case "ping":
                        return new SuccessResponse("pong", new { tool = "manage_build" });

                    case "build_player":
                        return BuildPlayer(@params);

                    case "get_build_settings":
                        return GetBuildSettings();

                    case "set_scenes":
                        return SetScenes(@params);

                    case "get_player_settings":
                        return GetPlayerSettings(@params);

                    case "set_player_settings":
                        return SetPlayerSettings(@params);

                    case "get_supported_targets":
                        return GetSupportedTargets();

                    case "get_scripting_defines":
                        return GetScriptingDefines(@params);

                    case "set_scripting_defines":
                        return SetScriptingDefines(@params);

                    case "refresh_assets":
                        return RefreshAssets(@params);

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: build_player, get_build_settings, set_scenes, get_player_settings, set_player_settings, get_supported_targets, get_scripting_defines, set_scripting_defines, refresh_assets");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static BuildTarget ParseBuildTarget(string targetStr)
        {
            if (string.IsNullOrEmpty(targetStr))
            {
                return EditorUserBuildSettings.activeBuildTarget;
            }

            switch (targetStr.ToLowerInvariant())
            {
                case "windows":
                case "standalonewindows":
                case "standalonewindows64":
                    return BuildTarget.StandaloneWindows64;
                case "osx":
                case "mac":
                case "macos":
                case "standaloneosx":
                    return BuildTarget.StandaloneOSX;
                case "linux":
                case "standalonelinux64":
                    return BuildTarget.StandaloneLinux64;
                case "android":
                    return BuildTarget.Android;
                case "ios":
                    return BuildTarget.iOS;
                case "webgl":
                    return BuildTarget.WebGL;
                case "tvos":
                    return BuildTarget.tvOS;
                case "ps4":
                    return BuildTarget.PS4;
                case "ps5":
                    return BuildTarget.PS5;
                case "xboxone":
                    return BuildTarget.XboxOne;
                case "switch":
                    return BuildTarget.Switch;
                default:
                    if (Enum.TryParse<BuildTarget>(targetStr, true, out var result))
                    {
                        return result;
                    }
                    return EditorUserBuildSettings.activeBuildTarget;
            }
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.WebGL:
                    return BuildTargetGroup.WebGL;
                case BuildTarget.tvOS:
                    return BuildTargetGroup.tvOS;
                case BuildTarget.PS4:
                    return BuildTargetGroup.PS4;
                case BuildTarget.PS5:
                    return BuildTargetGroup.PS5;
                case BuildTarget.XboxOne:
                    return BuildTargetGroup.XboxOne;
                case BuildTarget.Switch:
                    return BuildTargetGroup.Switch;
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        private static object BuildPlayer(JObject @params)
        {
            string targetStr = @params["target"]?.ToString();
            BuildTarget target = ParseBuildTarget(targetStr);

            string locationPath = @params["locationPath"]?.ToString() ?? @params["path"]?.ToString();
            if (string.IsNullOrEmpty(locationPath))
            {
                // Generate default path
                string extension = "";
                switch (target)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        extension = ".exe";
                        break;
                    case BuildTarget.StandaloneOSX:
                        extension = ".app";
                        break;
                    case BuildTarget.Android:
                        extension = ".apk";
                        break;
                }
                locationPath = Path.Combine("Builds", target.ToString(), PlayerSettings.productName + extension);
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(locationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Get scenes
            string[] scenes = null;
            if (@params["scenes"] != null && @params["scenes"] is JArray scenesArray)
            {
                scenes = scenesArray.Select(s => s.ToString()).ToArray();
            }
            else
            {
                scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
            }

            if (scenes.Length == 0)
            {
                return new ErrorResponse("No scenes to build. Add scenes to Build Settings or provide scenes parameter.");
            }

            // Build options
            BuildOptions options = BuildOptions.None;
            if (@params["development"]?.ToObject<bool>() ?? false)
            {
                options |= BuildOptions.Development;
            }
            if (@params["autoRun"]?.ToObject<bool>() ?? false)
            {
                options |= BuildOptions.AutoRunPlayer;
            }
            if (@params["deepProfiling"]?.ToObject<bool>() ?? false)
            {
                options |= BuildOptions.EnableDeepProfilingSupport;
            }
            if (@params["strictMode"]?.ToObject<bool>() ?? false)
            {
                options |= BuildOptions.StrictMode;
            }
            if (@params["allowDebugging"]?.ToObject<bool>() ?? false)
            {
                options |= BuildOptions.AllowDebugging;
            }
            if (@params["compressTextures"]?.ToObject<bool>() ?? true)
            {
                // CompressWithLz4 is default, CompressWithLz4HC is more compressed
                if (@params["compressHC"]?.ToObject<bool>() ?? false)
                {
                    options |= BuildOptions.CompressWithLz4HC;
                }
                else
                {
                    options |= BuildOptions.CompressWithLz4;
                }
            }

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = target,
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            var errors = new List<object>();
            var warnings = new List<object>();

            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error)
                    {
                        errors.Add(new { content = message.content });
                    }
                    else if (message.type == LogType.Warning)
                    {
                        warnings.Add(new { content = message.content });
                    }
                }
            }

            if (summary.result == BuildResult.Succeeded)
            {
                return new SuccessResponse($"Build succeeded for {target}", new
                {
                    result = summary.result.ToString(),
                    target = target.ToString(),
                    outputPath = summary.outputPath,
                    totalSize = summary.totalSize,
                    totalTime = summary.totalTime.TotalSeconds,
                    totalErrors = summary.totalErrors,
                    totalWarnings = summary.totalWarnings,
                    warnings = warnings
                });
            }
            else
            {
                return new ErrorResponse($"Build failed: {summary.result}", new
                {
                    result = summary.result.ToString(),
                    target = target.ToString(),
                    totalErrors = summary.totalErrors,
                    totalWarnings = summary.totalWarnings,
                    errors = errors,
                    warnings = warnings
                });
            }
        }

        private static object GetBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Select((s, i) => new
            {
                index = i,
                path = s.path,
                enabled = s.enabled,
                guid = s.guid.ToString()
            }).ToList();

            return new SuccessResponse("Retrieved build settings", new
            {
                activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                development = EditorUserBuildSettings.development,
                allowDebugging = EditorUserBuildSettings.allowDebugging,
                buildAppBundle = EditorUserBuildSettings.buildAppBundle,
                scenes = scenes
            });
        }

        private static object SetScenes(JObject @params)
        {
            if (@params["scenes"] == null || !(@params["scenes"] is JArray scenesArray))
            {
                return new ErrorResponse("scenes array is required");
            }

            var newScenes = new List<EditorBuildSettingsScene>();

            foreach (var scene in scenesArray)
            {
                string path = null;
                bool enabled = true;

                if (scene.Type == JTokenType.String)
                {
                    path = scene.ToString();
                }
                else if (scene is JObject sceneObj)
                {
                    path = sceneObj["path"]?.ToString();
                    enabled = sceneObj["enabled"]?.ToObject<bool>() ?? true;
                }

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                // Ensure path starts with Assets/
                if (!path.StartsWith("Assets/"))
                {
                    path = "Assets/" + path;
                }

                // Ensure .unity extension
                if (!path.EndsWith(".unity"))
                {
                    path += ".unity";
                }

                newScenes.Add(new EditorBuildSettingsScene(path, enabled));
            }

            EditorBuildSettings.scenes = newScenes.ToArray();

            return new SuccessResponse($"Set {newScenes.Count} scenes in Build Settings", new
            {
                sceneCount = newScenes.Count,
                scenes = newScenes.Select(s => new { path = s.path, enabled = s.enabled }).ToList()
            });
        }

        private static object GetPlayerSettings(JObject @params)
        {
            string targetStr = @params["target"]?.ToString();
            BuildTarget target = ParseBuildTarget(targetStr);
            BuildTargetGroup targetGroup = GetBuildTargetGroup(target);

            string scriptingBackend = "Unknown";
            string apiCompatLevel = "Unknown";

            try
            {
#if UNITY_6000_0_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget).ToString();
                apiCompatLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget).ToString();
#else
                scriptingBackend = PlayerSettings.GetScriptingBackend(targetGroup).ToString();
                apiCompatLevel = PlayerSettings.GetApiCompatibilityLevel(targetGroup).ToString();
#endif
            }
            catch { }

            return new SuccessResponse("Retrieved player settings", new
            {
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                bundleVersion = PlayerSettings.bundleVersion,
                applicationIdentifier = PlayerSettings.applicationIdentifier,
                defaultIsFullScreen = PlayerSettings.defaultIsNativeResolution,
                runInBackground = PlayerSettings.runInBackground,
                resizableWindow = PlayerSettings.resizableWindow,
                visibleInBackground = PlayerSettings.visibleInBackground,
                allowFullscreenSwitch = PlayerSettings.allowFullscreenSwitch,
                forceSingleInstance = PlayerSettings.forceSingleInstance,
                usePlayerLog = PlayerSettings.usePlayerLog,
                target = target.ToString(),
                targetGroup = targetGroup.ToString(),
                scriptingBackend = scriptingBackend,
                apiCompatibilityLevel = apiCompatLevel,
                colorSpace = PlayerSettings.colorSpace.ToString()
            });
        }

        private static object SetPlayerSettings(JObject @params)
        {
            var changes = new List<string>();

            if (@params["productName"] != null)
            {
                PlayerSettings.productName = @params["productName"].ToString();
                changes.Add($"productName={PlayerSettings.productName}");
            }

            if (@params["companyName"] != null)
            {
                PlayerSettings.companyName = @params["companyName"].ToString();
                changes.Add($"companyName={PlayerSettings.companyName}");
            }

            if (@params["bundleVersion"] != null)
            {
                PlayerSettings.bundleVersion = @params["bundleVersion"].ToString();
                changes.Add($"bundleVersion={PlayerSettings.bundleVersion}");
            }

            if (@params["applicationIdentifier"] != null)
            {
                PlayerSettings.applicationIdentifier = @params["applicationIdentifier"].ToString();
                changes.Add($"applicationIdentifier set");
            }

            if (@params["runInBackground"] != null)
            {
                PlayerSettings.runInBackground = @params["runInBackground"].ToObject<bool>();
                changes.Add($"runInBackground={PlayerSettings.runInBackground}");
            }

            if (@params["resizableWindow"] != null)
            {
                PlayerSettings.resizableWindow = @params["resizableWindow"].ToObject<bool>();
                changes.Add($"resizableWindow={PlayerSettings.resizableWindow}");
            }

            if (@params["allowFullscreenSwitch"] != null)
            {
                PlayerSettings.allowFullscreenSwitch = @params["allowFullscreenSwitch"].ToObject<bool>();
                changes.Add($"allowFullscreenSwitch={PlayerSettings.allowFullscreenSwitch}");
            }

            if (@params["forceSingleInstance"] != null)
            {
                PlayerSettings.forceSingleInstance = @params["forceSingleInstance"].ToObject<bool>();
                changes.Add($"forceSingleInstance={PlayerSettings.forceSingleInstance}");
            }

            if (@params["usePlayerLog"] != null)
            {
                PlayerSettings.usePlayerLog = @params["usePlayerLog"].ToObject<bool>();
                changes.Add($"usePlayerLog={PlayerSettings.usePlayerLog}");
            }

            // Platform-specific settings
            string targetStr = @params["target"]?.ToString();
            BuildTarget target = ParseBuildTarget(targetStr);
            BuildTargetGroup targetGroup = GetBuildTargetGroup(target);

            if (targetGroup != BuildTargetGroup.Unknown)
            {
                if (@params["scriptingBackend"] != null)
                {
                    string backend = @params["scriptingBackend"].ToString().ToLowerInvariant();
                    ScriptingImplementation impl = ScriptingImplementation.Mono2x;
                    switch (backend)
                    {
                        case "mono":
                        case "mono2x":
                            impl = ScriptingImplementation.Mono2x;
                            break;
                        case "il2cpp":
                            impl = ScriptingImplementation.IL2CPP;
                            break;
                    }
#if UNITY_6000_0_OR_NEWER
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                    PlayerSettings.SetScriptingBackend(namedBuildTarget, impl);
#else
                    PlayerSettings.SetScriptingBackend(targetGroup, impl);
#endif
                    changes.Add($"scriptingBackend={impl}");
                }

                if (@params["apiCompatibilityLevel"] != null)
                {
                    string level = @params["apiCompatibilityLevel"].ToString().ToLowerInvariant();
                    ApiCompatibilityLevel api = ApiCompatibilityLevel.NET_Standard_2_0;
                    switch (level)
                    {
                        case "netstandard":
                        case "netstandard2":
                        case "netstandard20":
                        case "netstandard2.0":
                        case "net_standard_2_0":
                            api = ApiCompatibilityLevel.NET_Standard_2_0;
                            break;
                        case "netframework":
                        case "net4":
                        case "net_4_6":
                            api = ApiCompatibilityLevel.NET_Unity_4_8;
                            break;
                    }
#if UNITY_6000_0_OR_NEWER
                    var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                    PlayerSettings.SetApiCompatibilityLevel(namedTarget, api);
#else
                    PlayerSettings.SetApiCompatibilityLevel(targetGroup, api);
#endif
                    changes.Add($"apiCompatibilityLevel={api}");
                }
            }

            if (@params["colorSpace"] != null)
            {
                string cs = @params["colorSpace"].ToString().ToLowerInvariant();
                switch (cs)
                {
                    case "gamma":
                        PlayerSettings.colorSpace = ColorSpace.Gamma;
                        break;
                    case "linear":
                        PlayerSettings.colorSpace = ColorSpace.Linear;
                        break;
                }
                changes.Add($"colorSpace={PlayerSettings.colorSpace}");
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No player settings changed");
            }

            return new SuccessResponse($"Updated player settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }

        private static object GetSupportedTargets()
        {
            var targets = new List<object>();

            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                if (target == BuildTarget.NoTarget) continue;

                bool isSupported = false;
                try
                {
                    isSupported = BuildPipeline.IsBuildTargetSupported(GetBuildTargetGroup(target), target);
                }
                catch
                {
                    // Some targets might throw exceptions
                }

                if (isSupported)
                {
                    targets.Add(new
                    {
                        name = target.ToString(),
                        group = GetBuildTargetGroup(target).ToString()
                    });
                }
            }

            return new SuccessResponse($"Found {targets.Count} supported build targets", new
            {
                activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                supportedTargets = targets
            });
        }

        private static object GetScriptingDefines(JObject @params)
        {
            string targetStr = @params["target"]?.ToString();
            BuildTarget target = ParseBuildTarget(targetStr);
            BuildTargetGroup targetGroup = GetBuildTargetGroup(target);

            if (targetGroup == BuildTargetGroup.Unknown)
            {
                return new ErrorResponse("Unknown or unsupported build target group");
            }

            string[] defines;
#if UNITY_6000_0_OR_NEWER
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out defines);
#else
            string definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            defines = string.IsNullOrEmpty(definesStr) ? new string[0] : definesStr.Split(';');
#endif

            return new SuccessResponse("Retrieved scripting defines", new
            {
                target = target.ToString(),
                targetGroup = targetGroup.ToString(),
                defines = defines
            });
        }

        private static object SetScriptingDefines(JObject @params)
        {
            string targetStr = @params["target"]?.ToString();
            BuildTarget target = ParseBuildTarget(targetStr);
            BuildTargetGroup targetGroup = GetBuildTargetGroup(target);

            if (targetGroup == BuildTargetGroup.Unknown)
            {
                return new ErrorResponse("Unknown or unsupported build target group");
            }

            string[] newDefines = null;

            if (@params["defines"] != null)
            {
                if (@params["defines"] is JArray definesArray)
                {
                    newDefines = definesArray.Select(d => d.ToString()).ToArray();
                }
                else
                {
                    newDefines = @params["defines"].ToString().Split(';', ',');
                }
            }

            // Handle add/remove operations
            if (@params["add"] != null || @params["remove"] != null)
            {
                string[] currentDefines;
#if UNITY_6000_0_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out currentDefines);
#else
                string definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                currentDefines = string.IsNullOrEmpty(definesStr) ? new string[0] : definesStr.Split(';');
#endif
                var definesList = new List<string>(currentDefines);

                if (@params["add"] != null)
                {
                    string[] toAdd = @params["add"] is JArray addArr
                        ? addArr.Select(d => d.ToString()).ToArray()
                        : @params["add"].ToString().Split(';', ',');

                    foreach (var d in toAdd)
                    {
                        string trimmed = d.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !definesList.Contains(trimmed))
                        {
                            definesList.Add(trimmed);
                        }
                    }
                }

                if (@params["remove"] != null)
                {
                    string[] toRemove = @params["remove"] is JArray remArr
                        ? remArr.Select(d => d.ToString()).ToArray()
                        : @params["remove"].ToString().Split(';', ',');

                    foreach (var d in toRemove)
                    {
                        definesList.Remove(d.Trim());
                    }
                }

                newDefines = definesList.ToArray();
            }

            if (newDefines == null)
            {
                return new ErrorResponse("defines, add, or remove parameter is required");
            }

#if UNITY_6000_0_OR_NEWER
            var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", newDefines));
#endif

            return new SuccessResponse($"Set {newDefines.Length} scripting defines for {targetGroup}", new
            {
                target = target.ToString(),
                targetGroup = targetGroup.ToString(),
                defines = newDefines
            });
        }

        private static object RefreshAssets(JObject @params)
        {
            bool importNow = @params["importNow"]?.ToObject<bool>() ?? false;

            if (importNow)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                AssetDatabase.Refresh();
            }

            return new SuccessResponse("Asset database refreshed", new
            {
                importNow = importNow
            });
        }
    }
}
