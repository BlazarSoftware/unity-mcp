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
    /// Manages model import settings for FBX/OBJ/GLTF files with humanoid avatar support.
    /// </summary>
    [McpForUnityTool("manage_model_import", AutoRegister = false, Description = "Configure model import settings including humanoid avatar setup.")]
    public static class ManageModelImport
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: get_settings, set_settings, configure_humanoid, validate_rig, get_bone_mapping");
            }

            try
            {
                switch (action)
                {
                    case "get_settings":
                        return GetSettings(@params);

                    case "set_settings":
                        return SetSettings(@params);

                    case "configure_humanoid":
                        return ConfigureHumanoid(@params);

                    case "validate_rig":
                        return ValidateRig(@params);

                    case "get_bone_mapping":
                        return GetBoneMapping(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: get_settings, set_settings, configure_humanoid, validate_rig, get_bone_mapping");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object GetSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            return new SuccessResponse("Retrieved model import settings", new
            {
                assetPath = assetPath,
                globalScale = importer.globalScale,
                meshCompression = importer.meshCompression.ToString(),
                importAnimation = importer.importAnimation,
                animationType = importer.animationType.ToString(),
                materialImportMode = importer.materialImportMode.ToString(),
                isReadable = importer.isReadable,
                optimizeMeshPolygons = importer.optimizeMeshPolygons,
                optimizeMeshVertices = importer.optimizeMeshVertices,
                importNormals = importer.importNormals.ToString(),
                importTangents = importer.importTangents.ToString(),
                generateSecondaryUV = importer.generateSecondaryUV,
                swapUVChannels = importer.swapUVChannels,
                importBlendShapes = importer.importBlendShapes,
                importVisibility = importer.importVisibility,
                importCameras = importer.importCameras,
                importLights = importer.importLights,
                addCollider = importer.addCollider,
                preserveHierarchy = importer.preserveHierarchy,
                sortHierarchyByName = importer.sortHierarchyByName
            });
        }

        private static object SetSettings(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            var changes = new List<string>();

            // Global scale
            if (@params["globalScale"] != null)
            {
                importer.globalScale = @params["globalScale"].ToObject<float>();
                changes.Add($"globalScale={importer.globalScale}");
            }

            // Mesh compression
            if (@params["meshCompression"] != null)
            {
                if (Enum.TryParse<ModelImporterMeshCompression>(@params["meshCompression"].ToString(), true, out var compression))
                {
                    importer.meshCompression = compression;
                    changes.Add($"meshCompression={compression}");
                }
            }

            // Import animation
            if (@params["importAnimation"] != null)
            {
                importer.importAnimation = @params["importAnimation"].ToObject<bool>();
                changes.Add($"importAnimation={importer.importAnimation}");
            }

            // Material import mode
            if (@params["materialImportMode"] != null)
            {
                if (Enum.TryParse<ModelImporterMaterialImportMode>(@params["materialImportMode"].ToString(), true, out var matMode))
                {
                    importer.materialImportMode = matMode;
                    changes.Add($"materialImportMode={matMode}");
                }
            }

            // Read/write enabled
            if (@params["readWriteEnabled"] != null)
            {
                importer.isReadable = @params["readWriteEnabled"].ToObject<bool>();
                changes.Add($"isReadable={importer.isReadable}");
            }

            // Generate colliders
            if (@params["generateColliders"] != null)
            {
                importer.addCollider = @params["generateColliders"].ToObject<bool>();
                changes.Add($"addCollider={importer.addCollider}");
            }

            // Optimize mesh
            if (@params["optimizeMesh"] != null)
            {
                bool optimize = @params["optimizeMesh"].ToObject<bool>();
                importer.optimizeMeshPolygons = optimize;
                importer.optimizeMeshVertices = optimize;
                changes.Add($"optimizeMesh={optimize}");
            }

            if (changes.Count > 0)
            {
                importer.SaveAndReimport();
            }

            return new SuccessResponse($"Updated {changes.Count} settings", new
            {
                assetPath = assetPath,
                changes = changes
            });
        }

        private static object ConfigureHumanoid(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            var changes = new List<string>();

            // Animation type
            string animationType = @params["animationType"]?.ToString();
            if (!string.IsNullOrEmpty(animationType))
            {
                if (Enum.TryParse<ModelImporterAnimationType>(animationType, true, out var animType))
                {
                    importer.animationType = animType;
                    changes.Add($"animationType={animType}");
                }
            }
            else
            {
                // Default to Human for this action
                importer.animationType = ModelImporterAnimationType.Human;
                changes.Add("animationType=Human");
            }

            // Avatar definition
            string avatarDef = @params["avatarDefinition"]?.ToString();
            if (!string.IsNullOrEmpty(avatarDef))
            {
                if (Enum.TryParse<ModelImporterAvatarSetup>(avatarDef, true, out var setup))
                {
                    importer.avatarSetup = setup;
                    changes.Add($"avatarSetup={setup}");

                    // If copying from other, set source avatar
                    if (setup == ModelImporterAvatarSetup.CopyFromOther)
                    {
                        string sourceAvatarPath = @params["sourceAvatarPath"]?.ToString();
                        if (!string.IsNullOrEmpty(sourceAvatarPath))
                        {
                            sourceAvatarPath = AssetPathUtility.SanitizeAssetPath(sourceAvatarPath);
                            Avatar sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(sourceAvatarPath);
                            if (sourceAvatar != null)
                            {
                                importer.sourceAvatar = sourceAvatar;
                                changes.Add($"sourceAvatar={sourceAvatarPath}");
                            }
                            else
                            {
                                return new ErrorResponse($"Source avatar not found: {sourceAvatarPath}");
                            }
                        }
                    }
                }
            }

            // Apply bone mapping if provided
            JObject boneMapping = @params["boneMapping"] as JObject;
            if (boneMapping != null && boneMapping.HasValues)
            {
                var humanDescription = importer.humanDescription;
                var humanBones = new List<HumanBone>(humanDescription.human);

                foreach (var prop in boneMapping.Properties())
                {
                    string humanName = prop.Name;
                    string boneName = prop.Value.ToString();

                    int existingIndex = humanBones.FindIndex(b => b.humanName == humanName);
                    var newBone = new HumanBone
                    {
                        humanName = humanName,
                        boneName = boneName,
                        limit = new HumanLimit { useDefaultValues = true }
                    };

                    if (existingIndex >= 0)
                    {
                        humanBones[existingIndex] = newBone;
                    }
                    else
                    {
                        humanBones.Add(newBone);
                    }
                }

                humanDescription.human = humanBones.ToArray();
                importer.humanDescription = humanDescription;
                changes.Add($"boneMapping={boneMapping.Count} bones");
            }

            importer.SaveAndReimport();

            return new SuccessResponse($"Configured humanoid with {changes.Count} changes", new
            {
                assetPath = assetPath,
                changes = changes,
                animationType = importer.animationType.ToString(),
                avatarSetup = importer.avatarSetup.ToString()
            });
        }

        private static object ValidateRig(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            // Load the model to inspect bones
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelPrefab == null)
            {
                return new ErrorResponse($"Could not load model: {assetPath}");
            }

            // Get all transforms as potential bones
            var allBones = modelPrefab.GetComponentsInChildren<Transform>()
                .Select(t => t.name)
                .ToList();

            // Check for skinned mesh renderers
            var skinnedMeshes = modelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            var hasSkinnedMesh = skinnedMeshes.Length > 0;

            // Get bone info from skinned mesh
            var boundBones = new List<string>();
            foreach (var smr in skinnedMeshes)
            {
                if (smr.bones != null)
                {
                    boundBones.AddRange(smr.bones.Where(b => b != null).Select(b => b.name));
                }
            }
            boundBones = boundBones.Distinct().ToList();

            // Check if current animation type is humanoid
            bool isHumanoid = importer.animationType == ModelImporterAnimationType.Human;
            bool canBeHumanoid = hasSkinnedMesh && boundBones.Count >= 15; // Minimum bones for humanoid

            // Required Unity humanoid bone keywords
            var requiredKeywords = new[]
            {
                new[] { "hip", "pelvis" },
                new[] { "spine" },
                new[] { "head" },
                new[] { "arm", "upperarm" },
                new[] { "forearm", "lowerarm" },
                new[] { "hand", "wrist" },
                new[] { "leg", "thigh", "upleg" },
                new[] { "calf", "shin", "lowerleg" },
                new[] { "foot", "ankle" }
            };

            var foundKeywords = new List<string>();
            var missingKeywords = new List<string>();

            foreach (var keywords in requiredKeywords)
            {
                bool found = allBones.Any(bone =>
                    keywords.Any(kw => bone.ToLowerInvariant().Contains(kw)));

                if (found)
                {
                    foundKeywords.Add(string.Join("/", keywords));
                }
                else
                {
                    missingKeywords.Add(string.Join("/", keywords));
                }
            }

            bool humanoidCompatible = missingKeywords.Count <= 2; // Allow some flexibility

            return new SuccessResponse("Rig validation complete", new
            {
                assetPath = assetPath,
                currentAnimationType = importer.animationType.ToString(),
                totalTransforms = allBones.Count,
                hasSkinnedMesh = hasSkinnedMesh,
                skinnedMeshCount = skinnedMeshes.Length,
                boundBoneCount = boundBones.Count,
                canBeHumanoid = canBeHumanoid,
                humanoidCompatible = humanoidCompatible,
                foundBoneTypes = foundKeywords,
                missingBoneTypes = missingKeywords,
                allBoneNames = allBones
            });
        }

        private static object GetBoneMapping(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                return new SuccessResponse("Model is not configured as Humanoid", new
                {
                    assetPath = assetPath,
                    isHumanoid = false,
                    message = "Use configure_humanoid action first"
                });
            }

            var humanDescription = importer.humanDescription;
            var mapping = new Dictionary<string, string>();

            foreach (var bone in humanDescription.human)
            {
                mapping[bone.humanName] = bone.boneName;
            }

            // Also get skeleton bones
            var skeletonBones = humanDescription.skeleton
                .Select(s => new { name = s.name, position = s.position, rotation = s.rotation, scale = s.scale })
                .ToList();

            return new SuccessResponse("Retrieved bone mapping", new
            {
                assetPath = assetPath,
                isHumanoid = true,
                humanBoneCount = mapping.Count,
                mapping = mapping,
                skeletonBoneCount = skeletonBones.Count
            });
        }
    }
}
