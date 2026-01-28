using System;
using System.Collections.Generic;
using System.IO;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCPForUnity.Editor.Tools.Prefabs
{
    [McpForUnityTool("manage_prefabs", AutoRegister = false, Description = "Create prefabs from GameObjects and manage prefab stages (open/save/close).")]
    /// <summary>
    /// Tool to manage Unity Prefab stages and create prefabs from GameObjects.
    /// </summary>
    public static class ManagePrefabs
    {
        private const string SupportedActions = "open_stage, close_stage, save_open_stage, create_from_gameobject, create_from_model";

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse($"Action parameter is required. Valid actions are: {SupportedActions}.");
            }

            try
            {
                switch (action)
                {
                    case "open_stage":
                        return OpenStage(@params);
                    case "close_stage":
                        return CloseStage(@params);
                    case "save_open_stage":
                        return SaveOpenStage();
                    case "create_from_gameobject":
                        return CreatePrefabFromGameObject(@params);
                    case "create_from_model":
                        return CreatePrefabFromModel(@params);
                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions are: {SupportedActions}.");
                }
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManagePrefabs] Action '{action}' failed: {e}");
                return new ErrorResponse($"Internal error: {e.Message}");
            }
        }

        private static object OpenStage(JObject @params)
        {
            string prefabPath = @params["prefabPath"]?.ToString();
            if (string.IsNullOrEmpty(prefabPath))
            {
                return new ErrorResponse("'prefabPath' parameter is required for open_stage.");
            }

            string sanitizedPath = AssetPathUtility.SanitizeAssetPath(prefabPath);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sanitizedPath);
            if (prefabAsset == null)
            {
                return new ErrorResponse($"No prefab asset found at path '{sanitizedPath}'.");
            }

            string modeValue = @params["mode"]?.ToString();
            if (!string.IsNullOrEmpty(modeValue) && !modeValue.Equals(PrefabStage.Mode.InIsolation.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorResponse("Only PrefabStage mode 'InIsolation' is supported at this time.");
            }

            PrefabStage stage = PrefabStageUtility.OpenPrefab(sanitizedPath);
            if (stage == null)
            {
                return new ErrorResponse($"Failed to open prefab stage for '{sanitizedPath}'.");
            }

            return new SuccessResponse($"Opened prefab stage for '{sanitizedPath}'.", SerializeStage(stage));
        }

        private static object CloseStage(JObject @params)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                return new SuccessResponse("No prefab stage was open.");
            }

            bool saveBeforeClose = @params["saveBeforeClose"]?.ToObject<bool>() ?? false;
            if (saveBeforeClose && stage.scene.isDirty)
            {
                SaveStagePrefab(stage);
                AssetDatabase.SaveAssets();
            }

            StageUtility.GoToMainStage();
            return new SuccessResponse($"Closed prefab stage for '{stage.assetPath}'.");
        }

        private static object SaveOpenStage()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                return new ErrorResponse("No prefab stage is currently open.");
            }

            SaveStagePrefab(stage);
            AssetDatabase.SaveAssets();
            return new SuccessResponse($"Saved prefab stage for '{stage.assetPath}'.", SerializeStage(stage));
        }

        private static void SaveStagePrefab(PrefabStage stage)
        {
            if (stage?.prefabContentsRoot == null)
            {
                throw new InvalidOperationException("Cannot save prefab stage without a prefab root.");
            }

            bool saved = PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            if (!saved)
            {
                throw new InvalidOperationException($"Failed to save prefab asset at '{stage.assetPath}'.");
            }
        }

        private static object CreatePrefabFromGameObject(JObject @params)
        {
            string targetName = @params["target"]?.ToString() ?? @params["name"]?.ToString();
            if (string.IsNullOrEmpty(targetName))
            {
                return new ErrorResponse("'target' parameter is required for create_from_gameobject.");
            }

            bool includeInactive = @params["searchInactive"]?.ToObject<bool>() ?? false;
            GameObject sourceObject = FindSceneObjectByName(targetName, includeInactive);
            if (sourceObject == null)
            {
                return new ErrorResponse($"GameObject '{targetName}' not found in the active scene.");
            }

            if (PrefabUtility.IsPartOfPrefabAsset(sourceObject))
            {
                return new ErrorResponse(
                    $"GameObject '{sourceObject.name}' is part of a prefab asset. Open the prefab stage to save changes instead."
                );
            }

            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(sourceObject);
            if (status != PrefabInstanceStatus.NotAPrefab)
            {
                return new ErrorResponse(
                    $"GameObject '{sourceObject.name}' is already linked to an existing prefab instance."
                );
            }

            string requestedPath = @params["prefabPath"]?.ToString();
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                return new ErrorResponse("'prefabPath' parameter is required for create_from_gameobject.");
            }

            string sanitizedPath = AssetPathUtility.SanitizeAssetPath(requestedPath);
            if (!sanitizedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                sanitizedPath += ".prefab";
            }

            bool allowOverwrite = @params["allowOverwrite"]?.ToObject<bool>() ?? false;
            string finalPath = sanitizedPath;

            if (!allowOverwrite && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finalPath) != null)
            {
                finalPath = AssetDatabase.GenerateUniqueAssetPath(finalPath);
            }

            EnsureAssetDirectoryExists(finalPath);

            try
            {
                GameObject connectedInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    sourceObject,
                    finalPath,
                    InteractionMode.AutomatedAction
                );

                if (connectedInstance == null)
                {
                    return new ErrorResponse($"Failed to save prefab asset at '{finalPath}'.");
                }

                Selection.activeGameObject = connectedInstance;

                return new SuccessResponse(
                    $"Prefab created at '{finalPath}' and instance linked.",
                    new
                    {
                        prefabPath = finalPath,
                        instanceId = connectedInstance.GetInstanceID()
                    }
                );
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Error saving prefab asset at '{finalPath}': {e.Message}");
            }
        }

        private static object CreatePrefabFromModel(JObject @params)
        {
            string modelPath = @params["modelPath"]?.ToString();
            if (string.IsNullOrEmpty(modelPath))
            {
                return new ErrorResponse("'modelPath' parameter is required for create_from_model.");
            }

            string requestedPath = @params["prefabPath"]?.ToString();
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                return new ErrorResponse("'prefabPath' parameter is required for create_from_model.");
            }

            string sanitizedModelPath = AssetPathUtility.SanitizeAssetPath(modelPath);
            string sanitizedPrefabPath = AssetPathUtility.SanitizeAssetPath(requestedPath);

            if (!sanitizedPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                sanitizedPrefabPath += ".prefab";
            }

            // Load the model
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sanitizedModelPath);
            if (modelAsset == null)
            {
                return new ErrorResponse($"Model asset not found: {sanitizedModelPath}");
            }

            bool allowOverwrite = @params["allowOverwrite"]?.ToObject<bool>() ?? false;
            string finalPath = sanitizedPrefabPath;

            if (!allowOverwrite && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finalPath) != null)
            {
                finalPath = AssetDatabase.GenerateUniqueAssetPath(finalPath);
            }

            EnsureAssetDirectoryExists(finalPath);

            try
            {
                // Instantiate the model in the scene temporarily
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                if (instance == null)
                {
                    // Fallback to regular instantiation
                    instance = UnityEngine.Object.Instantiate(modelAsset);
                }

                instance.name = Path.GetFileNameWithoutExtension(finalPath);

                // Add Animator component with controller if specified
                string animatorControllerPath = @params["animatorController"]?.ToString();
                if (!string.IsNullOrEmpty(animatorControllerPath))
                {
                    string sanitizedControllerPath = AssetPathUtility.SanitizeAssetPath(animatorControllerPath);
                    var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(sanitizedControllerPath);

                    if (controller != null)
                    {
                        Animator animator = instance.GetComponent<Animator>();
                        if (animator == null)
                        {
                            animator = instance.AddComponent<Animator>();
                        }
                        animator.runtimeAnimatorController = controller;
                    }
                    else
                    {
                        Debug.LogWarning($"[ManagePrefabs] Animator controller not found: {sanitizedControllerPath}");
                    }
                }

                // Add specified components
                JArray components = @params["components"] as JArray;
                var addedComponents = new List<string>();

                if (components != null)
                {
                    foreach (var componentToken in components)
                    {
                        string componentTypeName = componentToken.ToString();

                        // Try to find the type
                        Type componentType = null;

                        // Search in common assemblies
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            componentType = assembly.GetType(componentTypeName);
                            if (componentType != null) break;

                            // Try with UnityEngine prefix
                            componentType = assembly.GetType($"UnityEngine.{componentTypeName}");
                            if (componentType != null) break;
                        }

                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            if (instance.GetComponent(componentType) == null)
                            {
                                instance.AddComponent(componentType);
                                addedComponents.Add(componentTypeName);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ManagePrefabs] Component type not found or not a Component: {componentTypeName}");
                        }
                    }
                }

                // Save as prefab
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(instance, finalPath);

                // Cleanup scene instance
                UnityEngine.Object.DestroyImmediate(instance);

                if (prefabAsset == null)
                {
                    return new ErrorResponse($"Failed to save prefab asset at '{finalPath}'.");
                }

                return new SuccessResponse(
                    $"Prefab created from model at '{finalPath}'.",
                    new
                    {
                        prefabPath = finalPath,
                        modelPath = sanitizedModelPath,
                        addedComponents = addedComponents,
                        hasAnimator = !string.IsNullOrEmpty(animatorControllerPath)
                    }
                );
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Error creating prefab from model: {e.Message}");
            }
        }

        private static void EnsureAssetDirectoryExists(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string fullDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static GameObject FindSceneObjectByName(string name, bool includeInactive)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage?.prefabContentsRoot != null)
            {
                foreach (Transform transform in stage.prefabContentsRoot.GetComponentsInChildren<Transform>(includeInactive))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive))
                {
                    GameObject candidate = transform.gameObject;
                    if (candidate.name == name)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static object SerializeStage(PrefabStage stage)
        {
            if (stage == null)
            {
                return new { isOpen = false };
            }

            return new
            {
                isOpen = true,
                assetPath = stage.assetPath,
                prefabRootName = stage.prefabContentsRoot != null ? stage.prefabContentsRoot.name : null,
                mode = stage.mode.ToString(),
                isDirty = stage.scene.isDirty
            };
        }

    }
}
