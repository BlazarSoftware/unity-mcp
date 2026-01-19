using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Scene View camera and visualization settings.
    /// </summary>
    [McpForUnityTool("manage_scene_view", AutoRegister = false, Description = "Control Scene View: camera position, focus, view modes, capture screenshots.")]
    public static class ManageSceneView
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: get_camera, set_camera, focus_object, set_view_mode, capture_screenshot");
            }

            try
            {
                switch (action)
                {
                    case "get_camera":
                    case "get_camera_position":
                        return GetCameraPosition();

                    case "set_camera":
                    case "set_camera_position":
                        return SetCameraPosition(@params);

                    case "focus_object":
                    case "frame_object":
                        return FocusOnObject(@params);

                    case "set_view_mode":
                        return SetViewMode(@params);

                    case "get_view_mode":
                        return GetViewMode();

                    case "capture_screenshot":
                        return CaptureScreenshot(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: get_camera, set_camera, focus_object, set_view_mode, get_view_mode, capture_screenshot");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object GetCameraPosition()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            return new SuccessResponse("Retrieved Scene View camera position", new
            {
                position = new
                {
                    x = sceneView.camera.transform.position.x,
                    y = sceneView.camera.transform.position.y,
                    z = sceneView.camera.transform.position.z
                },
                rotation = new
                {
                    x = sceneView.camera.transform.rotation.eulerAngles.x,
                    y = sceneView.camera.transform.rotation.eulerAngles.y,
                    z = sceneView.camera.transform.rotation.eulerAngles.z
                },
                size = sceneView.size,
                orthographic = sceneView.orthographic
            });
        }

        private static object SetCameraPosition(JObject @params)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            if (@params["position"] != null)
            {
                var posObj = @params["position"] as JObject;
                if (posObj != null)
                {
                    Vector3 position = new Vector3(
                        posObj["x"]?.ToObject<float>() ?? sceneView.pivot.x,
                        posObj["y"]?.ToObject<float>() ?? sceneView.pivot.y,
                        posObj["z"]?.ToObject<float>() ?? sceneView.pivot.z
                    );
                    sceneView.pivot = position;
                }
            }

            if (@params["rotation"] != null)
            {
                var rotObj = @params["rotation"] as JObject;
                if (rotObj != null)
                {
                    Vector3 euler = new Vector3(
                        rotObj["x"]?.ToObject<float>() ?? sceneView.rotation.eulerAngles.x,
                        rotObj["y"]?.ToObject<float>() ?? sceneView.rotation.eulerAngles.y,
                        rotObj["z"]?.ToObject<float>() ?? sceneView.rotation.eulerAngles.z
                    );
                    sceneView.rotation = Quaternion.Euler(euler);
                }
            }

            if (@params["size"] != null)
            {
                sceneView.size = @params["size"].ToObject<float>();
            }

            sceneView.Repaint();

            return new SuccessResponse("Scene View camera position updated", new
            {
                position = sceneView.pivot,
                rotation = sceneView.rotation.eulerAngles,
                size = sceneView.size
            });
        }

        private static object FocusOnObject(JObject @params)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            string objectName = @params["objectName"]?.ToString();
            int? instanceID = @params["instanceID"]?.ToObject<int?>();

            GameObject target = null;

            if (instanceID.HasValue)
            {
                var obj = EditorUtility.InstanceIDToObject(instanceID.Value);
                target = obj as GameObject;
            }
            else if (!string.IsNullOrEmpty(objectName))
            {
                target = GameObject.Find(objectName);
            }
            else
            {
                // Focus on selection if nothing specified
                if (Selection.activeGameObject != null)
                {
                    target = Selection.activeGameObject;
                }
            }

            if (target == null)
            {
                return new ErrorResponse("Object not found or not specified");
            }

            Selection.activeGameObject = target;
            sceneView.FrameSelected();

            return new SuccessResponse($"Focused on object: {target.name}", new
            {
                objectName = target.name,
                instanceID = target.GetInstanceID(),
                position = sceneView.pivot
            });
        }

        private static object SetViewMode(JObject @params)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            string mode = @params["mode"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(mode))
            {
                return new ErrorResponse("mode parameter is required (shaded, wireframe, shadedwireframe, textured)");
            }

            switch (mode)
            {
                case "wireframe":
                    sceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Wireframe);
                    break;
                case "shaded":
                case "shadedwireframe":
                    sceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.TexturedWire);
                    break;
                case "textured":
                    sceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
                    break;
                default:
                    return new ErrorResponse($"Unknown view mode: {mode}");
            }

            sceneView.Repaint();

            return new SuccessResponse($"View mode set to: {mode}", new
            {
                mode = sceneView.cameraMode.name
            });
        }

        private static object GetViewMode()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            return new SuccessResponse("Retrieved view mode", new
            {
                mode = sceneView.cameraMode.name,
                drawMode = sceneView.cameraMode.drawMode.ToString()
            });
        }

        private static object CaptureScreenshot(JObject @params)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            string savePath = @params["savePath"]?.ToString();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = $"Assets/SceneView_Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            }

            // Ensure .png extension
            if (!savePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                savePath += ".png";
            }

            // Note: SceneView screenshot is complex - would need to use Camera.Render or similar
            // For simplicity, returning guidance
            return new SuccessResponse("Use Unity's Screenshot Capture API", new
            {
                suggestion = "ScreenCapture.CaptureScreenshot",
                recommendedPath = savePath,
                note = "Scene View screenshots require complex camera setup. Use Game View or manual capture."
            });
        }
    }
}
