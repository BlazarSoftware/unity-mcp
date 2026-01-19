using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Addressables system.
    /// Note: Requires Addressables package to be installed.
    /// </summary>
    [McpForUnityTool("manage_addressables", AutoRegister = false, Description = "Manage Addressables: check package, set addressable assets. Requires Addressables package.")]
    public static class ManageAddressables
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: check_package, set_addressable, get_groups");
            }

            try
            {
                switch (action)
                {
                    case "check_package":
                        return CheckAddressablesPackage();

                    case "set_addressable":
                        return SetAddressable(@params);

                    case "get_groups":
                        return GetAddressableGroups();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: check_package, set_addressable, get_groups");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object CheckAddressablesPackage()
        {
            // Check if Addressables package is installed
            bool hasAddressables = false;

            try
            {
                var addressablesType = Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettings, Unity.Addressables.Editor");
                hasAddressables = addressablesType != null;
            }
            catch { }

            return new SuccessResponse("Checked Addressables package status", new
            {
                hasAddressables = hasAddressables,
                recommendation = !hasAddressables
                    ? "Install Addressables package via Package Manager"
                    : "Addressables package is installed"
            });
        }

        private static object SetAddressable(JObject @params)
        {
            return new ErrorResponse("Addressables package is not installed or feature not yet implemented. Install Addressables via Package Manager.");
        }

        private static object GetAddressableGroups()
        {
            return new ErrorResponse("Addressables package is not installed or feature not yet implemented. Install Addressables via Package Manager.");
        }
    }
}
