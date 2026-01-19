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
    /// Advanced search functionality for assets and scene objects.
    /// </summary>
    [McpForUnityTool("search", AutoRegister = false, Description = "Search for assets, scene objects, and perform advanced queries.")]
    public static class ManageSearch
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                action = "search_assets"; // Default action
            }

            try
            {
                switch (action)
                {
                    case "search_assets":
                        return SearchAssets(@params);

                    case "search_scenes":
                        return SearchScenes(@params);

                    case "find_by_type":
                        return FindAssetsByType(@params);

                    case "find_by_label":
                        return FindAssetsByLabel(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: search_assets, search_scenes, find_by_type, find_by_label");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object SearchAssets(JObject @params)
        {
            string query = @params["query"]?.ToString();
            if (string.IsNullOrEmpty(query))
            {
                return new ErrorResponse("query parameter is required");
            }

            string typeFilter = @params["type"]?.ToString();
            string[] searchInFolders = (@params["folders"] as JArray)?.Select(f => f.ToString()).ToArray();

            // Build search filter
            string filter = query;
            if (!string.IsNullOrEmpty(typeFilter))
            {
                filter = $"t:{typeFilter} {query}";
            }

            string[] guids = searchInFolders != null && searchInFolders.Length > 0
                ? AssetDatabase.FindAssets(filter, searchInFolders)
                : AssetDatabase.FindAssets(filter);

            var results = guids.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                return new
                {
                    guid = guid,
                    path = path,
                    name = asset != null ? asset.name : System.IO.Path.GetFileNameWithoutExtension(path),
                    type = asset != null ? asset.GetType().Name : "Unknown"
                };
            }).ToList();

            return new SuccessResponse($"Found {results.Count} assets", new
            {
                count = results.Count,
                query = query,
                filter = filter,
                results = results
            });
        }

        private static object SearchScenes(JObject @params)
        {
            string objectName = @params["objectName"]?.ToString();
            string componentType = @params["componentType"]?.ToString();

            if (string.IsNullOrEmpty(objectName) && string.IsNullOrEmpty(componentType))
            {
                return new ErrorResponse("Either objectName or componentType is required");
            }

            var results = new List<object>();

            // Search in all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    SearchInHierarchy(root.transform, objectName, componentType, scene.name, results);
                }
            }

            return new SuccessResponse($"Found {results.Count} objects", new
            {
                count = results.Count,
                objectName = objectName,
                componentType = componentType,
                results = results
            });
        }

        private static void SearchInHierarchy(Transform obj, string objectName, string componentType, string sceneName, List<object> results)
        {
            bool nameMatch = string.IsNullOrEmpty(objectName) || obj.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0;
            bool typeMatch = string.IsNullOrEmpty(componentType) || obj.GetComponent(componentType) != null;

            if (nameMatch && typeMatch)
            {
                string path = GetGameObjectPath(obj);
                results.Add(new
                {
                    name = obj.name,
                    path = path,
                    scene = sceneName,
                    instanceID = obj.gameObject.GetInstanceID()
                });
            }

            foreach (Transform child in obj)
            {
                SearchInHierarchy(child, objectName, componentType, sceneName, results);
            }
        }

        private static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static object FindAssetsByType(JObject @params)
        {
            string typeName = @params["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
            {
                return new ErrorResponse("type parameter is required");
            }

            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");

            var results = guids.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return new
                {
                    guid = guid,
                    path = path,
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
            }).ToList();

            return new SuccessResponse($"Found {results.Count} assets of type {typeName}", new
            {
                count = results.Count,
                type = typeName,
                results = results
            });
        }

        private static object FindAssetsByLabel(JObject @params)
        {
            string label = @params["label"]?.ToString();
            if (string.IsNullOrEmpty(label))
            {
                return new ErrorResponse("label parameter is required");
            }

            string[] guids = AssetDatabase.FindAssets($"l:{label}");

            var results = guids.Select(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                return new
                {
                    guid = guid,
                    path = path,
                    name = asset != null ? asset.name : System.IO.Path.GetFileNameWithoutExtension(path),
                    type = asset != null ? asset.GetType().Name : "Unknown"
                };
            }).ToList();

            return new SuccessResponse($"Found {results.Count} assets with label '{label}'", new
            {
                count = results.Count,
                label = label,
                results = results
            });
        }
    }
}
