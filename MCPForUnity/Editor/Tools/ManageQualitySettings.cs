using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity quality settings and quality levels.
    /// </summary>
    [McpForUnityTool("manage_quality_settings", AutoRegister = false, Description = "Manage quality settings: get/set quality levels, configure graphics settings.")]
    public static class ManageQualitySettings
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: get_levels, get_current, set_level, get_settings, set_settings");
            }

            try
            {
                switch (action)
                {
                    case "get_levels":
                        return GetQualityLevels();

                    case "get_current":
                    case "get_current_level":
                        return GetCurrentLevel();

                    case "set_level":
                        return SetQualityLevel(@params);

                    case "get_settings":
                        return GetQualitySettings();

                    case "set_settings":
                        return SetQualitySettings(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: get_levels, get_current, set_level, get_settings, set_settings");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object GetQualityLevels()
        {
            string[] names = QualitySettings.names;

            var levels = names.Select((name, index) => new
            {
                index = index,
                name = name
            }).ToList();

            return new SuccessResponse($"Retrieved {levels.Count} quality levels", new
            {
                count = levels.Count,
                currentLevel = QualitySettings.GetQualityLevel(),
                levels = levels
            });
        }

        private static object GetCurrentLevel()
        {
            int currentLevel = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;

            return new SuccessResponse("Retrieved current quality level", new
            {
                level = currentLevel,
                name = currentLevel < names.Length ? names[currentLevel] : "Unknown"
            });
        }

        private static object SetQualityLevel(JObject @params)
        {
            if (!@params.ContainsKey("level"))
            {
                return new ErrorResponse("level parameter is required (integer index or string name)");
            }

            int level;
            var levelToken = @params["level"];

            if (levelToken.Type == JTokenType.Integer)
            {
                level = levelToken.ToObject<int>();
            }
            else
            {
                string levelName = levelToken.ToString();
                string[] names = QualitySettings.names;
                level = Array.IndexOf(names, levelName);

                if (level < 0)
                {
                    return new ErrorResponse($"Quality level '{levelName}' not found. Available: {string.Join(", ", names)}");
                }
            }

            QualitySettings.SetQualityLevel(level);

            return new SuccessResponse($"Quality level set to {level}", new
            {
                level = level,
                name = QualitySettings.names[level]
            });
        }

        private static object GetQualitySettings()
        {
            return new SuccessResponse("Retrieved quality settings", new
            {
                pixelLightCount = QualitySettings.pixelLightCount,
                shadows = QualitySettings.shadows.ToString(),
                shadowResolution = QualitySettings.shadowResolution.ToString(),
                shadowProjection = QualitySettings.shadowProjection.ToString(),
                shadowDistance = QualitySettings.shadowDistance,
                shadowCascades = QualitySettings.shadowCascades,
                antiAliasing = QualitySettings.antiAliasing,
                softParticles = QualitySettings.softParticles,
                realtimeReflectionProbes = QualitySettings.realtimeReflectionProbes,
                vSyncCount = QualitySettings.vSyncCount,
                lodBias = QualitySettings.lodBias,
                maximumLODLevel = QualitySettings.maximumLODLevel,
                particleRaycastBudget = QualitySettings.particleRaycastBudget,
                asyncUploadTimeSlice = QualitySettings.asyncUploadTimeSlice,
                asyncUploadBufferSize = QualitySettings.asyncUploadBufferSize,
                asyncUploadPersistentBuffer = QualitySettings.asyncUploadPersistentBuffer
            });
        }

        private static object SetQualitySettings(JObject @params)
        {
            var changes = new System.Collections.Generic.List<string>();

            if (@params["pixelLightCount"] != null)
            {
                QualitySettings.pixelLightCount = @params["pixelLightCount"].ToObject<int>();
                changes.Add($"pixelLightCount={QualitySettings.pixelLightCount}");
            }

            if (@params["shadowDistance"] != null)
            {
                QualitySettings.shadowDistance = @params["shadowDistance"].ToObject<float>();
                changes.Add($"shadowDistance={QualitySettings.shadowDistance}");
            }

            if (@params["antiAliasing"] != null)
            {
                QualitySettings.antiAliasing = @params["antiAliasing"].ToObject<int>();
                changes.Add($"antiAliasing={QualitySettings.antiAliasing}");
            }

            if (@params["vSyncCount"] != null)
            {
                QualitySettings.vSyncCount = @params["vSyncCount"].ToObject<int>();
                changes.Add($"vSyncCount={QualitySettings.vSyncCount}");
            }

            if (@params["lodBias"] != null)
            {
                QualitySettings.lodBias = @params["lodBias"].ToObject<float>();
                changes.Add($"lodBias={QualitySettings.lodBias}");
            }

            if (@params["shadows"] != null)
            {
                if (Enum.TryParse<ShadowQuality>(@params["shadows"].ToString(), true, out var shadows))
                {
                    QualitySettings.shadows = shadows;
                    changes.Add($"shadows={shadows}");
                }
            }

            if (@params["shadowResolution"] != null)
            {
                if (Enum.TryParse<ShadowResolution>(@params["shadowResolution"].ToString(), true, out var shadowRes))
                {
                    QualitySettings.shadowResolution = shadowRes;
                    changes.Add($"shadowResolution={shadowRes}");
                }
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No quality settings changed");
            }

            return new SuccessResponse($"Updated quality settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }
    }
}
