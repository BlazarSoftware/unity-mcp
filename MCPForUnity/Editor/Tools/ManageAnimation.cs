using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages animation clip creation and editing.
    /// </summary>
    [McpForUnityTool("manage_animation", AutoRegister = false, Description = "Create and edit animation clips: create clips, add curves, set keyframes.")]
    public static class ManageAnimation
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: create_clip, add_curve, set_event, get_clip_info");
            }

            try
            {
                switch (action)
                {
                    case "create_clip":
                        return CreateAnimationClip(@params);

                    case "add_curve":
                        return AddCurve(@params);

                    case "set_event":
                    case "add_event":
                        return AddAnimationEvent(@params);

                    case "get_clip_info":
                        return GetClipInfo(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: create_clip, add_curve, set_event, get_clip_info");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object CreateAnimationClip(JObject @params)
        {
            string savePath = @params["savePath"]?.ToString();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "Assets/NewAnimationClip.anim";
            }

            if (!savePath.StartsWith("Assets/"))
            {
                savePath = "Assets/" + savePath;
            }

            if (!savePath.EndsWith(".anim"))
            {
                savePath += ".anim";
            }

            AnimationClip clip = new AnimationClip();
            clip.frameRate = @params["frameRate"]?.ToObject<float>() ?? 60f;

            // Set legacy mode if specified
            bool legacy = @params["legacy"]?.ToObject<bool>() ?? false;
            clip.legacy = legacy;

            AssetDatabase.CreateAsset(clip, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new SuccessResponse($"Created animation clip at {savePath}", new
            {
                path = savePath,
                frameRate = clip.frameRate,
                legacy = clip.legacy,
                length = clip.length
            });
        }

        private static object AddCurve(JObject @params)
        {
            string clipPath = @params["clipPath"]?.ToString();
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("clipPath is required");
            }

            if (!clipPath.StartsWith("Assets/"))
            {
                clipPath = "Assets/" + clipPath;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"Animation clip not found: {clipPath}");
            }

            string propertyPath = @params["propertyPath"]?.ToString();
            if (string.IsNullOrEmpty(propertyPath))
            {
                return new ErrorResponse("propertyPath is required (e.g., 'm_LocalPosition.x')");
            }

            Type propertyType = typeof(Transform);
            string propertyTypeName = @params["propertyType"]?.ToString();
            if (!string.IsNullOrEmpty(propertyTypeName))
            {
                propertyType = Type.GetType(propertyTypeName) ?? typeof(Transform);
            }

            // Get keyframes
            var keyframesArray = @params["keyframes"] as JArray;
            if (keyframesArray == null || keyframesArray.Count == 0)
            {
                return new ErrorResponse("keyframes array is required");
            }

            List<Keyframe> keyframes = new List<Keyframe>();
            foreach (var kf in keyframesArray)
            {
                var kfObj = kf as JObject;
                if (kfObj != null)
                {
                    float time = kfObj["time"]?.ToObject<float>() ?? 0f;
                    float value = kfObj["value"]?.ToObject<float>() ?? 0f;
                    float inTangent = kfObj["inTangent"]?.ToObject<float>() ?? 0f;
                    float outTangent = kfObj["outTangent"]?.ToObject<float>() ?? 0f;

                    keyframes.Add(new Keyframe(time, value, inTangent, outTangent));
                }
            }

            AnimationCurve curve = new AnimationCurve(keyframes.ToArray());

            // Set the curve on the clip
            clip.SetCurve("", propertyType, propertyPath, curve);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Added curve to animation clip: {propertyPath}", new
            {
                clipPath = clipPath,
                propertyPath = propertyPath,
                keyframeCount = keyframes.Count,
                length = clip.length
            });
        }

        private static object AddAnimationEvent(JObject @params)
        {
            string clipPath = @params["clipPath"]?.ToString();
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("clipPath is required");
            }

            if (!clipPath.StartsWith("Assets/"))
            {
                clipPath = "Assets/" + clipPath;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"Animation clip not found: {clipPath}");
            }

            string functionName = @params["functionName"]?.ToString();
            if (string.IsNullOrEmpty(functionName))
            {
                return new ErrorResponse("functionName is required");
            }

            float time = @params["time"]?.ToObject<float>() ?? 0f;

            AnimationEvent animEvent = new AnimationEvent
            {
                time = time,
                functionName = functionName
            };

            // Optional parameters
            if (@params["intParameter"] != null)
            {
                animEvent.intParameter = @params["intParameter"].ToObject<int>();
            }
            if (@params["floatParameter"] != null)
            {
                animEvent.floatParameter = @params["floatParameter"].ToObject<float>();
            }
            if (@params["stringParameter"] != null)
            {
                animEvent.stringParameter = @params["stringParameter"].ToString();
            }

            AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[] { animEvent });

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Added animation event: {functionName} at time {time}", new
            {
                clipPath = clipPath,
                functionName = functionName,
                time = time
            });
        }

        private static object GetClipInfo(JObject @params)
        {
            string clipPath = @params["clipPath"]?.ToString();
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("clipPath is required");
            }

            if (!clipPath.StartsWith("Assets/"))
            {
                clipPath = "Assets/" + clipPath;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"Animation clip not found: {clipPath}");
            }

            var events = AnimationUtility.GetAnimationEvents(clip);

            return new SuccessResponse("Retrieved animation clip info", new
            {
                path = clipPath,
                name = clip.name,
                length = clip.length,
                frameRate = clip.frameRate,
                legacy = clip.legacy,
                looping = clip.isLooping,
                eventCount = events.Length,
                events = System.Array.ConvertAll(events, e => new
                {
                    time = e.time,
                    functionName = e.functionName
                })
            });
        }
    }
}
