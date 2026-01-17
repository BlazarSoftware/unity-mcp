using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Runtime.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles camera operations including property configuration, capture, and targeting.
    /// </summary>
    [McpForUnityTool("manage_camera", AutoRegister = false, Description = "Manage Camera components: field of view, clipping planes, and background color.")]
    public static class ManageCamera
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
                        return new SuccessResponse("pong", new { tool = "manage_camera" });

                    case "set_properties":
                        return SetProperties(@params);

                    case "get_properties":
                        return GetProperties(@params);

                    case "look_at":
                        return LookAt(@params);

                    case "set_transform":
                        return SetTransform(@params);

                    case "capture":
                        return Capture(@params);

                    case "set_culling_mask":
                        return SetCullingMask(@params);

                    case "get_all_cameras":
                        return GetAllCameras();

                    case "set_main":
                        return SetMainCamera(@params);

                    case "create":
                        return CreateCamera(@params);

                    case "align_to_view":
                        return AlignToView(@params);

                    case "frame_selected":
                        return FrameSelected(@params);

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: set_properties, get_properties, look_at, set_transform, capture, set_culling_mask, get_all_cameras, set_main, create, align_to_view, frame_selected");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static Camera FindCamera(JObject @params)
        {
            string target = @params["target"]?.ToString();
            bool useMain = @params["useMain"]?.ToObject<bool>() ?? false;

            if (useMain || string.IsNullOrEmpty(target))
            {
                return Camera.main;
            }

            var goInstruction = new JObject { ["find"] = target };
            string searchMethod = @params["searchMethod"]?.ToString();
            if (!string.IsNullOrEmpty(searchMethod))
            {
                goInstruction["method"] = searchMethod;
            }

            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            return go?.GetComponent<Camera>();
        }

        private static Vector3 ParseVector3(JToken token, Vector3 defaultValue = default)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Vector3(
                    arr[0].ToObject<float>(),
                    arr[1].ToObject<float>(),
                    arr[2].ToObject<float>()
                );
            }

            if (token is JObject obj)
            {
                return new Vector3(
                    obj["x"]?.ToObject<float>() ?? defaultValue.x,
                    obj["y"]?.ToObject<float>() ?? defaultValue.y,
                    obj["z"]?.ToObject<float>() ?? defaultValue.z
                );
            }

            return defaultValue;
        }

        private static Color ParseColor(JToken token, Color defaultColor)
        {
            if (token == null) return defaultColor;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Color(
                    arr[0].ToObject<float>(),
                    arr[1].ToObject<float>(),
                    arr[2].ToObject<float>(),
                    arr.Count > 3 ? arr[3].ToObject<float>() : 1f
                );
            }

            if (token is JObject obj)
            {
                return new Color(
                    obj["r"]?.ToObject<float>() ?? defaultColor.r,
                    obj["g"]?.ToObject<float>() ?? defaultColor.g,
                    obj["b"]?.ToObject<float>() ?? defaultColor.b,
                    obj["a"]?.ToObject<float>() ?? 1f
                );
            }

            if (token.Type == JTokenType.String)
            {
                string hex = token.ToString();
                if (ColorUtility.TryParseHtmlString(hex, out Color parsed))
                {
                    return parsed;
                }
            }

            return defaultColor;
        }

        private static object SetProperties(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            Undo.RecordObject(camera, "Set Camera Properties");

            var changes = new List<string>();

            if (@params["fieldOfView"] != null || @params["fov"] != null)
            {
                camera.fieldOfView = (@params["fieldOfView"] ?? @params["fov"]).ToObject<float>();
                changes.Add($"fieldOfView={camera.fieldOfView}");
            }

            if (@params["nearClipPlane"] != null)
            {
                camera.nearClipPlane = @params["nearClipPlane"].ToObject<float>();
                changes.Add($"nearClipPlane={camera.nearClipPlane}");
            }

            if (@params["farClipPlane"] != null)
            {
                camera.farClipPlane = @params["farClipPlane"].ToObject<float>();
                changes.Add($"farClipPlane={camera.farClipPlane}");
            }

            if (@params["orthographic"] != null)
            {
                camera.orthographic = @params["orthographic"].ToObject<bool>();
                changes.Add($"orthographic={camera.orthographic}");
            }

            if (@params["orthographicSize"] != null)
            {
                camera.orthographicSize = @params["orthographicSize"].ToObject<float>();
                changes.Add($"orthographicSize={camera.orthographicSize}");
            }

            if (@params["depth"] != null)
            {
                camera.depth = @params["depth"].ToObject<float>();
                changes.Add($"depth={camera.depth}");
            }

            if (@params["clearFlags"] != null)
            {
                string flags = @params["clearFlags"].ToString().ToLowerInvariant();
                switch (flags)
                {
                    case "skybox":
                        camera.clearFlags = CameraClearFlags.Skybox;
                        break;
                    case "solidcolor":
                    case "color":
                        camera.clearFlags = CameraClearFlags.SolidColor;
                        break;
                    case "depth":
                        camera.clearFlags = CameraClearFlags.Depth;
                        break;
                    case "nothing":
                    case "none":
                        camera.clearFlags = CameraClearFlags.Nothing;
                        break;
                }
                changes.Add($"clearFlags={camera.clearFlags}");
            }

            if (@params["backgroundColor"] != null)
            {
                camera.backgroundColor = ParseColor(@params["backgroundColor"], camera.backgroundColor);
                changes.Add($"backgroundColor set");
            }

            if (@params["rect"] != null)
            {
                JToken rectToken = @params["rect"];
                if (rectToken is JObject rectObj)
                {
                    camera.rect = new Rect(
                        rectObj["x"]?.ToObject<float>() ?? 0f,
                        rectObj["y"]?.ToObject<float>() ?? 0f,
                        rectObj["width"]?.ToObject<float>() ?? 1f,
                        rectObj["height"]?.ToObject<float>() ?? 1f
                    );
                    changes.Add($"rect set");
                }
            }

            if (@params["renderingPath"] != null)
            {
                string path = @params["renderingPath"].ToString().ToLowerInvariant();
                switch (path)
                {
                    case "useplayersettings":
                        camera.renderingPath = RenderingPath.UsePlayerSettings;
                        break;
                    case "forward":
                        camera.renderingPath = RenderingPath.Forward;
                        break;
                    case "deferred":
                        camera.renderingPath = RenderingPath.DeferredShading;
                        break;
                    case "vertexlit":
                        camera.renderingPath = RenderingPath.VertexLit;
                        break;
                }
                changes.Add($"renderingPath={camera.renderingPath}");
            }

            if (@params["allowHDR"] != null)
            {
                camera.allowHDR = @params["allowHDR"].ToObject<bool>();
                changes.Add($"allowHDR={camera.allowHDR}");
            }

            if (@params["allowMSAA"] != null)
            {
                camera.allowMSAA = @params["allowMSAA"].ToObject<bool>();
                changes.Add($"allowMSAA={camera.allowMSAA}");
            }

            if (@params["allowDynamicResolution"] != null)
            {
                camera.allowDynamicResolution = @params["allowDynamicResolution"].ToObject<bool>();
                changes.Add($"allowDynamicResolution={camera.allowDynamicResolution}");
            }

            if (@params["enabled"] != null)
            {
                camera.enabled = @params["enabled"].ToObject<bool>();
                changes.Add($"enabled={camera.enabled}");
            }

            EditorUtility.SetDirty(camera);

            return new SuccessResponse($"Updated camera '{camera.gameObject.name}': {string.Join(", ", changes)}", new
            {
                gameObject = camera.gameObject.name,
                changes = changes
            });
        }

        private static object GetProperties(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            return new SuccessResponse($"Retrieved camera properties for '{camera.gameObject.name}'", new
            {
                gameObject = camera.gameObject.name,
                instanceID = camera.gameObject.GetInstanceID(),
                enabled = camera.enabled,
                isMain = camera == Camera.main,
                fieldOfView = camera.fieldOfView,
                nearClipPlane = camera.nearClipPlane,
                farClipPlane = camera.farClipPlane,
                orthographic = camera.orthographic,
                orthographicSize = camera.orthographicSize,
                depth = camera.depth,
                clearFlags = camera.clearFlags.ToString(),
                backgroundColor = new { r = camera.backgroundColor.r, g = camera.backgroundColor.g, b = camera.backgroundColor.b, a = camera.backgroundColor.a },
                cullingMask = camera.cullingMask,
                rect = new { x = camera.rect.x, y = camera.rect.y, width = camera.rect.width, height = camera.rect.height },
                pixelRect = new { x = camera.pixelRect.x, y = camera.pixelRect.y, width = camera.pixelRect.width, height = camera.pixelRect.height },
                renderingPath = camera.renderingPath.ToString(),
                actualRenderingPath = camera.actualRenderingPath.ToString(),
                allowHDR = camera.allowHDR,
                allowMSAA = camera.allowMSAA,
                allowDynamicResolution = camera.allowDynamicResolution,
                targetTexture = camera.targetTexture?.name,
                position = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
                rotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z },
                forward = new { x = camera.transform.forward.x, y = camera.transform.forward.y, z = camera.transform.forward.z }
            });
        }

        private static object LookAt(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            Vector3 targetPosition = default;
            bool hasTarget = false;

            // Check for target position
            if (@params["position"] != null)
            {
                targetPosition = ParseVector3(@params["position"]);
                hasTarget = true;
            }
            // Check for target GameObject
            else if (@params["lookTarget"] != null)
            {
                string lookTargetStr = @params["lookTarget"].ToString();
                var goInstruction = new JObject { ["find"] = lookTargetStr };
                GameObject targetGo = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;

                if (targetGo != null)
                {
                    targetPosition = targetGo.transform.position;
                    hasTarget = true;
                }
            }

            if (!hasTarget)
            {
                return new ErrorResponse("position or lookTarget is required");
            }

            Undo.RecordObject(camera.transform, "Camera Look At");

            Vector3 worldUp = ParseVector3(@params["worldUp"], Vector3.up);
            camera.transform.LookAt(targetPosition, worldUp);

            EditorUtility.SetDirty(camera.transform);

            return new SuccessResponse($"Camera '{camera.gameObject.name}' now looking at target", new
            {
                gameObject = camera.gameObject.name,
                targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
                newRotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z }
            });
        }

        private static object SetTransform(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            Undo.RecordObject(camera.transform, "Set Camera Transform");

            var changes = new List<string>();

            if (@params["position"] != null)
            {
                camera.transform.position = ParseVector3(@params["position"]);
                changes.Add($"position set");
            }

            if (@params["rotation"] != null)
            {
                camera.transform.eulerAngles = ParseVector3(@params["rotation"]);
                changes.Add($"rotation set");
            }

            if (@params["localPosition"] != null)
            {
                camera.transform.localPosition = ParseVector3(@params["localPosition"]);
                changes.Add($"localPosition set");
            }

            if (@params["localRotation"] != null)
            {
                camera.transform.localEulerAngles = ParseVector3(@params["localRotation"]);
                changes.Add($"localRotation set");
            }

            EditorUtility.SetDirty(camera.transform);

            return new SuccessResponse($"Updated camera transform: {string.Join(", ", changes)}", new
            {
                gameObject = camera.gameObject.name,
                changes = changes,
                position = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
                rotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z }
            });
        }

        private static object Capture(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            string fileName = @params["fileName"]?.ToString();
            int superSize = @params["superSize"]?.ToObject<int>() ?? 1;
            int width = @params["width"]?.ToObject<int>() ?? 1920;
            int height = @params["height"]?.ToObject<int>() ?? 1080;

            // Use ScreenshotUtility if available, otherwise manual capture
            try
            {
                ScreenshotCaptureResult result = ScreenshotUtility.CaptureFromCameraToAssetsFolder(camera, fileName, superSize, ensureUniqueFileName: true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                return new SuccessResponse($"Captured screenshot from camera '{camera.gameObject.name}'", new
                {
                    camera = camera.gameObject.name,
                    path = result.AssetsRelativePath,
                    fullPath = result.FullPath,
                    superSize = result.SuperSize
                });
            }
            catch (Exception ex)
            {
                // Fallback to manual capture
                RenderTexture rt = new RenderTexture(width * superSize, height * superSize, 24);
                camera.targetTexture = rt;
                Texture2D screenShot = new Texture2D(width * superSize, height * superSize, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, width * superSize, height * superSize), 0, 0);
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = screenShot.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(screenShot);

                string screenshotsDir = Path.Combine(Application.dataPath, "Screenshots");
                if (!Directory.Exists(screenshotsDir))
                {
                    Directory.CreateDirectory(screenshotsDir);
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                }
                if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".png";
                }

                string fullPath = Path.Combine(screenshotsDir, fileName);
                File.WriteAllBytes(fullPath, bytes);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                string relativePath = "Assets/Screenshots/" + fileName;

                return new SuccessResponse($"Captured screenshot from camera '{camera.gameObject.name}'", new
                {
                    camera = camera.gameObject.name,
                    path = relativePath,
                    fullPath = fullPath,
                    width = width * superSize,
                    height = height * superSize,
                    note = $"Fallback capture used: {ex.Message}"
                });
            }
        }

        private static object SetCullingMask(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            Undo.RecordObject(camera, "Set Camera Culling Mask");

            if (@params["cullingMask"] != null)
            {
                camera.cullingMask = @params["cullingMask"].ToObject<int>();
            }

            // Handle layer names
            if (@params["layers"] != null && @params["layers"] is JArray layersArray)
            {
                int mask = 0;
                foreach (var layer in layersArray)
                {
                    string layerName = layer.ToString();
                    int layerIndex = LayerMask.NameToLayer(layerName);
                    if (layerIndex >= 0)
                    {
                        mask |= (1 << layerIndex);
                    }
                }
                camera.cullingMask = mask;
            }

            // Handle include/exclude operations
            if (@params["include"] != null && @params["include"] is JArray includeArray)
            {
                foreach (var layer in includeArray)
                {
                    string layerName = layer.ToString();
                    int layerIndex = LayerMask.NameToLayer(layerName);
                    if (layerIndex >= 0)
                    {
                        camera.cullingMask |= (1 << layerIndex);
                    }
                }
            }

            if (@params["exclude"] != null && @params["exclude"] is JArray excludeArray)
            {
                foreach (var layer in excludeArray)
                {
                    string layerName = layer.ToString();
                    int layerIndex = LayerMask.NameToLayer(layerName);
                    if (layerIndex >= 0)
                    {
                        camera.cullingMask &= ~(1 << layerIndex);
                    }
                }
            }

            // Handle "everything" and "nothing"
            if (@params["everything"]?.ToObject<bool>() == true)
            {
                camera.cullingMask = -1; // All layers
            }
            else if (@params["nothing"]?.ToObject<bool>() == true)
            {
                camera.cullingMask = 0; // No layers
            }

            EditorUtility.SetDirty(camera);

            // Get readable layer names
            var layerNames = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((camera.cullingMask & (1 << i)) != 0)
                {
                    string name = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(name))
                    {
                        layerNames.Add(name);
                    }
                }
            }

            return new SuccessResponse($"Set culling mask for camera '{camera.gameObject.name}'", new
            {
                gameObject = camera.gameObject.name,
                cullingMask = camera.cullingMask,
                layers = layerNames
            });
        }

        private static object GetAllCameras()
        {
            Camera[] cameras = Camera.allCameras;

            var results = cameras.Select(c => new
            {
                gameObject = c.gameObject.name,
                instanceID = c.gameObject.GetInstanceID(),
                isMain = c == Camera.main,
                enabled = c.enabled,
                depth = c.depth,
                fieldOfView = c.fieldOfView,
                orthographic = c.orthographic,
                position = new { x = c.transform.position.x, y = c.transform.position.y, z = c.transform.position.z }
            }).ToList();

            return new SuccessResponse($"Found {cameras.Length} cameras", new
            {
                count = cameras.Length,
                mainCamera = Camera.main?.gameObject.name,
                cameras = results
            });
        }

        private static object SetMainCamera(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            Camera camera = go.GetComponent<Camera>();
            if (camera == null)
            {
                return new ErrorResponse($"No Camera component on '{go.name}'");
            }

            // Set as main camera by changing the tag
            Undo.RecordObject(go, "Set Main Camera");
            go.tag = "MainCamera";
            EditorUtility.SetDirty(go);

            return new SuccessResponse($"Set '{go.name}' as the main camera", new
            {
                gameObject = go.name,
                tag = go.tag
            });
        }

        private static object CreateCamera(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "New Camera";

            GameObject camGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(camGo, "Create Camera");

            Camera camera = camGo.AddComponent<Camera>();

            // Set position if provided
            if (@params["position"] != null)
            {
                camGo.transform.position = ParseVector3(@params["position"]);
            }

            // Set rotation if provided
            if (@params["rotation"] != null)
            {
                camGo.transform.eulerAngles = ParseVector3(@params["rotation"]);
            }

            // Set FOV if provided
            if (@params["fieldOfView"] != null || @params["fov"] != null)
            {
                camera.fieldOfView = (@params["fieldOfView"] ?? @params["fov"]).ToObject<float>();
            }

            // Set orthographic mode
            if (@params["orthographic"] != null)
            {
                camera.orthographic = @params["orthographic"].ToObject<bool>();
            }

            // Set as main if requested
            if (@params["setAsMain"]?.ToObject<bool>() == true)
            {
                camGo.tag = "MainCamera";
            }

            EditorUtility.SetDirty(camera);

            return new SuccessResponse($"Created camera '{name}'", new
            {
                gameObject = camGo.name,
                instanceID = camGo.GetInstanceID(),
                position = new { x = camGo.transform.position.x, y = camGo.transform.position.y, z = camGo.transform.position.z },
                fieldOfView = camera.fieldOfView,
                isMain = camGo.CompareTag("MainCamera")
            });
        }

        private static object AlignToView(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return new ErrorResponse("No active Scene View found");
            }

            Undo.RecordObject(camera.transform, "Align Camera to View");

            camera.transform.position = sceneView.camera.transform.position;
            camera.transform.rotation = sceneView.camera.transform.rotation;

            EditorUtility.SetDirty(camera.transform);

            return new SuccessResponse($"Aligned camera '{camera.gameObject.name}' to Scene View", new
            {
                gameObject = camera.gameObject.name,
                position = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
                rotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z }
            });
        }

        private static object FrameSelected(JObject @params)
        {
            Camera camera = FindCamera(@params);
            if (camera == null)
            {
                return new ErrorResponse("Camera not found");
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                // Try to frame a specific target
                string frameTarget = @params["frameTarget"]?.ToString();
                if (!string.IsNullOrEmpty(frameTarget))
                {
                    var goInstruction = new JObject { ["find"] = frameTarget };
                    GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
                    if (go != null)
                    {
                        selectedObjects = new[] { go };
                    }
                }
            }

            if (selectedObjects.Length == 0)
            {
                return new ErrorResponse("No objects selected or specified to frame");
            }

            // Calculate bounds of all selected objects
            Bounds bounds = new Bounds();
            bool boundsInitialized = false;

            foreach (var obj in selectedObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!boundsInitialized)
                    {
                        bounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
                else
                {
                    // Use transform position for objects without renderers
                    if (!boundsInitialized)
                    {
                        bounds = new Bounds(obj.transform.position, Vector3.one);
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(obj.transform.position);
                    }
                }
            }

            // Position camera to frame bounds
            float cameraDistance = bounds.size.magnitude / (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            float padding = @params["padding"]?.ToObject<float>() ?? 1.5f;

            Undo.RecordObject(camera.transform, "Frame Camera");

            Vector3 direction = camera.transform.forward;
            camera.transform.position = bounds.center - direction * cameraDistance * padding;
            camera.transform.LookAt(bounds.center);

            EditorUtility.SetDirty(camera.transform);

            return new SuccessResponse($"Framed {selectedObjects.Length} object(s) in camera '{camera.gameObject.name}'", new
            {
                gameObject = camera.gameObject.name,
                framedObjects = selectedObjects.Select(o => o.name).ToList(),
                boundsCenter = new { x = bounds.center.x, y = bounds.center.y, z = bounds.center.z },
                boundsSize = new { x = bounds.size.x, y = bounds.size.y, z = bounds.size.z },
                newPosition = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z }
            });
        }
    }
}
