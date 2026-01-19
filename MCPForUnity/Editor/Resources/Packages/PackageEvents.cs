using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace MCPForUnity.Editor.Resources.Packages
{
    /// <summary>
    /// Provides information about Package Manager operations and events.
    /// </summary>
    [McpForUnityResource("package_events")]
    public static class PackageEvents
    {
        private static ListRequest _listRequest;
        private static AddRequest _addRequest;
        private static RemoveRequest _removeRequest;

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                action = "get_status"; // Default action
            }

            try
            {
                switch (action)
                {
                    case "get_status":
                    case "get_package_status":
                        return GetPackageStatus();

                    case "get_operations":
                        return GetOngoingOperations();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: get_status, get_operations");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object GetPackageStatus()
        {
            // Start a list request if not already running
            if (_listRequest == null || _listRequest.Status == StatusCode.Success || _listRequest.Status == StatusCode.Failure)
            {
                _listRequest = Client.List(true);
            }

            // Check status
            bool isListing = _listRequest.Status == StatusCode.InProgress;

            if (_listRequest.Status == StatusCode.Success)
            {
                var packages = _listRequest.Result.Select(p => new
                {
                    name = p.name,
                    version = p.version,
                    isDirectDependency = p.isDirectDependency,
                    source = p.source.ToString()
                }).ToList();

                return new SuccessResponse("Retrieved package status", new
                {
                    isListing = false,
                    packageCount = packages.Count,
                    packages = packages
                });
            }
            else if (_listRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to list packages: {_listRequest.Error?.message}");
            }
            else
            {
                return new SuccessResponse("Package listing in progress", new
                {
                    isListing = true,
                    status = _listRequest.Status.ToString()
                });
            }
        }

        private static object GetOngoingOperations()
        {
            var operations = new System.Collections.Generic.List<object>();

            if (_listRequest != null && _listRequest.Status == StatusCode.InProgress)
            {
                operations.Add(new
                {
                    type = "list",
                    status = _listRequest.Status.ToString()
                });
            }

            if (_addRequest != null && _addRequest.Status == StatusCode.InProgress)
            {
                operations.Add(new
                {
                    type = "add",
                    status = _addRequest.Status.ToString()
                });
            }

            if (_removeRequest != null && _removeRequest.Status == StatusCode.InProgress)
            {
                operations.Add(new
                {
                    type = "remove",
                    status = _removeRequest.Status.ToString()
                });
            }

            return new SuccessResponse($"Found {operations.Count} ongoing operations", new
            {
                count = operations.Count,
                operations = operations,
                hasOngoingOperations = operations.Count > 0
            });
        }

        // Event handlers can be registered here
        static PackageEvents()
        {
            Events.registeredPackages += OnPackagesRegistered;
        }

        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            McpLog.Info($"[PackageEvents] Packages registered: {args.added.Count()} added, {args.removed.Count()} removed");
        }
    }
}
