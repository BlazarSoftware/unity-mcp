using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles lighting system operations including light configuration, lightmap baking, probes, and ambient settings.
    /// </summary>
    [McpForUnityTool("manage_lighting", AutoRegister = false, Description = "Manage lighting settings, Light components, and reflection probes.")]
    public static class ManageLighting
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
                        return new SuccessResponse("pong", new { tool = "manage_lighting" });

                    case "set_light":
                        return SetLight(@params);

                    case "get_light_info":
                        return GetLightInfo(@params);

                    case "create_light":
                        return CreateLight(@params);

                    case "bake_lightmaps":
                        return BakeLightmaps(@params);

                    case "get_bake_status":
                        return GetBakeStatus();

                    case "cancel_bake":
                        return CancelBake();

                    case "configure_probes":
                        return ConfigureProbes(@params);

                    case "set_ambient":
                        return SetAmbient(@params);

                    case "get_ambient":
                        return GetAmbient();

                    case "set_fog":
                        return SetFog(@params);

                    case "get_fog":
                        return GetFog();

                    case "get_all_lights":
                        return GetAllLights();

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: set_light, get_light_info, create_light, bake_lightmaps, get_bake_status, cancel_bake, configure_probes, set_ambient, get_ambient, set_fog, get_fog, get_all_lights");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static Light FindLight(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            var goInstruction = new JObject { ["find"] = target };
            string searchMethod = @params["searchMethod"]?.ToString();
            if (!string.IsNullOrEmpty(searchMethod))
            {
                goInstruction["method"] = searchMethod;
            }

            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            return go?.GetComponent<Light>();
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

            // Try parsing as hex color
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

        private static object SetLight(JObject @params)
        {
            Light light = FindLight(@params);
            if (light == null)
            {
                return new ErrorResponse("Light not found on target GameObject");
            }

            Undo.RecordObject(light, "Set Light Properties");

            var changes = new List<string>();

            if (@params["color"] != null)
            {
                light.color = ParseColor(@params["color"], light.color);
                changes.Add($"color set");
            }

            if (@params["intensity"] != null)
            {
                light.intensity = @params["intensity"].ToObject<float>();
                changes.Add($"intensity={light.intensity}");
            }

            if (@params["range"] != null)
            {
                light.range = @params["range"].ToObject<float>();
                changes.Add($"range={light.range}");
            }

            if (@params["spotAngle"] != null)
            {
                light.spotAngle = @params["spotAngle"].ToObject<float>();
                changes.Add($"spotAngle={light.spotAngle}");
            }

            if (@params["innerSpotAngle"] != null)
            {
                light.innerSpotAngle = @params["innerSpotAngle"].ToObject<float>();
                changes.Add($"innerSpotAngle={light.innerSpotAngle}");
            }

            if (@params["shadows"] != null)
            {
                string shadowMode = @params["shadows"].ToString().ToLowerInvariant();
                switch (shadowMode)
                {
                    case "none":
                        light.shadows = LightShadows.None;
                        break;
                    case "hard":
                        light.shadows = LightShadows.Hard;
                        break;
                    case "soft":
                        light.shadows = LightShadows.Soft;
                        break;
                }
                changes.Add($"shadows={light.shadows}");
            }

            if (@params["shadowStrength"] != null)
            {
                light.shadowStrength = Mathf.Clamp01(@params["shadowStrength"].ToObject<float>());
                changes.Add($"shadowStrength={light.shadowStrength}");
            }

            if (@params["shadowBias"] != null)
            {
                light.shadowBias = @params["shadowBias"].ToObject<float>();
                changes.Add($"shadowBias={light.shadowBias}");
            }

            if (@params["shadowNormalBias"] != null)
            {
                light.shadowNormalBias = @params["shadowNormalBias"].ToObject<float>();
                changes.Add($"shadowNormalBias={light.shadowNormalBias}");
            }

            if (@params["shadowNearPlane"] != null)
            {
                light.shadowNearPlane = @params["shadowNearPlane"].ToObject<float>();
                changes.Add($"shadowNearPlane={light.shadowNearPlane}");
            }

            if (@params["cookieSize"] != null)
            {
                light.cookieSize = @params["cookieSize"].ToObject<float>();
                changes.Add($"cookieSize={light.cookieSize}");
            }

            if (@params["renderMode"] != null)
            {
                string rm = @params["renderMode"].ToString().ToLowerInvariant();
                switch (rm)
                {
                    case "auto":
                        light.renderMode = LightRenderMode.Auto;
                        break;
                    case "forcevertex":
                        light.renderMode = LightRenderMode.ForceVertex;
                        break;
                    case "forcepixel":
                        light.renderMode = LightRenderMode.ForcePixel;
                        break;
                }
                changes.Add($"renderMode={light.renderMode}");
            }

            if (@params["cullingMask"] != null)
            {
                light.cullingMask = @params["cullingMask"].ToObject<int>();
                changes.Add($"cullingMask={light.cullingMask}");
            }

            if (@params["enabled"] != null)
            {
                light.enabled = @params["enabled"].ToObject<bool>();
                changes.Add($"enabled={light.enabled}");
            }

            if (@params["bounceIntensity"] != null)
            {
                light.bounceIntensity = @params["bounceIntensity"].ToObject<float>();
                changes.Add($"bounceIntensity={light.bounceIntensity}");
            }

            EditorUtility.SetDirty(light);

            return new SuccessResponse($"Updated light '{light.gameObject.name}': {string.Join(", ", changes)}", new
            {
                gameObject = light.gameObject.name,
                changes = changes
            });
        }

        private static object GetLightInfo(JObject @params)
        {
            Light light = FindLight(@params);
            if (light == null)
            {
                return new ErrorResponse("Light not found on target GameObject");
            }

            return new SuccessResponse($"Retrieved light info for '{light.gameObject.name}'", new
            {
                gameObject = light.gameObject.name,
                type = light.type.ToString(),
                color = new { r = light.color.r, g = light.color.g, b = light.color.b, a = light.color.a },
                colorTemperature = light.colorTemperature,
                intensity = light.intensity,
                range = light.range,
                spotAngle = light.spotAngle,
                innerSpotAngle = light.innerSpotAngle,
                shadows = light.shadows.ToString(),
                shadowStrength = light.shadowStrength,
                shadowBias = light.shadowBias,
                shadowNormalBias = light.shadowNormalBias,
                shadowNearPlane = light.shadowNearPlane,
                renderMode = light.renderMode.ToString(),
                cullingMask = light.cullingMask,
                enabled = light.enabled,
                bounceIntensity = light.bounceIntensity,
                cookie = light.cookie?.name,
                cookieSize = light.cookieSize
            });
        }

        private static object CreateLight(JObject @params)
        {
            string lightTypeStr = @params["lightType"]?.ToString()?.ToLowerInvariant() ?? "point";
            string name = @params["name"]?.ToString() ?? $"New {lightTypeStr} Light";

            LightType lightType = LightType.Point;
            switch (lightTypeStr)
            {
                case "directional":
                    lightType = LightType.Directional;
                    break;
                case "point":
                    lightType = LightType.Point;
                    break;
                case "spot":
                    lightType = LightType.Spot;
                    break;
                case "area":
                case "rectangle":
                    lightType = LightType.Rectangle;
                    break;
                case "disc":
                    lightType = LightType.Disc;
                    break;
            }

            GameObject lightGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(lightGo, "Create Light");

            Light light = lightGo.AddComponent<Light>();
            light.type = lightType;

            // Set position if provided
            if (@params["position"] != null)
            {
                lightGo.transform.position = ParseVector3(@params["position"]);
            }

            // Set rotation if provided
            if (@params["rotation"] != null)
            {
                lightGo.transform.eulerAngles = ParseVector3(@params["rotation"]);
            }

            // Set color if provided
            if (@params["color"] != null)
            {
                light.color = ParseColor(@params["color"], Color.white);
            }

            // Set intensity if provided
            if (@params["intensity"] != null)
            {
                light.intensity = @params["intensity"].ToObject<float>();
            }

            // Set range if provided (for point/spot)
            if (@params["range"] != null)
            {
                light.range = @params["range"].ToObject<float>();
            }

            // Set spot angle if provided
            if (@params["spotAngle"] != null && lightType == LightType.Spot)
            {
                light.spotAngle = @params["spotAngle"].ToObject<float>();
            }

            // Set shadows
            if (@params["shadows"] != null)
            {
                string shadowMode = @params["shadows"].ToString().ToLowerInvariant();
                switch (shadowMode)
                {
                    case "none":
                        light.shadows = LightShadows.None;
                        break;
                    case "hard":
                        light.shadows = LightShadows.Hard;
                        break;
                    case "soft":
                        light.shadows = LightShadows.Soft;
                        break;
                }
            }

            EditorUtility.SetDirty(light);

            return new SuccessResponse($"Created {lightType} light '{name}'", new
            {
                gameObject = lightGo.name,
                instanceID = lightGo.GetInstanceID(),
                lightType = lightType.ToString(),
                color = new { r = light.color.r, g = light.color.g, b = light.color.b },
                intensity = light.intensity,
                range = light.range
            });
        }

        private static object BakeLightmaps(JObject @params)
        {
            if (Lightmapping.isRunning)
            {
                return new ErrorResponse("Lightmap baking is already in progress");
            }

            // Apply settings if provided
            if (@params["bounces"] != null)
            {
                Lightmapping.bounceBoost = @params["bounces"].ToObject<float>();
            }

            bool async = @params["async"]?.ToObject<bool>() ?? true;

            if (async)
            {
                Lightmapping.BakeAsync();
                return new SuccessResponse("Started asynchronous lightmap baking", new
                {
                    async = true,
                    status = "baking"
                });
            }
            else
            {
                Lightmapping.Bake();
                return new SuccessResponse("Completed synchronous lightmap baking", new
                {
                    async = false,
                    status = "completed"
                });
            }
        }

        private static object GetBakeStatus()
        {
            return new SuccessResponse("Retrieved bake status", new
            {
                isRunning = Lightmapping.isRunning,
                buildProgress = Lightmapping.buildProgress
            });
        }

        private static object CancelBake()
        {
            if (!Lightmapping.isRunning)
            {
                return new SuccessResponse("No bake in progress to cancel");
            }

            Lightmapping.Cancel();
            return new SuccessResponse("Cancelled lightmap baking");
        }

        private static object ConfigureProbes(JObject @params)
        {
            string probeType = @params["probeType"]?.ToString()?.ToLowerInvariant() ?? "light";

            if (probeType == "light")
            {
                string target = @params["target"]?.ToString();
                if (string.IsNullOrEmpty(target))
                {
                    return new ErrorResponse("target is required for light probe configuration");
                }

                var goInstruction = new JObject { ["find"] = target };
                GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
                if (go == null)
                {
                    return new ErrorResponse($"GameObject not found: {target}");
                }

                LightProbeGroup probeGroup = go.GetComponent<LightProbeGroup>();
                if (probeGroup == null)
                {
                    if (@params["createIfMissing"]?.ToObject<bool>() ?? true)
                    {
                        Undo.RecordObject(go, "Add LightProbeGroup");
                        probeGroup = Undo.AddComponent<LightProbeGroup>(go);
                    }
                    else
                    {
                        return new ErrorResponse($"LightProbeGroup not found on '{go.name}'");
                    }
                }

                // Configure probe positions if provided
                if (@params["positions"] != null && @params["positions"] is JArray posArray)
                {
                    var positions = new List<Vector3>();
                    foreach (var pos in posArray)
                    {
                        positions.Add(ParseVector3(pos));
                    }
                    probeGroup.probePositions = positions.ToArray();
                }

                EditorUtility.SetDirty(probeGroup);

                return new SuccessResponse($"Configured light probe group on '{go.name}'", new
                {
                    gameObject = go.name,
                    probeCount = probeGroup.probePositions.Length
                });
            }
            else if (probeType == "reflection")
            {
                string target = @params["target"]?.ToString();
                if (string.IsNullOrEmpty(target))
                {
                    return new ErrorResponse("target is required for reflection probe configuration");
                }

                var goInstruction = new JObject { ["find"] = target };
                GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
                if (go == null)
                {
                    return new ErrorResponse($"GameObject not found: {target}");
                }

                ReflectionProbe probe = go.GetComponent<ReflectionProbe>();
                if (probe == null)
                {
                    if (@params["createIfMissing"]?.ToObject<bool>() ?? true)
                    {
                        Undo.RecordObject(go, "Add ReflectionProbe");
                        probe = Undo.AddComponent<ReflectionProbe>(go);
                    }
                    else
                    {
                        return new ErrorResponse($"ReflectionProbe not found on '{go.name}'");
                    }
                }

                Undo.RecordObject(probe, "Configure ReflectionProbe");

                if (@params["size"] != null)
                {
                    probe.size = ParseVector3(@params["size"], Vector3.one * 10f);
                }

                if (@params["resolution"] != null)
                {
                    probe.resolution = @params["resolution"].ToObject<int>();
                }

                if (@params["intensity"] != null)
                {
                    probe.intensity = @params["intensity"].ToObject<float>();
                }

                if (@params["boxProjection"] != null)
                {
                    probe.boxProjection = @params["boxProjection"].ToObject<bool>();
                }

                if (@params["mode"] != null)
                {
                    string mode = @params["mode"].ToString().ToLowerInvariant();
                    switch (mode)
                    {
                        case "baked":
                            probe.mode = ReflectionProbeMode.Baked;
                            break;
                        case "realtime":
                            probe.mode = ReflectionProbeMode.Realtime;
                            break;
                        case "custom":
                            probe.mode = ReflectionProbeMode.Custom;
                            break;
                    }
                }

                EditorUtility.SetDirty(probe);

                return new SuccessResponse($"Configured reflection probe on '{go.name}'", new
                {
                    gameObject = go.name,
                    size = new { x = probe.size.x, y = probe.size.y, z = probe.size.z },
                    resolution = probe.resolution,
                    intensity = probe.intensity,
                    mode = probe.mode.ToString()
                });
            }

            return new ErrorResponse($"Unknown probe type: {probeType}. Valid types: light, reflection");
        }

        private static object SetAmbient(JObject @params)
        {
            var changes = new List<string>();

            if (@params["mode"] != null)
            {
                string mode = @params["mode"].ToString().ToLowerInvariant();
                switch (mode)
                {
                    case "skybox":
                        RenderSettings.ambientMode = AmbientMode.Skybox;
                        break;
                    case "trilight":
                        RenderSettings.ambientMode = AmbientMode.Trilight;
                        break;
                    case "flat":
                    case "color":
                        RenderSettings.ambientMode = AmbientMode.Flat;
                        break;
                }
                changes.Add($"mode={RenderSettings.ambientMode}");
            }

            if (@params["color"] != null)
            {
                RenderSettings.ambientLight = ParseColor(@params["color"], RenderSettings.ambientLight);
                changes.Add($"ambientLight set");
            }

            if (@params["skyColor"] != null)
            {
                RenderSettings.ambientSkyColor = ParseColor(@params["skyColor"], RenderSettings.ambientSkyColor);
                changes.Add($"skyColor set");
            }

            if (@params["equatorColor"] != null)
            {
                RenderSettings.ambientEquatorColor = ParseColor(@params["equatorColor"], RenderSettings.ambientEquatorColor);
                changes.Add($"equatorColor set");
            }

            if (@params["groundColor"] != null)
            {
                RenderSettings.ambientGroundColor = ParseColor(@params["groundColor"], RenderSettings.ambientGroundColor);
                changes.Add($"groundColor set");
            }

            if (@params["intensity"] != null)
            {
                RenderSettings.ambientIntensity = @params["intensity"].ToObject<float>();
                changes.Add($"intensity={RenderSettings.ambientIntensity}");
            }

            if (@params["skybox"] != null)
            {
                string skyboxPath = @params["skybox"].ToString();
                Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                if (skybox != null)
                {
                    RenderSettings.skybox = skybox;
                    changes.Add($"skybox={skybox.name}");
                }
            }

            if (@params["reflectionIntensity"] != null)
            {
                RenderSettings.reflectionIntensity = @params["reflectionIntensity"].ToObject<float>();
                changes.Add($"reflectionIntensity={RenderSettings.reflectionIntensity}");
            }

            if (@params["reflectionBounces"] != null)
            {
                RenderSettings.reflectionBounces = @params["reflectionBounces"].ToObject<int>();
                changes.Add($"reflectionBounces={RenderSettings.reflectionBounces}");
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No ambient settings changed");
            }

            return new SuccessResponse($"Updated ambient settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }

        private static object GetAmbient()
        {
            return new SuccessResponse("Retrieved ambient settings", new
            {
                mode = RenderSettings.ambientMode.ToString(),
                ambientLight = new { r = RenderSettings.ambientLight.r, g = RenderSettings.ambientLight.g, b = RenderSettings.ambientLight.b },
                skyColor = new { r = RenderSettings.ambientSkyColor.r, g = RenderSettings.ambientSkyColor.g, b = RenderSettings.ambientSkyColor.b },
                equatorColor = new { r = RenderSettings.ambientEquatorColor.r, g = RenderSettings.ambientEquatorColor.g, b = RenderSettings.ambientEquatorColor.b },
                groundColor = new { r = RenderSettings.ambientGroundColor.r, g = RenderSettings.ambientGroundColor.g, b = RenderSettings.ambientGroundColor.b },
                intensity = RenderSettings.ambientIntensity,
                skybox = RenderSettings.skybox?.name,
                reflectionIntensity = RenderSettings.reflectionIntensity,
                reflectionBounces = RenderSettings.reflectionBounces
            });
        }

        private static object SetFog(JObject @params)
        {
            var changes = new List<string>();

            if (@params["enabled"] != null)
            {
                RenderSettings.fog = @params["enabled"].ToObject<bool>();
                changes.Add($"enabled={RenderSettings.fog}");
            }

            if (@params["color"] != null)
            {
                RenderSettings.fogColor = ParseColor(@params["color"], RenderSettings.fogColor);
                changes.Add($"color set");
            }

            if (@params["mode"] != null)
            {
                string mode = @params["mode"].ToString().ToLowerInvariant();
                switch (mode)
                {
                    case "linear":
                        RenderSettings.fogMode = FogMode.Linear;
                        break;
                    case "exponential":
                        RenderSettings.fogMode = FogMode.Exponential;
                        break;
                    case "exponentialsquared":
                        RenderSettings.fogMode = FogMode.ExponentialSquared;
                        break;
                }
                changes.Add($"mode={RenderSettings.fogMode}");
            }

            if (@params["density"] != null)
            {
                RenderSettings.fogDensity = @params["density"].ToObject<float>();
                changes.Add($"density={RenderSettings.fogDensity}");
            }

            if (@params["startDistance"] != null)
            {
                RenderSettings.fogStartDistance = @params["startDistance"].ToObject<float>();
                changes.Add($"startDistance={RenderSettings.fogStartDistance}");
            }

            if (@params["endDistance"] != null)
            {
                RenderSettings.fogEndDistance = @params["endDistance"].ToObject<float>();
                changes.Add($"endDistance={RenderSettings.fogEndDistance}");
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No fog settings changed");
            }

            return new SuccessResponse($"Updated fog settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }

        private static object GetFog()
        {
            return new SuccessResponse("Retrieved fog settings", new
            {
                enabled = RenderSettings.fog,
                color = new { r = RenderSettings.fogColor.r, g = RenderSettings.fogColor.g, b = RenderSettings.fogColor.b },
                mode = RenderSettings.fogMode.ToString(),
                density = RenderSettings.fogDensity,
                startDistance = RenderSettings.fogStartDistance,
                endDistance = RenderSettings.fogEndDistance
            });
        }

        private static object GetAllLights()
        {
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            var results = lights.Select(l => new
            {
                gameObject = l.gameObject.name,
                instanceID = l.gameObject.GetInstanceID(),
                type = l.type.ToString(),
                color = new { r = l.color.r, g = l.color.g, b = l.color.b },
                intensity = l.intensity,
                range = l.range,
                shadows = l.shadows.ToString(),
                enabled = l.enabled
            }).ToList();

            return new SuccessResponse($"Found {lights.Length} lights in scene", new
            {
                count = lights.Length,
                lights = results
            });
        }
    }
}
