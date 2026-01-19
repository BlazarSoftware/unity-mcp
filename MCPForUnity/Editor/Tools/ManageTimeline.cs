using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Timeline - create timelines, add tracks and clips.
    /// Note: Requires Timeline package to be installed.
    /// </summary>
    [McpForUnityTool("manage_timeline", AutoRegister = false, Description = "Manage Unity Timeline: create timelines, add tracks. Requires Timeline package.")]
    public static class ManageTimeline
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: check_package, create_timeline");
            }

            try
            {
                switch (action)
                {
                    case "check_package":
                        return CheckTimelinePackage();

                    case "create_timeline":
                        return CreateTimeline(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: check_package, create_timeline");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object CheckTimelinePackage()
        {
            // Check if Timeline package is installed
            bool hasTimeline = false;

            try
            {
                var timelineAssetType = Type.GetType("UnityEngine.Timeline.TimelineAsset, Unity.Timeline");
                hasTimeline = timelineAssetType != null;
            }
            catch { }

            return new SuccessResponse("Checked Timeline package status", new
            {
                hasTimeline = hasTimeline,
                recommendation = !hasTimeline
                    ? "Install Timeline package via Package Manager"
                    : "Timeline package is installed"
            });
        }

        private static object CreateTimeline(JObject @params)
        {
            var timelineAssetType = Type.GetType("UnityEngine.Timeline.TimelineAsset, Unity.Timeline");
            if (timelineAssetType == null)
            {
                return new ErrorResponse("Timeline package is not installed. Install it via Package Manager.");
            }

            string savePath = @params["savePath"]?.ToString();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "Assets/NewTimeline.playable";
            }

            if (!savePath.StartsWith("Assets/"))
            {
                savePath = "Assets/" + savePath;
            }

            if (!savePath.EndsWith(".playable"))
            {
                savePath += ".playable";
            }

            try
            {
                var timeline = ScriptableObject.CreateInstance(timelineAssetType);
                AssetDatabase.CreateAsset(timeline, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return new SuccessResponse($"Created Timeline asset at {savePath}", new
                {
                    path = savePath
                });
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to create Timeline asset: {ex.Message}");
            }
        }
    }
}
