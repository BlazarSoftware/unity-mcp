using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity script compilation - trigger recompile, get compilation status, assembly info.
    /// </summary>
    [McpForUnityTool("manage_compilation", AutoRegister = false, Description = "Control script compilation: trigger recompile, get status, wait for compilation.")]
    public static class ManageCompilation
    {
        private static TaskCompletionSource<bool> _compilationWaiter;

        static ManageCompilation()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: trigger_recompile, get_status, get_assemblies, wait_for_compilation");
            }

            try
            {
                switch (action)
                {
                    case "trigger_recompile":
                    case "recompile":
                        return TriggerRecompile(@params);

                    case "get_status":
                        return GetCompilationStatus();

                    case "get_assemblies":
                        return GetAssemblies();

                    case "wait_for_compilation":
                        return WaitForCompilation();

                    case "get_errors":
                        return GetCompilationErrors();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: trigger_recompile, get_status, get_assemblies, wait_for_compilation, get_errors");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object TriggerRecompile(JObject @params)
        {
            bool force = @params["force"]?.ToObject<bool>() ?? false;

            if (EditorApplication.isCompiling)
            {
                return new ErrorResponse("Already compiling");
            }

            // Request script reload which triggers recompilation
            if (force)
            {
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            }
            else
            {
                CompilationPipeline.RequestScriptCompilation();
            }

            return new SuccessResponse("Recompilation triggered", new
            {
                isCompiling = EditorApplication.isCompiling,
                force = force
            });
        }

        private static object GetCompilationStatus()
        {
            bool isCompiling = EditorApplication.isCompiling;

            var status = new Dictionary<string, object>
            {
                ["isCompiling"] = isCompiling,
                ["isUpdating"] = EditorApplication.isUpdating
            };

            // Get compiler messages if available
            if (isCompiling)
            {
                status["message"] = "Compilation in progress";
            }

            return new SuccessResponse("Retrieved compilation status", status);
        }

        private static object GetAssemblies()
        {
            var assemblies = CompilationPipeline.GetAssemblies();
            var assemblyInfo = assemblies.Select(a => new
            {
                name = a.name,
                outputPath = a.outputPath,
                flags = a.flags.ToString(),
                defines = a.defines,
                sourceFiles = a.sourceFiles?.Length ?? 0,
                assemblyReferences = a.assemblyReferences?.Select(ar => ar.name).ToArray() ?? new string[0],
                compiledAssemblyReferences = a.compiledAssemblyReferences ?? new string[0]
            }).ToList();

            return new SuccessResponse($"Retrieved {assemblies.Length} assemblies", new
            {
                count = assemblies.Length,
                assemblies = assemblyInfo
            });
        }

        private static object WaitForCompilation()
        {
            if (!EditorApplication.isCompiling)
            {
                return new SuccessResponse("Not compiling", new
                {
                    isCompiling = false,
                    waited = false
                });
            }

            // Create a waiter
            if (_compilationWaiter == null || _compilationWaiter.Task.IsCompleted)
            {
                _compilationWaiter = new TaskCompletionSource<bool>();
            }

            return new SuccessResponse("Compilation in progress, use async pattern to wait", new
            {
                isCompiling = true,
                message = "Use polling with get_status to check completion"
            });
        }

        private static object GetCompilationErrors()
        {
            // Unity doesn't provide direct API for getting compilation errors in real-time
            // They're shown in the Console, which we can read via ReadConsole tool
            return new SuccessResponse("Use read_console tool to get compilation errors from the console", new
            {
                message = "Compilation errors appear in Unity Console",
                recommendation = "Use read_console with types=['error'] to retrieve compilation errors"
            });
        }

        // Event handlers
        private static void OnCompilationStarted(object obj)
        {
            McpLog.Info("[ManageCompilation] Compilation started");
        }

        private static void OnCompilationFinished(object obj)
        {
            McpLog.Info("[ManageCompilation] Compilation finished");
            _compilationWaiter?.TrySetResult(true);
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            int errors = messages.Count(m => m.type == CompilerMessageType.Error);
            int warnings = messages.Count(m => m.type == CompilerMessageType.Warning);

            if (errors > 0 || warnings > 0)
            {
                McpLog.Info($"[ManageCompilation] Assembly {assemblyPath} compiled with {errors} errors, {warnings} warnings");
            }
        }
    }
}
