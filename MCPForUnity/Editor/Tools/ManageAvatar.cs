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
    /// Manages Unity humanoid avatar configuration and bone mapping.
    /// </summary>
    [McpForUnityTool("manage_avatar", AutoRegister = false, Description = "Configure humanoid avatar bone mappings for imported models.")]
    public static class ManageAvatar
    {
        // Unity humanoid required bones
        private static readonly string[] RequiredBones = new[]
        {
            "Hips", "Spine", "Head",
            "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
            "RightUpperLeg", "RightLowerLeg", "RightFoot"
        };

        // Unity humanoid optional bones
        private static readonly string[] OptionalBones = new[]
        {
            "Chest", "UpperChest", "Neck", "Jaw",
            "LeftEye", "RightEye",
            "LeftShoulder", "RightShoulder",
            "LeftToes", "RightToes",
            "Left Thumb Proximal", "Left Thumb Intermediate", "Left Thumb Distal",
            "Left Index Proximal", "Left Index Intermediate", "Left Index Distal",
            "Left Middle Proximal", "Left Middle Intermediate", "Left Middle Distal",
            "Left Ring Proximal", "Left Ring Intermediate", "Left Ring Distal",
            "Left Little Proximal", "Left Little Intermediate", "Left Little Distal",
            "Right Thumb Proximal", "Right Thumb Intermediate", "Right Thumb Distal",
            "Right Index Proximal", "Right Index Intermediate", "Right Index Distal",
            "Right Middle Proximal", "Right Middle Intermediate", "Right Middle Distal",
            "Right Ring Proximal", "Right Ring Intermediate", "Right Ring Distal",
            "Right Little Proximal", "Right Little Intermediate", "Right Little Distal"
        };

        // Common naming convention mappings
        private static readonly Dictionary<string, Dictionary<string, string>> NamingConventions = new()
        {
            ["mixamo"] = new Dictionary<string, string>
            {
                ["mixamorig:Hips"] = "Hips",
                ["mixamorig:Spine"] = "Spine",
                ["mixamorig:Spine1"] = "Chest",
                ["mixamorig:Spine2"] = "UpperChest",
                ["mixamorig:Neck"] = "Neck",
                ["mixamorig:Head"] = "Head",
                ["mixamorig:LeftShoulder"] = "LeftShoulder",
                ["mixamorig:LeftArm"] = "LeftUpperArm",
                ["mixamorig:LeftForeArm"] = "LeftLowerArm",
                ["mixamorig:LeftHand"] = "LeftHand",
                ["mixamorig:RightShoulder"] = "RightShoulder",
                ["mixamorig:RightArm"] = "RightUpperArm",
                ["mixamorig:RightForeArm"] = "RightLowerArm",
                ["mixamorig:RightHand"] = "RightHand",
                ["mixamorig:LeftUpLeg"] = "LeftUpperLeg",
                ["mixamorig:LeftLeg"] = "LeftLowerLeg",
                ["mixamorig:LeftFoot"] = "LeftFoot",
                ["mixamorig:LeftToeBase"] = "LeftToes",
                ["mixamorig:RightUpLeg"] = "RightUpperLeg",
                ["mixamorig:RightLeg"] = "RightLowerLeg",
                ["mixamorig:RightFoot"] = "RightFoot",
                ["mixamorig:RightToeBase"] = "RightToes"
            },
            ["unreal"] = new Dictionary<string, string>
            {
                ["pelvis"] = "Hips",
                ["spine_01"] = "Spine",
                ["spine_02"] = "Chest",
                ["spine_03"] = "UpperChest",
                ["neck_01"] = "Neck",
                ["head"] = "Head",
                ["clavicle_l"] = "LeftShoulder",
                ["upperarm_l"] = "LeftUpperArm",
                ["lowerarm_l"] = "LeftLowerArm",
                ["hand_l"] = "LeftHand",
                ["clavicle_r"] = "RightShoulder",
                ["upperarm_r"] = "RightUpperArm",
                ["lowerarm_r"] = "RightLowerArm",
                ["hand_r"] = "RightHand",
                ["thigh_l"] = "LeftUpperLeg",
                ["calf_l"] = "LeftLowerLeg",
                ["foot_l"] = "LeftFoot",
                ["ball_l"] = "LeftToes",
                ["thigh_r"] = "RightUpperLeg",
                ["calf_r"] = "RightLowerLeg",
                ["foot_r"] = "RightFoot",
                ["ball_r"] = "RightToes"
            }
        };

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: get_mapping, set_mapping, validate, auto_map, get_required_bones");
            }

            try
            {
                switch (action)
                {
                    case "get_mapping":
                        return GetMapping(@params);

                    case "set_mapping":
                        return SetMapping(@params);

                    case "validate":
                        return ValidateMapping(@params);

                    case "auto_map":
                        return AutoMap(@params);

                    case "get_required_bones":
                        return GetRequiredBones();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: get_mapping, set_mapping, validate, auto_map, get_required_bones");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object GetMapping(JObject @params)
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
                return new ErrorResponse($"Model is not configured as Humanoid. Current type: {importer.animationType}");
            }

            var humanDescription = importer.humanDescription;
            var mapping = new Dictionary<string, string>();

            foreach (var bone in humanDescription.human)
            {
                mapping[bone.humanName] = bone.boneName;
            }

            return new SuccessResponse("Retrieved humanoid bone mapping", new
            {
                assetPath = assetPath,
                boneCount = mapping.Count,
                mapping = mapping
            });
        }

        private static object SetMapping(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            JObject boneMapping = @params["boneMapping"] as JObject;

            if (string.IsNullOrEmpty(assetPath))
            {
                return new ErrorResponse("assetPath is required");
            }

            if (boneMapping == null || !boneMapping.HasValues)
            {
                return new ErrorResponse("boneMapping is required and must be a non-empty object");
            }

            assetPath = AssetPathUtility.SanitizeAssetPath(assetPath);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                return new ErrorResponse($"Not a model asset or not found: {assetPath}");
            }

            // Ensure it's set to humanoid
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
            }

            var humanDescription = importer.humanDescription;
            var humanBones = new List<HumanBone>(humanDescription.human);

            var appliedMappings = new List<string>();

            foreach (var prop in boneMapping.Properties())
            {
                string humanName = prop.Name;
                string boneName = prop.Value.ToString();

                // Find or create the mapping
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

                appliedMappings.Add($"{humanName} -> {boneName}");
            }

            humanDescription.human = humanBones.ToArray();
            importer.humanDescription = humanDescription;
            importer.SaveAndReimport();

            return new SuccessResponse($"Applied {appliedMappings.Count} bone mappings", new
            {
                assetPath = assetPath,
                mappingsApplied = appliedMappings
            });
        }

        private static object ValidateMapping(JObject @params)
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
                    isValid = false,
                    message = "Configure as Humanoid first using manage_model_import"
                });
            }

            var humanDescription = importer.humanDescription;
            var mappedBones = humanDescription.human.Select(b => b.humanName).ToHashSet();

            var missingRequired = RequiredBones.Where(b => !mappedBones.Contains(b)).ToList();
            var mappedOptional = OptionalBones.Where(b => mappedBones.Contains(b)).ToList();

            bool isValid = missingRequired.Count == 0;

            return new SuccessResponse(isValid ? "Humanoid mapping is valid" : "Humanoid mapping has missing required bones", new
            {
                assetPath = assetPath,
                isHumanoid = true,
                isValid = isValid,
                totalMappedBones = mappedBones.Count,
                missingRequiredBones = missingRequired,
                mappedOptionalBones = mappedOptional
            });
        }

        private static object AutoMap(JObject @params)
        {
            string assetPath = @params["assetPath"]?.ToString();
            string conventionHint = @params["namingConvention"]?.ToString()?.ToLowerInvariant() ?? "auto";

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

            // Get all bones from the model
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelPrefab == null)
            {
                return new ErrorResponse($"Could not load model: {assetPath}");
            }

            var allBones = modelPrefab.GetComponentsInChildren<Transform>()
                .Select(t => t.name)
                .ToList();

            // Detect naming convention if auto
            string detectedConvention = conventionHint;
            if (conventionHint == "auto")
            {
                detectedConvention = DetectNamingConvention(allBones);
            }

            // Generate mapping
            var suggestedMapping = new Dictionary<string, string>();
            var unmappedBones = new List<string>();

            // If we have a known convention, use it
            if (NamingConventions.TryGetValue(detectedConvention, out var conventionMap))
            {
                // Reverse the map (model bone -> unity bone)
                var reverseMap = conventionMap.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);

                foreach (var bone in allBones)
                {
                    string boneLower = bone.ToLowerInvariant();
                    if (reverseMap.TryGetValue(boneLower, out string unityBone))
                    {
                        suggestedMapping[unityBone] = bone;
                    }
                }
            }
            else
            {
                // Fuzzy matching for unknown conventions
                suggestedMapping = FuzzyMatchBones(allBones);
            }

            // Find unmapped required bones
            foreach (var required in RequiredBones)
            {
                if (!suggestedMapping.ContainsKey(required))
                {
                    unmappedBones.Add(required);
                }
            }

            return new SuccessResponse($"Auto-mapped {suggestedMapping.Count} bones", new
            {
                assetPath = assetPath,
                detectedConvention = detectedConvention,
                suggestedMapping = suggestedMapping,
                unmappedRequiredBones = unmappedBones,
                allModelBones = allBones,
                message = unmappedBones.Count > 0
                    ? "Some required bones could not be auto-mapped. Please set them manually."
                    : "All required bones mapped. Use set_mapping action to apply."
            });
        }

        private static object GetRequiredBones()
        {
            return new SuccessResponse("Unity humanoid bone requirements", new
            {
                requiredBones = RequiredBones,
                optionalBones = OptionalBones,
                supportedConventions = NamingConventions.Keys.ToArray()
            });
        }

        private static string DetectNamingConvention(List<string> boneNames)
        {
            var boneSet = new HashSet<string>(boneNames.Select(b => b.ToLowerInvariant()));

            // Check Mixamo
            if (boneNames.Any(b => b.StartsWith("mixamorig:", StringComparison.OrdinalIgnoreCase)))
            {
                return "mixamo";
            }

            // Check Unreal
            if (boneSet.Contains("pelvis") && boneSet.Contains("spine_01"))
            {
                return "unreal";
            }

            // Check Unity style
            if (boneSet.Contains("hips") && boneSet.Contains("leftupperarm"))
            {
                return "unity";
            }

            // Check Rigify
            if (boneNames.Any(b => b.StartsWith("DEF-", StringComparison.OrdinalIgnoreCase)))
            {
                return "rigify";
            }

            return "unknown";
        }

        private static Dictionary<string, string> FuzzyMatchBones(List<string> modelBones)
        {
            var result = new Dictionary<string, string>();
            var modelBonesLower = modelBones.Select(b => new { Original = b, Lower = Regex.Replace(b.ToLowerInvariant(), @"[^a-z0-9]", "") }).ToList();

            // Patterns for each Unity bone
            var patterns = new Dictionary<string, string[]>
            {
                ["Hips"] = new[] { "hips", "pelvis", "hip" },
                ["Spine"] = new[] { "spine", "spine1", "spine01" },
                ["Chest"] = new[] { "chest", "spine2", "spine02", "ribcage" },
                ["UpperChest"] = new[] { "upperchest", "spine3", "spine03" },
                ["Neck"] = new[] { "neck", "neck1", "neck01" },
                ["Head"] = new[] { "head" },
                ["LeftShoulder"] = new[] { "leftshoulder", "shoulderl", "clavicleleft", "clavicle_l" },
                ["RightShoulder"] = new[] { "rightshoulder", "shoulderr", "clavicleright", "clavicle_r" },
                ["LeftUpperArm"] = new[] { "leftupperarm", "leftarm", "upperarml", "upperarm_l" },
                ["RightUpperArm"] = new[] { "rightupperarm", "rightarm", "upperarmr", "upperarm_r" },
                ["LeftLowerArm"] = new[] { "leftlowerarm", "leftforearm", "forearm_l", "lowerarm_l" },
                ["RightLowerArm"] = new[] { "rightlowerarm", "rightforearm", "forearm_r", "lowerarm_r" },
                ["LeftHand"] = new[] { "lefthand", "handl", "hand_l", "wristl" },
                ["RightHand"] = new[] { "righthand", "handr", "hand_r", "wristr" },
                ["LeftUpperLeg"] = new[] { "leftupleg", "leftthigh", "thighl", "thigh_l", "upperleg_l" },
                ["RightUpperLeg"] = new[] { "rightupleg", "rightthigh", "thighr", "thigh_r", "upperleg_r" },
                ["LeftLowerLeg"] = new[] { "leftleg", "leftcalf", "calfl", "calf_l", "shin_l", "lowerleg_l" },
                ["RightLowerLeg"] = new[] { "rightleg", "rightcalf", "calfr", "calf_r", "shin_r", "lowerleg_r" },
                ["LeftFoot"] = new[] { "leftfoot", "footl", "foot_l", "anklel" },
                ["RightFoot"] = new[] { "rightfoot", "footr", "foot_r", "ankler" },
                ["LeftToes"] = new[] { "lefttoebase", "lefttoe", "toel", "toe_l", "ball_l" },
                ["RightToes"] = new[] { "righttoebase", "righttoe", "toer", "toe_r", "ball_r" }
            };

            foreach (var kvp in patterns)
            {
                string unityBone = kvp.Key;
                foreach (var pattern in kvp.Value)
                {
                    var match = modelBonesLower.FirstOrDefault(b => b.Lower.Contains(pattern));
                    if (match != null && !result.ContainsKey(unityBone))
                    {
                        result[unityBone] = match.Original;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
