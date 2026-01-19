using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Input System (new Input System package).
    /// Note: Requires Input System package to be installed.
    /// </summary>
    [McpForUnityTool("manage_input", AutoRegister = false, Description = "Manage Input System: create input actions, configure bindings. Requires Input System package.")]
    public static class ManageInput
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: check_package, create_actions_asset, get_input_settings");
            }

            try
            {
                switch (action)
                {
                    case "check_package":
                        return CheckInputSystemPackage();

                    case "create_actions_asset":
                        return CreateInputActionsAsset(@params);

                    case "get_input_settings":
                        return GetInputSettings();

                    case "set_input_settings":
                        return SetInputSettings(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: check_package, create_actions_asset, get_input_settings, set_input_settings");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object CheckInputSystemPackage()
        {
            // Check if Input System package is installed
            bool hasNewInputSystem = false;
            bool hasLegacyInputManager = false;

            try
            {
                // Check for new Input System
                var inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
                hasNewInputSystem = inputSystemType != null;

                // Check for legacy Input
                var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
                hasLegacyInputManager = inputType != null;
            }
            catch { }

            return new SuccessResponse("Checked Input System package status", new
            {
                hasNewInputSystem = hasNewInputSystem,
                hasLegacyInputManager = hasLegacyInputManager,
                recommendation = !hasNewInputSystem
                    ? "Install Input System package via Package Manager"
                    : "Input System package is installed"
            });
        }

        private static object CreateInputActionsAsset(JObject @params)
        {
            // This requires the Input System package
            var inputActionsType = Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
            if (inputActionsType == null)
            {
                return new ErrorResponse("Input System package is not installed. Install it via Package Manager.");
            }

            string savePath = @params["savePath"]?.ToString();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = "Assets/InputActions.inputactions";
            }

            if (!savePath.StartsWith("Assets/"))
            {
                savePath = "Assets/" + savePath;
            }

            if (!savePath.EndsWith(".inputactions"))
            {
                savePath += ".inputactions";
            }

            // Create via reflection since we don't have hard dependency
            try
            {
                var asset = ScriptableObject.CreateInstance(inputActionsType);
                AssetDatabase.CreateAsset(asset, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return new SuccessResponse($"Created Input Actions asset at {savePath}", new
                {
                    path = savePath
                });
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to create Input Actions asset: {ex.Message}");
            }
        }

        private static object GetInputSettings()
        {
            return new SuccessResponse("Retrieved input settings", new
            {
                touchSupport = Input.touchSupported,
                multiTouchEnabled = Input.multiTouchEnabled,
                simulateMouseWithTouches = Input.simulateMouseWithTouches,
                deviceOrientation = Input.deviceOrientation.ToString(),
                compensateSensors = Input.compensateSensors,
                gyroUpdateInterval = Input.gyro.updateInterval
            });
        }

        private static object SetInputSettings(JObject @params)
        {
            var changes = new System.Collections.Generic.List<string>();

            if (@params["multiTouchEnabled"] != null)
            {
                Input.multiTouchEnabled = @params["multiTouchEnabled"].ToObject<bool>();
                changes.Add($"multiTouchEnabled={Input.multiTouchEnabled}");
            }

            if (@params["simulateMouseWithTouches"] != null)
            {
                Input.simulateMouseWithTouches = @params["simulateMouseWithTouches"].ToObject<bool>();
                changes.Add($"simulateMouseWithTouches={Input.simulateMouseWithTouches}");
            }

            if (@params["compensateSensors"] != null)
            {
                Input.compensateSensors = @params["compensateSensors"].ToObject<bool>();
                changes.Add($"compensateSensors={Input.compensateSensors}");
            }

            if (changes.Count == 0)
            {
                return new SuccessResponse("No input settings changed");
            }

            return new SuccessResponse($"Updated input settings: {string.Join(", ", changes)}", new
            {
                changes = changes
            });
        }
    }
}
