using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Git version control operations.
    /// </summary>
    [McpForUnityTool("manage_version_control", AutoRegister = false, Description = "Git version control: get status, stage files, commit, get diff, branch info.")]
    public static class ManageVersionControl
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: get_status, stage, commit, get_diff, get_branch, is_git_repo");
            }

            try
            {
                switch (action)
                {
                    case "is_git_repo":
                        return IsGitRepository();

                    case "get_status":
                        return GetGitStatus();

                    case "stage":
                    case "add":
                        return StageFiles(@params);

                    case "commit":
                        return Commit(@params);

                    case "get_diff":
                        return GetDiff(@params);

                    case "get_branch":
                        return GetCurrentBranch();

                    case "get_log":
                        return GetLog(@params);

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: is_git_repo, get_status, stage, commit, get_diff, get_branch, get_log");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object IsGitRepository()
        {
            string gitDir = Path.Combine(UnityEngine.Application.dataPath, "..", ".git");
            bool isRepo = Directory.Exists(gitDir);

            return new SuccessResponse(isRepo ? "This is a Git repository" : "Not a Git repository", new
            {
                isGitRepo = isRepo,
                gitPath = isRepo ? gitDir : null
            });
        }

        private static object GetGitStatus()
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            string output = ExecuteGitCommand("status --porcelain");

            return new SuccessResponse("Retrieved Git status", new
            {
                output = output,
                hasChanges = !string.IsNullOrWhiteSpace(output)
            });
        }

        private static object StageFiles(JObject @params)
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            var files = @params["files"] as JArray;
            string filesArg;

            if (files != null && files.Count > 0)
            {
                filesArg = string.Join(" ", files);
            }
            else
            {
                filesArg = "."; // Stage all
            }

            string output = ExecuteGitCommand($"add {filesArg}");

            return new SuccessResponse($"Staged files: {filesArg}", new
            {
                files = filesArg,
                output = output
            });
        }

        private static object Commit(JObject @params)
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            string message = @params["message"]?.ToString();
            if (string.IsNullOrEmpty(message))
            {
                return new ErrorResponse("commit message is required");
            }

            // Escape the message
            message = message.Replace("\"", "\\\"");

            string output = ExecuteGitCommand($"commit -m \"{message}\"");

            bool success = !output.Contains("nothing to commit");

            if (success)
            {
                return new SuccessResponse($"Committed: {message}", new
                {
                    message = message,
                    output = output
                });
            }
            else
            {
                return new ErrorResponse("Nothing to commit or commit failed", new
                {
                    output = output
                });
            }
        }

        private static object GetDiff(JObject @params)
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            string file = @params["file"]?.ToString();
            bool cached = @params["cached"]?.ToObject<bool>() ?? false;

            string command = "diff";
            if (cached)
            {
                command += " --cached";
            }
            if (!string.IsNullOrEmpty(file))
            {
                command += $" {file}";
            }

            string output = ExecuteGitCommand(command);

            return new SuccessResponse("Retrieved diff", new
            {
                diff = output,
                hasChanges = !string.IsNullOrWhiteSpace(output),
                cached = cached
            });
        }

        private static object GetCurrentBranch()
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            string branch = ExecuteGitCommand("branch --show-current").Trim();

            return new SuccessResponse("Retrieved current branch", new
            {
                branch = branch
            });
        }

        private static object GetLog(JObject @params)
        {
            if (!IsGitAvailable())
            {
                return new ErrorResponse("Git is not available or not installed");
            }

            int count = @params["count"]?.ToObject<int>() ?? 10;
            string format = @params["format"]?.ToString() ?? "oneline";

            string output = ExecuteGitCommand($"log -{count} --{format}");

            return new SuccessResponse($"Retrieved last {count} commits", new
            {
                count = count,
                log = output
            });
        }

        private static bool IsGitAvailable()
        {
            try
            {
                ExecuteGitCommand("--version");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ExecuteGitCommand(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.Combine(UnityEngine.Application.dataPath, "..")
            };

            using (Process process = Process.Start(startInfo))
            {
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0 && error.Length > 0)
                {
                    throw new Exception(error.ToString());
                }

                return output.ToString();
            }
        }
    }
}
