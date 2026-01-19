using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Profiler - start/stop profiling, capture data, get memory snapshots.
    /// </summary>
    [McpForUnityTool("manage_profiler", AutoRegister = false, Description = "Control Unity Profiler: start/stop profiling, get profiler data, memory snapshots.")]
    public static class ManageProfiler
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: start, stop, get_data, get_memory, clear, get_state");
            }

            try
            {
                switch (action)
                {
                    case "start":
                    case "start_profiling":
                        return StartProfiling(@params);

                    case "stop":
                    case "stop_profiling":
                        return StopProfiling();

                    case "get_data":
                    case "get_profiler_data":
                        return GetProfilerData(@params);

                    case "get_memory":
                    case "get_memory_snapshot":
                        return GetMemorySnapshot();

                    case "clear":
                        return ClearProfilerData();

                    case "get_state":
                        return GetProfilerState();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: start, stop, get_data, get_memory, clear, get_state");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object StartProfiling(JObject @params)
        {
            bool deepProfiling = @params["deepProfiling"]?.ToObject<bool>() ?? false;
            bool profileEditor = @params["profileEditor"]?.ToObject<bool>() ?? false;

            if (deepProfiling)
            {
                ProfilerDriver.deepProfiling = true;
            }

            ProfilerDriver.profileEditor = profileEditor;
            ProfilerDriver.enabled = true;

            return new SuccessResponse("Profiling started", new
            {
                enabled = ProfilerDriver.enabled,
                deepProfiling = ProfilerDriver.deepProfiling,
                profileEditor = ProfilerDriver.profileEditor
            });
        }

        private static object StopProfiling()
        {
            ProfilerDriver.enabled = false;
            ProfilerDriver.deepProfiling = false;

            return new SuccessResponse("Profiling stopped", new
            {
                enabled = ProfilerDriver.enabled
            });
        }

        private static object GetProfilerData(JObject @params)
        {
            if (!ProfilerDriver.enabled)
            {
                return new ErrorResponse("Profiler is not enabled. Start profiling first.");
            }

            int frameIndex = @params["frameIndex"]?.ToObject<int>() ?? ProfilerDriver.lastFrameIndex;
            bool includeRenderingStats = @params["includeRenderingStats"]?.ToObject<bool>() ?? true;
            bool includeMemoryStats = @params["includeMemoryStats"]?.ToObject<bool>() ?? true;

            // Get available frames
            int firstFrame = ProfilerDriver.firstFrameIndex;
            int lastFrame = ProfilerDriver.lastFrameIndex;

            if (frameIndex < firstFrame || frameIndex > lastFrame)
            {
                frameIndex = lastFrame;
            }

            var data = new Dictionary<string, object>();

            // Frame data
            data["frameIndex"] = frameIndex;
            data["firstFrameIndex"] = firstFrame;
            data["lastFrameIndex"] = lastFrame;
            data["frameCount"] = lastFrame - firstFrame + 1;

            // Get profiler areas data
            var areas = new List<string>();
            foreach (ProfilerArea area in Enum.GetValues(typeof(ProfilerArea)))
            {
                areas.Add(area.ToString());
            }
            data["availableAreas"] = areas;

            if (includeMemoryStats)
            {
                data["totalReservedMemory"] = Profiler.GetTotalReservedMemoryLong();
                data["totalAllocatedMemory"] = Profiler.GetTotalAllocatedMemoryLong();
                data["totalUnusedReservedMemory"] = Profiler.GetTotalUnusedReservedMemoryLong();
                data["monoHeapSize"] = Profiler.GetMonoHeapSizeLong();
                data["monoUsedSize"] = Profiler.GetMonoUsedSizeLong();
            }

            if (includeRenderingStats && EditorApplication.isPlaying)
            {
                // Note: Detailed rendering stats require deeper Unity API access
                // For now, include basic info that profiler is capturing this data
                data["renderingStatsNote"] = "Use Unity Profiler window for detailed rendering statistics";
            }

            return new SuccessResponse("Retrieved profiler data", data);
        }

        private static object GetMemorySnapshot()
        {
            var memoryData = new Dictionary<string, object>();

            // Memory stats
            memoryData["totalReservedMemory"] = Profiler.GetTotalReservedMemoryLong();
            memoryData["totalAllocatedMemory"] = Profiler.GetTotalAllocatedMemoryLong();
            memoryData["totalUnusedReservedMemory"] = Profiler.GetTotalUnusedReservedMemoryLong();
            memoryData["monoHeapSize"] = Profiler.GetMonoHeapSizeLong();
            memoryData["monoUsedSize"] = Profiler.GetMonoUsedSizeLong();
            memoryData["tempAllocatorSize"] = Profiler.GetTempAllocatorSize();

            // GC allocations
            memoryData["gcAllocatedInFrame"] = Profiler.GetAllocatedMemoryForGraphicsDriver();

            // Texture memory
            memoryData["textureMemory"] = Profiler.GetAllocatedMemoryForGraphicsDriver();

            // Mesh memory
            memoryData["meshMemory"] = 0; // Would need to enumerate all meshes

            return new SuccessResponse("Retrieved memory snapshot", memoryData);
        }

        private static object ClearProfilerData()
        {
            ProfilerDriver.ClearAllFrames();

            return new SuccessResponse("Profiler data cleared", new
            {
                frameCount = 0
            });
        }

        private static object GetProfilerState()
        {
            return new SuccessResponse("Retrieved profiler state", new
            {
                enabled = ProfilerDriver.enabled,
                deepProfiling = ProfilerDriver.deepProfiling,
                profileEditor = ProfilerDriver.profileEditor,
                firstFrameIndex = ProfilerDriver.firstFrameIndex,
                lastFrameIndex = ProfilerDriver.lastFrameIndex,
                frameCount = ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1,
                totalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                monoHeapSize = Profiler.GetMonoHeapSizeLong(),
                monoUsedSize = Profiler.GetMonoUsedSizeLong()
            });
        }
    }
}
