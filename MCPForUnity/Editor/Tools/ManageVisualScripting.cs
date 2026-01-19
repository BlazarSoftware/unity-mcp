using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Visual Scripting (formerly Bolt).
    /// Note: Requires Visual Scripting package to be installed.
    /// </summary>
    [McpForUnityTool("manage_visual_scripting", AutoRegister = false, Description = "Manage Visual Scripting: check package status. Requires Visual Scripting package.")]
    public static class ManageVisualScripting
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: check_package");
            }

            try
            {
                switch (action)
                {
                    case "check_package":
                        return CheckVisualScriptingPackage();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: check_package");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object CheckVisualScriptingPackage()
        {
            // Check if Visual Scripting package is installed
            bool hasVisualScripting = false;

            try
            {
                var vsType = Type.GetType("Unity.VisualScripting.ScriptGraphAsset, Unity.VisualScripting.Core");
                hasVisualScripting = vsType != null;
            }
            catch { }

            return new SuccessResponse("Checked Visual Scripting package status", new
            {
                hasVisualScripting = hasVisualScripting,
                recommendation = !hasVisualScripting
                    ? "Install Visual Scripting package via Package Manager"
                    : "Visual Scripting package is installed"
            });
        }
    }
}
