using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles object arrangement operations including array patterns, alignment, distribution, and snapping.
    /// </summary>
    [McpForUnityTool("manage_object_arrangement", AutoRegister = false,
        Description = "Arrange and duplicate objects in patterns. Actions: array_linear (duplicate along a line), array_grid (duplicate in 2D/3D grid), array_circular (duplicate around circle/arc), align_objects (align multiple objects on axis), distribute_objects (space evenly), snap_to_ground (raycast down to floor), randomize_transform (add random variation). Source can be GameObject name, instance ID, or prefab path.")]
    public static class ManageObjectArrangement
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: array_linear, array_grid, array_circular, align_objects, distribute_objects, snap_to_ground, randomize_transform, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_object_arrangement" }),
                    "array_linear" => ArrayLinear(@params),
                    "array_grid" => ArrayGrid(@params),
                    "array_circular" => ArrayCircular(@params),
                    "align_objects" => AlignObjects(@params),
                    "distribute_objects" => DistributeObjects(@params),
                    "snap_to_ground" => SnapToGround(@params),
                    "randomize_transform" => RandomizeTransform(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: array_linear, array_grid, array_circular, align_objects, distribute_objects, snap_to_ground, randomize_transform, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Duplicates an object along a line with spacing.
        /// </summary>
        private static object ArrayLinear(JObject @params)
        {
            GameObject source = ResolveSource(@params);
            if (source == null)
            {
                return new ErrorResponse("Source object not found. Provide 'source' as GameObject name, instance ID, or prefab path.");
            }

            int count = @params["count"]?.ToObject<int>() ?? 5;
            count = Mathf.Clamp(count, 1, 100);

            Vector3 direction = VectorParsing.ParseVector3OrDefault(@params["direction"], Vector3.right);
            float spacing = @params["spacing"]?.ToObject<float>() ?? 2f;
            string parentName = @params["parent"]?.ToString();
            bool includeSource = @params["includeSource"]?.ToObject<bool>() ?? false;

            // Get or create parent
            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                parent = GameObject.Find(parentName);
                if (parent == null)
                {
                    parent = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parent, $"Create Array Parent '{parentName}'");
                }
            }

            var created = new List<GameObject>();
            Vector3 normalizedDirection = direction.normalized;

            int startIndex = includeSource ? 0 : 1;

            for (int i = startIndex; i < count; i++)
            {
                GameObject instance;
                if (IsPrefab(source))
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                }
                else
                {
                    instance = UnityEngine.Object.Instantiate(source);
                }

                instance.name = $"{source.name}_{i}";
                instance.transform.position = source.transform.position + normalizedDirection * spacing * i;
                instance.transform.rotation = source.transform.rotation;

                if (parent != null)
                {
                    instance.transform.SetParent(parent.transform, true);
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Array Linear '{instance.name}'");
                created.Add(instance);
            }

            Selection.objects = created.ToArray();

            return new SuccessResponse($"Created {created.Count} objects in linear array",
                new
                {
                    sourceObject = source.name,
                    count = created.Count,
                    direction = new { x = normalizedDirection.x, y = normalizedDirection.y, z = normalizedDirection.z },
                    spacing = spacing,
                    objects = created.Select(g => new { name = g.name, instanceId = g.GetInstanceID() }).ToList()
                });
        }

        /// <summary>
        /// Duplicates an object in a 2D/3D grid pattern.
        /// </summary>
        private static object ArrayGrid(JObject @params)
        {
            GameObject source = ResolveSource(@params);
            if (source == null)
            {
                return new ErrorResponse("Source object not found. Provide 'source' as GameObject name, instance ID, or prefab path.");
            }

            int countX = @params["countX"]?.ToObject<int>() ?? 3;
            int countY = @params["countY"]?.ToObject<int>() ?? 1;
            int countZ = @params["countZ"]?.ToObject<int>() ?? 3;

            countX = Mathf.Clamp(countX, 1, 50);
            countY = Mathf.Clamp(countY, 1, 50);
            countZ = Mathf.Clamp(countZ, 1, 50);

            if (countX * countY * countZ > 1000)
            {
                return new ErrorResponse("Grid too large. Maximum 1000 total objects.");
            }

            float spacingX = @params["spacingX"]?.ToObject<float>() ?? 2f;
            float spacingY = @params["spacingY"]?.ToObject<float>() ?? 2f;
            float spacingZ = @params["spacingZ"]?.ToObject<float>() ?? 2f;
            string parentName = @params["parent"]?.ToString();
            bool centerGrid = @params["centerGrid"]?.ToObject<bool>() ?? true;

            // Get or create parent
            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                parent = GameObject.Find(parentName);
                if (parent == null)
                {
                    parent = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parent, $"Create Grid Parent '{parentName}'");
                }
            }

            var created = new List<GameObject>();
            Vector3 offset = Vector3.zero;

            if (centerGrid)
            {
                offset = new Vector3(
                    -(countX - 1) * spacingX / 2f,
                    -(countY - 1) * spacingY / 2f,
                    -(countZ - 1) * spacingZ / 2f
                );
            }

            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    for (int z = 0; z < countZ; z++)
                    {
                        // Skip the origin position if source is there
                        if (x == 0 && y == 0 && z == 0 && centerGrid)
                            continue;

                        GameObject instance;
                        if (IsPrefab(source))
                        {
                            instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                        }
                        else
                        {
                            instance = UnityEngine.Object.Instantiate(source);
                        }

                        instance.name = $"{source.name}_{x}_{y}_{z}";
                        instance.transform.position = source.transform.position + offset +
                            new Vector3(x * spacingX, y * spacingY, z * spacingZ);
                        instance.transform.rotation = source.transform.rotation;

                        if (parent != null)
                        {
                            instance.transform.SetParent(parent.transform, true);
                        }

                        Undo.RegisterCreatedObjectUndo(instance, $"Array Grid '{instance.name}'");
                        created.Add(instance);
                    }
                }
            }

            Selection.objects = created.ToArray();

            return new SuccessResponse($"Created {created.Count} objects in {countX}x{countY}x{countZ} grid",
                new
                {
                    sourceObject = source.name,
                    count = created.Count,
                    grid = new { x = countX, y = countY, z = countZ },
                    spacing = new { x = spacingX, y = spacingY, z = spacingZ }
                });
        }

        /// <summary>
        /// Duplicates an object around a circle or arc.
        /// </summary>
        private static object ArrayCircular(JObject @params)
        {
            GameObject source = ResolveSource(@params);
            if (source == null)
            {
                return new ErrorResponse("Source object not found. Provide 'source' as GameObject name, instance ID, or prefab path.");
            }

            int count = @params["count"]?.ToObject<int>() ?? 8;
            count = Mathf.Clamp(count, 1, 100);

            float radius = @params["radius"]?.ToObject<float>() ?? 5f;
            float startAngle = @params["startAngle"]?.ToObject<float>() ?? 0f;
            float endAngle = @params["endAngle"]?.ToObject<float>() ?? 360f;
            Vector3 center = VectorParsing.ParseVector3OrDefault(@params["center"], source.transform.position);
            Vector3 axis = VectorParsing.ParseVector3OrDefault(@params["axis"], Vector3.up);
            string parentName = @params["parent"]?.ToString();
            bool faceCenter = @params["faceCenter"]?.ToObject<bool>() ?? false;
            bool faceOutward = @params["faceOutward"]?.ToObject<bool>() ?? false;

            // Get or create parent
            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                parent = GameObject.Find(parentName);
                if (parent == null)
                {
                    parent = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parent, $"Create Circle Parent '{parentName}'");
                }
            }

            var created = new List<GameObject>();
            axis = axis.normalized;

            // Calculate perpendicular axes
            Vector3 forward = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(axis, forward)) > 0.9f)
            {
                forward = Vector3.right;
            }
            Vector3 right = Vector3.Cross(axis, forward).normalized;
            forward = Vector3.Cross(right, axis).normalized;

            float angleStep = (endAngle - startAngle) / (endAngle >= 360f ? count : count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * forward) * radius;
                Vector3 position = center + offset;

                GameObject instance;
                if (IsPrefab(source))
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                }
                else
                {
                    instance = UnityEngine.Object.Instantiate(source);
                }

                instance.name = $"{source.name}_{i}";
                instance.transform.position = position;

                if (faceCenter)
                {
                    instance.transform.LookAt(center, axis);
                }
                else if (faceOutward)
                {
                    instance.transform.LookAt(position + offset, axis);
                }
                else
                {
                    instance.transform.rotation = source.transform.rotation;
                }

                if (parent != null)
                {
                    instance.transform.SetParent(parent.transform, true);
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Array Circular '{instance.name}'");
                created.Add(instance);
            }

            Selection.objects = created.ToArray();

            return new SuccessResponse($"Created {created.Count} objects in circular array",
                new
                {
                    sourceObject = source.name,
                    count = created.Count,
                    radius = radius,
                    startAngle = startAngle,
                    endAngle = endAngle,
                    center = new { x = center.x, y = center.y, z = center.z }
                });
        }

        /// <summary>
        /// Aligns multiple objects along an axis.
        /// </summary>
        private static object AlignObjects(JObject @params)
        {
            var targets = ResolveTargets(@params);
            if (targets.Count < 2)
            {
                return new ErrorResponse("At least 2 targets required for alignment. Provide 'targets' as array of names/IDs.");
            }

            string axis = @params["axis"]?.ToString()?.ToLowerInvariant() ?? "x";
            string alignTo = @params["alignTo"]?.ToString()?.ToLowerInvariant() ?? "center";

            float targetValue = 0f;

            switch (alignTo)
            {
                case "min":
                    targetValue = targets.Min(t => GetAxisValue(t.transform.position, axis));
                    break;
                case "max":
                    targetValue = targets.Max(t => GetAxisValue(t.transform.position, axis));
                    break;
                case "center":
                    float min = targets.Min(t => GetAxisValue(t.transform.position, axis));
                    float max = targets.Max(t => GetAxisValue(t.transform.position, axis));
                    targetValue = (min + max) / 2f;
                    break;
                case "first":
                    targetValue = GetAxisValue(targets[0].transform.position, axis);
                    break;
                case "last":
                    targetValue = GetAxisValue(targets[targets.Count - 1].transform.position, axis);
                    break;
                default:
                    if (float.TryParse(alignTo, out float customValue))
                    {
                        targetValue = customValue;
                    }
                    break;
            }

            foreach (var target in targets)
            {
                Undo.RecordObject(target.transform, "Align Object");
                Vector3 pos = target.transform.position;
                target.transform.position = SetAxisValue(pos, axis, targetValue);
            }

            return new SuccessResponse($"Aligned {targets.Count} objects on {axis} axis to {alignTo}",
                new
                {
                    count = targets.Count,
                    axis = axis,
                    alignTo = alignTo,
                    value = targetValue
                });
        }

        /// <summary>
        /// Distributes objects evenly between bounds.
        /// </summary>
        private static object DistributeObjects(JObject @params)
        {
            var targets = ResolveTargets(@params);
            if (targets.Count < 3)
            {
                return new ErrorResponse("At least 3 targets required for distribution. Provide 'targets' as array of names/IDs.");
            }

            string axis = @params["axis"]?.ToString()?.ToLowerInvariant() ?? "x";
            bool sortFirst = @params["sortFirst"]?.ToObject<bool>() ?? true;

            // Sort by axis if requested
            if (sortFirst)
            {
                targets = targets.OrderBy(t => GetAxisValue(t.transform.position, axis)).ToList();
            }

            float min = GetAxisValue(targets[0].transform.position, axis);
            float max = GetAxisValue(targets[targets.Count - 1].transform.position, axis);
            float step = (max - min) / (targets.Count - 1);

            for (int i = 1; i < targets.Count - 1; i++)
            {
                Undo.RecordObject(targets[i].transform, "Distribute Object");
                Vector3 pos = targets[i].transform.position;
                targets[i].transform.position = SetAxisValue(pos, axis, min + step * i);
            }

            return new SuccessResponse($"Distributed {targets.Count} objects evenly on {axis} axis",
                new
                {
                    count = targets.Count,
                    axis = axis,
                    min = min,
                    max = max,
                    step = step
                });
        }

        /// <summary>
        /// Snaps objects to the ground using raycasting.
        /// </summary>
        private static object SnapToGround(JObject @params)
        {
            var targets = ResolveTargets(@params);
            if (targets.Count == 0)
            {
                return new ErrorResponse("No targets found. Provide 'targets' as array of names/IDs.");
            }

            float maxDistance = @params["maxDistance"]?.ToObject<float>() ?? 100f;
            float offset = @params["offset"]?.ToObject<float>() ?? 0f;
            int layerMask = @params["layerMask"]?.ToObject<int>() ?? Physics.DefaultRaycastLayers;
            bool alignToNormal = @params["alignToNormal"]?.ToObject<bool>() ?? false;

            int snappedCount = 0;
            var results = new List<object>();

            foreach (var target in targets)
            {
                Undo.RecordObject(target.transform, "Snap To Ground");

                Vector3 origin = target.transform.position + Vector3.up * 0.1f;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, layerMask))
                {
                    target.transform.position = hit.point + Vector3.up * offset;

                    if (alignToNormal)
                    {
                        target.transform.up = hit.normal;
                    }

                    snappedCount++;
                    results.Add(new
                    {
                        name = target.name,
                        snapped = true,
                        groundHeight = hit.point.y,
                        surface = hit.collider.gameObject.name
                    });
                }
                else
                {
                    results.Add(new
                    {
                        name = target.name,
                        snapped = false,
                        reason = "No ground found"
                    });
                }
            }

            return new SuccessResponse($"Snapped {snappedCount}/{targets.Count} objects to ground",
                new
                {
                    total = targets.Count,
                    snapped = snappedCount,
                    objects = results
                });
        }

        /// <summary>
        /// Adds random variation to object transforms.
        /// </summary>
        private static object RandomizeTransform(JObject @params)
        {
            var targets = ResolveTargets(@params);
            if (targets.Count == 0)
            {
                return new ErrorResponse("No targets found. Provide 'targets' as array of names/IDs.");
            }

            Vector3 positionRange = VectorParsing.ParseVector3OrDefault(@params["positionRange"]);
            Vector3 rotationRange = VectorParsing.ParseVector3OrDefault(@params["rotationRange"]);
            Vector3 scaleRange = VectorParsing.ParseVector3OrDefault(@params["scaleRange"]);
            float uniformScaleRange = @params["uniformScaleRange"]?.ToObject<float>() ?? 0f;
            int? seed = @params["seed"]?.ToObject<int>();

            if (seed.HasValue)
            {
                UnityEngine.Random.InitState(seed.Value);
            }

            foreach (var target in targets)
            {
                Undo.RecordObject(target.transform, "Randomize Transform");

                // Position
                if (positionRange != Vector3.zero)
                {
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-positionRange.x, positionRange.x),
                        UnityEngine.Random.Range(-positionRange.y, positionRange.y),
                        UnityEngine.Random.Range(-positionRange.z, positionRange.z)
                    );
                    target.transform.position += randomOffset;
                }

                // Rotation
                if (rotationRange != Vector3.zero)
                {
                    Vector3 randomRotation = new Vector3(
                        UnityEngine.Random.Range(-rotationRange.x, rotationRange.x),
                        UnityEngine.Random.Range(-rotationRange.y, rotationRange.y),
                        UnityEngine.Random.Range(-rotationRange.z, rotationRange.z)
                    );
                    target.transform.eulerAngles += randomRotation;
                }

                // Scale
                if (scaleRange != Vector3.zero)
                {
                    Vector3 randomScale = new Vector3(
                        1f + UnityEngine.Random.Range(-scaleRange.x, scaleRange.x),
                        1f + UnityEngine.Random.Range(-scaleRange.y, scaleRange.y),
                        1f + UnityEngine.Random.Range(-scaleRange.z, scaleRange.z)
                    );
                    target.transform.localScale = Vector3.Scale(target.transform.localScale, randomScale);
                }
                else if (uniformScaleRange > 0)
                {
                    float randomScale = 1f + UnityEngine.Random.Range(-uniformScaleRange, uniformScaleRange);
                    target.transform.localScale *= randomScale;
                }
            }

            return new SuccessResponse($"Randomized transforms of {targets.Count} objects",
                new
                {
                    count = targets.Count,
                    positionRange = new { x = positionRange.x, y = positionRange.y, z = positionRange.z },
                    rotationRange = new { x = rotationRange.x, y = rotationRange.y, z = rotationRange.z },
                    scaleRange = new { x = scaleRange.x, y = scaleRange.y, z = scaleRange.z }
                });
        }

        #region Helper Methods

        private static GameObject ResolveSource(JObject @params)
        {
            string source = @params["source"]?.ToString();
            if (string.IsNullOrEmpty(source))
                return null;

            // Try as instance ID
            if (int.TryParse(source, out int instanceId))
            {
                return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            }

            // Try as scene object by name
            GameObject sceneObj = GameObject.Find(source);
            if (sceneObj != null)
                return sceneObj;

            // Try as prefab path
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (prefab != null)
                return prefab;

            // Try to find prefab by name
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {source}");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            return null;
        }

        private static List<GameObject> ResolveTargets(JObject @params)
        {
            var result = new List<GameObject>();
            JArray targets = @params["targets"] as JArray;

            if (targets != null)
            {
                foreach (var target in targets)
                {
                    string targetStr = target.ToString();

                    // Try as instance ID
                    if (int.TryParse(targetStr, out int instanceId))
                    {
                        var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                        if (obj != null)
                            result.Add(obj);
                        continue;
                    }

                    // Try as name
                    var found = GameObject.Find(targetStr);
                    if (found != null)
                        result.Add(found);
                }
            }

            // Fallback to selection if no targets specified
            if (result.Count == 0 && Selection.gameObjects.Length > 0)
            {
                result.AddRange(Selection.gameObjects);
            }

            return result;
        }

        private static bool IsPrefab(GameObject obj)
        {
            return PrefabUtility.IsPartOfPrefabAsset(obj);
        }

        private static float GetAxisValue(Vector3 v, string axis)
        {
            return axis switch
            {
                "x" => v.x,
                "y" => v.y,
                "z" => v.z,
                _ => v.x
            };
        }

        private static Vector3 SetAxisValue(Vector3 v, string axis, float value)
        {
            return axis switch
            {
                "x" => new Vector3(value, v.y, v.z),
                "y" => new Vector3(v.x, value, v.z),
                "z" => new Vector3(v.x, v.y, value),
                _ => v
            };
        }

        #endregion
    }
}
