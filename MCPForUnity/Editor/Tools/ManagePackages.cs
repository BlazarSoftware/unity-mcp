using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles Unity Package Manager operations including listing, adding, removing, and searching packages.
    /// </summary>
    [McpForUnityTool("manage_packages", AutoRegister = false, Description = "Install, remove, and list Unity packages via the Package Manager.")]
    public static class ManagePackages
    {
        // Track ongoing requests for async operations
        private static ListRequest _listRequest;
        private static AddRequest _addRequest;
        private static RemoveRequest _removeRequest;
        private static SearchRequest _searchRequest;

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required");
            }

            try
            {
                switch (action)
                {
                    case "ping":
                        return new SuccessResponse("pong", new { tool = "manage_packages" });

                    case "list":
                        return ListPackages(@params);

                    case "add":
                        return AddPackage(@params);

                    case "remove":
                        return RemovePackage(@params);

                    case "search":
                        return SearchPackages(@params);

                    case "get_info":
                        return GetPackageInfo(@params);

                    case "embed":
                        return EmbedPackage(@params);

                    case "resolve":
                        return ResolvePackages();

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: list, add, remove, search, get_info, embed, resolve");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static object ListPackages(JObject @params)
        {
            bool offlineMode = @params["offlineMode"]?.ToObject<bool>() ?? false;
            bool includeIndirect = @params["includeIndirect"]?.ToObject<bool>() ?? false;

            _listRequest = Client.List(offlineMode, includeIndirect);

            // Wait for the request to complete (synchronous wait for editor tools)
            while (!_listRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_listRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to list packages: {_listRequest.Error?.message}", new
                {
                    errorCode = _listRequest.Error?.errorCode.ToString()
                });
            }

            var packages = new List<object>();
            foreach (var package in _listRequest.Result)
            {
                packages.Add(new
                {
                    name = package.name,
                    displayName = package.displayName,
                    version = package.version,
                    description = package.description,
                    category = package.category,
                    source = package.source.ToString(),
                    resolvedPath = package.resolvedPath,
                    isDirectDependency = package.isDirectDependency,
                    packageId = package.packageId
                });
            }

            return new SuccessResponse($"Found {packages.Count} packages", new
            {
                count = packages.Count,
                packages = packages
            });
        }

        private static object AddPackage(JObject @params)
        {
            string packageId = @params["packageId"]?.ToString() ?? @params["name"]?.ToString();
            if (string.IsNullOrEmpty(packageId))
            {
                return new ErrorResponse("packageId or name is required");
            }

            string version = @params["version"]?.ToString();
            string url = @params["url"]?.ToString();

            string identifier;
            if (!string.IsNullOrEmpty(url))
            {
                // Git URL or local path
                identifier = url;
            }
            else if (!string.IsNullOrEmpty(version))
            {
                // Package with specific version
                identifier = $"{packageId}@{version}";
            }
            else
            {
                // Latest version
                identifier = packageId;
            }

            _addRequest = Client.Add(identifier);

            // Wait for the request to complete
            while (!_addRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_addRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to add package: {_addRequest.Error?.message}", new
                {
                    errorCode = _addRequest.Error?.errorCode.ToString(),
                    packageId = identifier
                });
            }

            var result = _addRequest.Result;
            return new SuccessResponse($"Added package '{result.displayName}' ({result.version})", new
            {
                name = result.name,
                displayName = result.displayName,
                version = result.version,
                source = result.source.ToString(),
                resolvedPath = result.resolvedPath
            });
        }

        private static object RemovePackage(JObject @params)
        {
            string packageName = @params["packageName"]?.ToString() ?? @params["name"]?.ToString();
            if (string.IsNullOrEmpty(packageName))
            {
                return new ErrorResponse("packageName or name is required");
            }

            _removeRequest = Client.Remove(packageName);

            // Wait for the request to complete
            while (!_removeRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_removeRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to remove package: {_removeRequest.Error?.message}", new
                {
                    errorCode = _removeRequest.Error?.errorCode.ToString(),
                    packageName = packageName
                });
            }

            return new SuccessResponse($"Removed package '{packageName}'", new
            {
                packageName = packageName
            });
        }

        private static object SearchPackages(JObject @params)
        {
            string searchText = @params["searchText"]?.ToString() ?? @params["query"]?.ToString();
            bool offlineMode = @params["offlineMode"]?.ToObject<bool>() ?? false;

            if (!string.IsNullOrEmpty(searchText))
            {
                _searchRequest = Client.Search(searchText, offlineMode);
            }
            else
            {
                _searchRequest = Client.SearchAll(offlineMode);
            }

            // Wait for the request to complete
            while (!_searchRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_searchRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to search packages: {_searchRequest.Error?.message}", new
                {
                    errorCode = _searchRequest.Error?.errorCode.ToString()
                });
            }

            var packages = new List<object>();
            foreach (var package in _searchRequest.Result)
            {
                var versions = new List<object>();
                foreach (var ver in package.versions.all)
                {
                    versions.Add(ver);
                }

                packages.Add(new
                {
                    name = package.name,
                    displayName = package.displayName,
                    description = package.description,
                    latestVersion = package.versions.latest,
                    verifiedVersion = package.versions.recommended,
                    allVersions = versions
                });
            }

            return new SuccessResponse($"Found {packages.Count} packages", new
            {
                searchText = searchText,
                count = packages.Count,
                packages = packages
            });
        }

        private static object GetPackageInfo(JObject @params)
        {
            string packageName = @params["packageName"]?.ToString() ?? @params["name"]?.ToString();
            if (string.IsNullOrEmpty(packageName))
            {
                return new ErrorResponse("packageName or name is required");
            }

            // First, list packages to find the one we're looking for
            _listRequest = Client.List(false, true);

            while (!_listRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (_listRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to get package info: {_listRequest.Error?.message}");
            }

            UnityEditor.PackageManager.PackageInfo foundPackage = null;
            foreach (var package in _listRequest.Result)
            {
                if (package.name == packageName || package.displayName == packageName)
                {
                    foundPackage = package;
                    break;
                }
            }

            if (foundPackage == null)
            {
                // Try searching in registry
                _searchRequest = Client.Search(packageName, false);

                while (!_searchRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }

                if (_searchRequest.Status == StatusCode.Success && _searchRequest.Result.Length > 0)
                {
                    var searchResult = _searchRequest.Result.FirstOrDefault(p => p.name == packageName);
                    if (searchResult != null)
                    {
                        return new SuccessResponse($"Found package '{searchResult.displayName}' in registry", new
                        {
                            name = searchResult.name,
                            displayName = searchResult.displayName,
                            description = searchResult.description,
                            latestVersion = searchResult.versions.latest,
                            verifiedVersion = searchResult.versions.recommended,
                            installed = false
                        });
                    }
                }

                return new ErrorResponse($"Package '{packageName}' not found");
            }

            var dependencies = new List<object>();
            if (foundPackage.dependencies != null)
            {
                foreach (var dep in foundPackage.dependencies)
                {
                    dependencies.Add(new
                    {
                        name = dep.name,
                        version = dep.version
                    });
                }
            }

            var keywords = foundPackage.keywords?.ToList() ?? new List<string>();

            return new SuccessResponse($"Retrieved info for '{foundPackage.displayName}'", new
            {
                name = foundPackage.name,
                displayName = foundPackage.displayName,
                version = foundPackage.version,
                description = foundPackage.description,
                category = foundPackage.category,
                source = foundPackage.source.ToString(),
                resolvedPath = foundPackage.resolvedPath,
                isDirectDependency = foundPackage.isDirectDependency,
                packageId = foundPackage.packageId,
                author = foundPackage.author?.name,
                documentationUrl = foundPackage.documentationUrl,
                changelogUrl = foundPackage.changelogUrl,
                licensesUrl = foundPackage.licensesUrl,
                keywords = keywords,
                dependencies = dependencies,
                installed = true
            });
        }

        private static object EmbedPackage(JObject @params)
        {
            string packageName = @params["packageName"]?.ToString() ?? @params["name"]?.ToString();
            if (string.IsNullOrEmpty(packageName))
            {
                return new ErrorResponse("packageName or name is required");
            }

            var embedRequest = Client.Embed(packageName);

            while (!embedRequest.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            if (embedRequest.Status == StatusCode.Failure)
            {
                return new ErrorResponse($"Failed to embed package: {embedRequest.Error?.message}", new
                {
                    errorCode = embedRequest.Error?.errorCode.ToString(),
                    packageName = packageName
                });
            }

            var result = embedRequest.Result;
            return new SuccessResponse($"Embedded package '{result.displayName}'", new
            {
                name = result.name,
                displayName = result.displayName,
                version = result.version,
                resolvedPath = result.resolvedPath,
                source = result.source.ToString()
            });
        }

        private static object ResolvePackages()
        {
            Client.Resolve();

            return new SuccessResponse("Started package resolution. Check console for progress.", new
            {
                status = "resolving"
            });
        }
    }
}
