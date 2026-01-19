using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity Editor play mode state - enter, exit, pause, resume, and step frames.
    /// </summary>
    [McpForUnityTool("manage_playmode", AutoRegister = false, Description = "Control Unity Editor play mode: enter, exit, pause, resume, step frames.")]
    public static class ManagePlayMode
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: enter, exit, pause, resume, step, get_state, set_timescale");
            }

            try
            {
                switch (action)
                {
                    case "enter":
                    case "enter_playmode":
                        return EnterPlayMode(@params);

                    case "exit":
                    case "exit_playmode":
                        return ExitPlayMode();

                    case "pause":
                        return PausePlayMode();

                    case "resume":
                    case "unpause":
                        return ResumePlayMode();

                    case "step":
                    case "step_frame":
                        return StepFrame();

                    case "get_state":
                        return GetPlayModeState();

                    case "set_timescale":
                        return SetTimeScale(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: enter, exit, pause, resume, step, get_state, set_timescale");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object EnterPlayMode(JObject @params)
        {
            if (EditorApplication.isPlaying)
            {
                return new ErrorResponse("Already in play mode");
            }

            if (EditorApplication.isCompiling)
            {
                return new ErrorResponse("Cannot enter play mode while compiling");
            }

            // Optional: disable domain reload for faster play mode entry
            bool disableDomainReload = @params["disableDomainReload"]?.ToObject<bool>() ?? false;
            bool disableSceneReload = @params["disableSceneReload"]?.ToObject<bool>() ?? false;

            if (disableDomainReload || disableSceneReload)
            {
                var options = EditorSettings.enterPlayModeOptions;
                if (disableDomainReload)
                {
                    options |= EnterPlayModeOptions.DisableDomainReload;
                }
                if (disableSceneReload)
                {
                    options |= EnterPlayModeOptions.DisableSceneReload;
                }
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = options;
            }

            EditorApplication.isPlaying = true;

            return new SuccessResponse("Entering play mode", new
            {
                isPlaying = EditorApplication.isPlaying,
                isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode,
                disableDomainReload = disableDomainReload,
                disableSceneReload = disableSceneReload
            });
        }

        private static object ExitPlayMode()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return new ErrorResponse("Not in play mode");
            }

            EditorApplication.isPlaying = false;

            return new SuccessResponse("Exiting play mode", new
            {
                isPlaying = EditorApplication.isPlaying,
                isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode
            });
        }

        private static object PausePlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                return new ErrorResponse("Not in play mode");
            }

            EditorApplication.isPaused = true;

            return new SuccessResponse("Play mode paused", new
            {
                isPaused = EditorApplication.isPaused,
                frameCount = Time.frameCount,
                time = Time.time
            });
        }

        private static object ResumePlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                return new ErrorResponse("Not in play mode");
            }

            if (!EditorApplication.isPaused)
            {
                return new SuccessResponse("Play mode already running", new
                {
                    isPaused = false
                });
            }

            EditorApplication.isPaused = false;

            return new SuccessResponse("Play mode resumed", new
            {
                isPaused = EditorApplication.isPaused,
                frameCount = Time.frameCount,
                time = Time.time
            });
        }

        private static object StepFrame()
        {
            if (!EditorApplication.isPlaying)
            {
                return new ErrorResponse("Not in play mode");
            }

            if (!EditorApplication.isPaused)
            {
                return new ErrorResponse("Must be paused to step frames");
            }

            EditorApplication.Step();

            return new SuccessResponse("Stepped one frame", new
            {
                frameCount = Time.frameCount,
                time = Time.time,
                isPaused = EditorApplication.isPaused
            });
        }

        private static object GetPlayModeState()
        {
            return new SuccessResponse("Retrieved play mode state", new
            {
                isPlaying = EditorApplication.isPlaying,
                isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode,
                isPaused = EditorApplication.isPaused,
                isCompiling = EditorApplication.isCompiling,
                frameCount = EditorApplication.isPlaying ? Time.frameCount : 0,
                time = EditorApplication.isPlaying ? Time.time : 0f,
                timeScale = Time.timeScale,
                realtimeSinceStartup = Time.realtimeSinceStartup,
                enterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled,
                enterPlayModeOptions = EditorSettings.enterPlayModeOptions.ToString()
            });
        }

        private static object SetTimeScale(JObject @params)
        {
            if (!@params.ContainsKey("timeScale"))
            {
                return new ErrorResponse("timeScale parameter is required");
            }

            float timeScale = @params["timeScale"].ToObject<float>();
            if (timeScale < 0)
            {
                return new ErrorResponse("timeScale must be >= 0");
            }

            Time.timeScale = timeScale;

            return new SuccessResponse($"Time scale set to {timeScale}", new
            {
                timeScale = Time.timeScale,
                isPlaying = EditorApplication.isPlaying
            });
        }
    }
}
