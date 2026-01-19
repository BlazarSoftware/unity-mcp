using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages level organization including hierarchy folders, layers, tags, and batch operations.
    /// </summary>
    [McpForUnityTool("manage_level_organization", AutoRegister = false,
        Description = "Organize level hierarchy and perform batch operations. Actions: create_folder (create empty GameObject as folder), organize_by_type (sort objects into folders by component), setup_layers (create physics layers), batch_set_layer (set layer for objects matching pattern), batch_set_tag (set tag for objects matching pattern), batch_set_static (mark objects as static), get_statistics (count objects/colliders/triangles), validate_level (check for common issues).")]
    public static class ManageLevelOrganization
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: create_folder, organize_by_type, setup_layers, batch_set_layer, batch_set_tag, batch_set_static, get_statistics, validate_level, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_level_organization" }),
                    "create_folder" => CreateFolder(@params),
                    "organize_by_type" => OrganizeByType(@params),
                    "setup_layers" => SetupLayers(@params),
                    "batch_set_layer" => BatchSetLayer(@params),
                    "batch_set_tag" => BatchSetTag(@params),
                    "batch_set_static" => BatchSetStatic(@params),
                    "get_statistics" => GetStatistics(@params),
                    "validate_level" => ValidateLevel(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: create_folder, organize_by_type, setup_layers, batch_set_layer, batch_set_tag, batch_set_static, get_statistics, validate_level, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Creates an empty GameObject as a folder/organizer.
        /// </summary>
        private static object CreateFolder(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "Folder";
            string parentName = @params["parent"]?.ToString();
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            bool resetTransform = @params["resetTransform"]?.ToObject<bool>() ?? true;

            GameObject folder = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(folder, $"Create Folder '{name}'");

            if (!string.IsNullOrEmpty(parentName))
            {
                GameObject parent = GameObject.Find(parentName);
                if (parent != null)
                {
                    folder.transform.SetParent(parent.transform, false);
                }
            }

            if (resetTransform)
            {
                folder.transform.localPosition = Vector3.zero;
                folder.transform.localRotation = Quaternion.identity;
                folder.transform.localScale = Vector3.one;
            }
            else
            {
                folder.transform.position = position;
            }

            Selection.activeGameObject = folder;

            return new SuccessResponse($"Created folder '{name}'",
                new
                {
                    name = name,
                    instanceId = folder.GetInstanceID(),
                    parent = parentName
                });
        }

        /// <summary>
        /// Organizes scene objects into folders by component type.
        /// </summary>
        private static object OrganizeByType(JObject @params)
        {
            JObject categories = @params["categories"] as JObject;
            bool createFolders = @params["createFolders"]?.ToObject<bool>() ?? true;
            bool moveRootOnly = @params["moveRootOnly"]?.ToObject<bool>() ?? true;

            // Default categories if none provided
            if (categories == null)
            {
                categories = new JObject
                {
                    ["_Geometry"] = new JArray { "MeshRenderer", "MeshFilter" },
                    ["_Lights"] = new JArray { "Light" },
                    ["_Cameras"] = new JArray { "Camera" },
                    ["_Audio"] = new JArray { "AudioSource" },
                    ["_UI"] = new JArray { "Canvas", "RectTransform" },
                    ["_Triggers"] = new JArray { "TriggerVolume", "BoxCollider" },
                    ["_Markers"] = new JArray { "SpawnPoint", "Waypoint", "Checkpoint", "ObjectiveMarker" }
                };
            }

            var results = new Dictionary<string, int>();
            var folders = new Dictionary<string, GameObject>();

            // Create or find folders
            if (createFolders)
            {
                foreach (var category in categories)
                {
                    string folderName = category.Key;
                    GameObject folder = GameObject.Find(folderName);
                    if (folder == null)
                    {
                        folder = new GameObject(folderName);
                        Undo.RegisterCreatedObjectUndo(folder, $"Create Organization Folder '{folderName}'");
                    }
                    folders[folderName] = folder;
                    results[folderName] = 0;
                }
            }

            // Find all root-level game objects (or all if moveRootOnly is false)
            var allObjects = moveRootOnly
                ? UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.parent == null)
                    .Select(t => t.gameObject)
                    .ToArray()
                : UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            // Categorize objects
            foreach (var obj in allObjects)
            {
                // Skip folders themselves
                if (folders.ContainsValue(obj))
                    continue;

                foreach (var category in categories)
                {
                    string folderName = category.Key;
                    JArray componentTypes = category.Value as JArray;

                    if (componentTypes == null)
                        continue;

                    bool hasComponent = false;
                    foreach (var compType in componentTypes)
                    {
                        string typeName = compType.ToString();
                        Component comp = obj.GetComponent(typeName);
                        if (comp != null)
                        {
                            hasComponent = true;
                            break;
                        }
                    }

                    if (hasComponent && folders.TryGetValue(folderName, out GameObject folder))
                    {
                        Undo.SetTransformParent(obj.transform, folder.transform, $"Organize '{obj.name}'");
                        results[folderName]++;
                        break; // Only move to first matching category
                    }
                }
            }

            int totalMoved = results.Values.Sum();

            return new SuccessResponse($"Organized {totalMoved} objects into {folders.Count} folders",
                new
                {
                    totalMoved = totalMoved,
                    folderCount = folders.Count,
                    results = results
                });
        }

        /// <summary>
        /// Creates and configures physics layers.
        /// </summary>
        private static object SetupLayers(JObject @params)
        {
            JArray layers = @params["layers"] as JArray;

            if (layers == null || layers.Count == 0)
            {
                return new ErrorResponse("No layers specified. Provide 'layers' as array of layer names.");
            }

            var created = new List<string>();
            var existing = new List<string>();
            var failed = new List<string>();

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            foreach (var layerToken in layers)
            {
                string layerName = layerToken.ToString();
                int existingIndex = LayerMask.NameToLayer(layerName);

                if (existingIndex != -1)
                {
                    existing.Add(layerName);
                    continue;
                }

                // Find first empty user layer (8-31)
                bool found = false;
                for (int i = 8; i < 32; i++)
                {
                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layerName;
                        created.Add(layerName);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    failed.Add(layerName);
                }
            }

            tagManager.ApplyModifiedProperties();

            return new SuccessResponse($"Created {created.Count} layers, {existing.Count} already exist",
                new
                {
                    created = created,
                    existing = existing,
                    failed = failed,
                    totalUserLayers = 32 - 8 - created.Count
                });
        }

        /// <summary>
        /// Sets layer for objects matching a pattern.
        /// </summary>
        private static object BatchSetLayer(JObject @params)
        {
            string pattern = @params["pattern"]?.ToString();
            string layerName = @params["layer"]?.ToString();
            bool includeChildren = @params["includeChildren"]?.ToObject<bool>() ?? true;

            if (string.IsNullOrEmpty(pattern))
            {
                return new ErrorResponse("Pattern is required.");
            }

            if (string.IsNullOrEmpty(layerName))
            {
                return new ErrorResponse("Layer name is required.");
            }

            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex == -1)
            {
                return new ErrorResponse($"Layer not found: {layerName}");
            }

            var matches = FindObjectsByPattern(pattern);
            int count = 0;

            foreach (var obj in matches)
            {
                if (includeChildren)
                {
                    SetLayerRecursive(obj, layerIndex);
                    count += obj.GetComponentsInChildren<Transform>(true).Length;
                }
                else
                {
                    Undo.RecordObject(obj, "Set Layer");
                    obj.layer = layerIndex;
                    count++;
                }
            }

            return new SuccessResponse($"Set layer '{layerName}' on {count} objects",
                new
                {
                    pattern = pattern,
                    layer = layerName,
                    layerIndex = layerIndex,
                    matchCount = matches.Count,
                    affectedCount = count
                });
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            Undo.RecordObject(obj, "Set Layer Recursive");
            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Sets tag for objects matching a pattern.
        /// </summary>
        private static object BatchSetTag(JObject @params)
        {
            string pattern = @params["pattern"]?.ToString();
            string tag = @params["tag"]?.ToString();

            if (string.IsNullOrEmpty(pattern))
            {
                return new ErrorResponse("Pattern is required.");
            }

            if (string.IsNullOrEmpty(tag))
            {
                return new ErrorResponse("Tag is required.");
            }

            // Check if tag exists, create if needed
            try
            {
                // Test if tag exists
                GameObject.FindGameObjectsWithTag(tag);
            }
            catch
            {
                // Tag doesn't exist, create it
                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty tagsProp = tagManager.FindProperty("tags");

                // Check if already in list
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    tagManager.ApplyModifiedProperties();
                }
            }

            var matches = FindObjectsByPattern(pattern);
            int count = 0;

            foreach (var obj in matches)
            {
                Undo.RecordObject(obj, "Set Tag");
                obj.tag = tag;
                count++;
            }

            return new SuccessResponse($"Set tag '{tag}' on {count} objects",
                new
                {
                    pattern = pattern,
                    tag = tag,
                    matchCount = matches.Count
                });
        }

        /// <summary>
        /// Marks objects as static for batching, navigation, etc.
        /// </summary>
        private static object BatchSetStatic(JObject @params)
        {
            string pattern = @params["pattern"]?.ToString();
            bool isStatic = @params["static"]?.ToObject<bool>() ?? true;
            bool includeChildren = @params["includeChildren"]?.ToObject<bool>() ?? true;
            JArray flagsArray = @params["flags"] as JArray;

            if (string.IsNullOrEmpty(pattern))
            {
                return new ErrorResponse("Pattern is required.");
            }

            // Parse static flags
            StaticEditorFlags flags = StaticEditorFlags.ContributeGI |
                                      StaticEditorFlags.OccluderStatic |
                                      StaticEditorFlags.OccludeeStatic |
                                      StaticEditorFlags.BatchingStatic |
                                      StaticEditorFlags.NavigationStatic |
                                      StaticEditorFlags.ReflectionProbeStatic;

            if (flagsArray != null && flagsArray.Count > 0)
            {
                flags = 0;
                foreach (var flagToken in flagsArray)
                {
                    string flagName = flagToken.ToString().ToLowerInvariant();
                    flags |= flagName switch
                    {
                        "contributegi" or "lightmap" => StaticEditorFlags.ContributeGI,
                        "occluder" => StaticEditorFlags.OccluderStatic,
                        "occludee" => StaticEditorFlags.OccludeeStatic,
                        "batching" => StaticEditorFlags.BatchingStatic,
                        "navigation" => StaticEditorFlags.NavigationStatic,
                        "reflectionprobe" => StaticEditorFlags.ReflectionProbeStatic,
                        _ => 0
                    };
                }
            }

            var matches = FindObjectsByPattern(pattern);
            int count = 0;

            foreach (var obj in matches)
            {
                if (includeChildren)
                {
                    foreach (var child in obj.GetComponentsInChildren<Transform>(true))
                    {
                        Undo.RecordObject(child.gameObject, "Set Static");
                        if (isStatic)
                        {
                            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
                        }
                        else
                        {
                            GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
                        }
                        count++;
                    }
                }
                else
                {
                    Undo.RecordObject(obj, "Set Static");
                    if (isStatic)
                    {
                        GameObjectUtility.SetStaticEditorFlags(obj, flags);
                    }
                    else
                    {
                        GameObjectUtility.SetStaticEditorFlags(obj, 0);
                    }
                    count++;
                }
            }

            return new SuccessResponse($"Set static={isStatic} on {count} objects",
                new
                {
                    pattern = pattern,
                    isStatic = isStatic,
                    flags = flags.ToString(),
                    matchCount = matches.Count,
                    affectedCount = count
                });
        }

        /// <summary>
        /// Gets scene statistics.
        /// </summary>
        private static object GetStatistics(JObject @params)
        {
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            var rigidbodies = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

            // Count triangles
            long totalTriangles = 0;
            long totalVertices = 0;
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    totalTriangles += mf.sharedMesh.triangles.Length / 3;
                    totalVertices += mf.sharedMesh.vertexCount;
                }
            }

            // Count static objects
            int staticCount = allObjects.Count(obj => obj.isStatic);

            // Count by layer
            var layerCounts = new Dictionary<string, int>();
            foreach (var obj in allObjects)
            {
                string layerName = LayerMask.LayerToName(obj.layer);
                if (string.IsNullOrEmpty(layerName)) layerName = $"Layer {obj.layer}";

                if (!layerCounts.ContainsKey(layerName))
                    layerCounts[layerName] = 0;
                layerCounts[layerName]++;
            }

            // Count collider types
            var colliderTypes = new Dictionary<string, int>();
            foreach (var col in colliders)
            {
                string typeName = col.GetType().Name;
                if (!colliderTypes.ContainsKey(typeName))
                    colliderTypes[typeName] = 0;
                colliderTypes[typeName]++;
            }

            return new SuccessResponse($"Scene statistics: {allObjects.Length} objects",
                new
                {
                    totalObjects = allObjects.Length,
                    staticObjects = staticCount,
                    meshCount = meshFilters.Length,
                    totalTriangles = totalTriangles,
                    totalVertices = totalVertices,
                    colliderCount = colliders.Length,
                    colliderTypes = colliderTypes,
                    rigidbodyCount = rigidbodies.Length,
                    lightCount = lights.Length,
                    cameraCount = cameras.Length,
                    audioSourceCount = audioSources.Length,
                    layerCounts = layerCounts
                });
        }

        /// <summary>
        /// Validates level for common issues.
        /// </summary>
        private static object ValidateLevel(JObject @params)
        {
            var issues = new List<object>();

            // Check for missing meshes
            var meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null)
                {
                    issues.Add(new { type = "error", category = "Mesh", message = $"Missing mesh on '{mf.gameObject.name}'", objectName = mf.gameObject.name });
                }
            }

            // Check for missing materials
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                {
                    issues.Add(new { type = "warning", category = "Material", message = $"Missing material on '{renderer.gameObject.name}'", objectName = renderer.gameObject.name });
                }

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        issues.Add(new { type = "warning", category = "Material", message = $"Null material in array on '{renderer.gameObject.name}'", objectName = renderer.gameObject.name });
                    }
                }
            }

            // Check for overlapping colliders at same position
            var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            var colliderPositions = new Dictionary<Vector3, List<string>>();
            foreach (var col in colliders)
            {
                Vector3 pos = Vector3Int.RoundToInt(col.bounds.center);
                if (!colliderPositions.ContainsKey(pos))
                    colliderPositions[pos] = new List<string>();
                colliderPositions[pos].Add(col.gameObject.name);
            }

            foreach (var kvp in colliderPositions)
            {
                if (kvp.Value.Count > 3)
                {
                    issues.Add(new { type = "warning", category = "Physics", message = $"Many overlapping colliders at {kvp.Key}: {string.Join(", ", kvp.Value.Take(5))}" });
                }
            }

            // Check for large scale values
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in transforms)
            {
                if (t.localScale.x > 100 || t.localScale.y > 100 || t.localScale.z > 100 ||
                    t.localScale.x < 0.01f || t.localScale.y < 0.01f || t.localScale.z < 0.01f)
                {
                    issues.Add(new { type = "warning", category = "Transform", message = $"Extreme scale on '{t.gameObject.name}': {t.localScale}", objectName = t.gameObject.name });
                }
            }

            // Check for far-from-origin objects
            foreach (var t in transforms)
            {
                if (Mathf.Abs(t.position.x) > 10000 || Mathf.Abs(t.position.y) > 10000 || Mathf.Abs(t.position.z) > 10000)
                {
                    issues.Add(new { type = "warning", category = "Transform", message = $"Object '{t.gameObject.name}' is far from origin: {t.position}", objectName = t.gameObject.name });
                }
            }

            // Check for negative scales (can cause issues)
            foreach (var t in transforms)
            {
                if (t.localScale.x < 0 || t.localScale.y < 0 || t.localScale.z < 0)
                {
                    issues.Add(new { type = "info", category = "Transform", message = $"Negative scale on '{t.gameObject.name}': {t.localScale}", objectName = t.gameObject.name });
                }
            }

            // Check lights
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int realtimeLights = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
            if (realtimeLights > 8)
            {
                issues.Add(new { type = "warning", category = "Lighting", message = $"Many realtime lights ({realtimeLights}). Consider baking some lights." });
            }

            string status = issues.Count == 0 ? "Valid" :
                issues.Any(i => ((dynamic)i).type == "error") ? "Errors Found" : "Warnings Only";

            return new SuccessResponse($"Level validation complete: {status}",
                new
                {
                    status = status,
                    issueCount = issues.Count,
                    errorCount = issues.Count(i => ((dynamic)i).type == "error"),
                    warningCount = issues.Count(i => ((dynamic)i).type == "warning"),
                    infoCount = issues.Count(i => ((dynamic)i).type == "info"),
                    issues = issues
                });
        }

        #region Helper Methods

        private static List<GameObject> FindObjectsByPattern(string pattern)
        {
            var results = new List<GameObject>();
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            // Check if pattern is a regex
            bool isRegex = pattern.StartsWith("/") && pattern.EndsWith("/");
            Regex regex = null;

            if (isRegex)
            {
                string regexPattern = pattern.Substring(1, pattern.Length - 2);
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            }

            foreach (var obj in allObjects)
            {
                bool matches = false;

                if (regex != null)
                {
                    matches = regex.IsMatch(obj.name);
                }
                else if (pattern.Contains("*"))
                {
                    // Wildcard pattern
                    string wildcardPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    matches = Regex.IsMatch(obj.name, wildcardPattern, RegexOptions.IgnoreCase);
                }
                else
                {
                    // Exact match or contains
                    matches = obj.name.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                }

                if (matches)
                {
                    results.Add(obj);
                }
            }

            return results;
        }

        #endregion
    }
}
