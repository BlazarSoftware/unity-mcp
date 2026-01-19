using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity's undo/redo system.
    /// </summary>
    [McpForUnityTool("manage_history", AutoRegister = false, Description = "Control undo/redo history: undo, redo, create undo groups, get history.")]
    public static class ManageHistory
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: undo, redo, create_group, get_history");
            }

            try
            {
                switch (action)
                {
                    case "undo":
                        return PerformUndo();

                    case "redo":
                        return PerformRedo();

                    case "create_group":
                    case "begin_group":
                        return CreateUndoGroup(@params);

                    case "record_object":
                        return RecordObject(@params);

                    case "get_history":
                    case "get_status":
                        return GetHistoryStatus();

                    case "collapse_group":
                        return CollapseUndoGroup(@params);

                    case "clear_all":
                        return ClearAll();

                    default:
                        return new ErrorResponse($"Unknown action: '{action}'. Valid actions: undo, redo, create_group, record_object, get_history, collapse_group, clear_all");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing action '{action}': {ex.Message}", new { stackTrace = ex.StackTrace });
            }
        }

        private static object PerformUndo()
        {
            Undo.PerformUndo();

            return new SuccessResponse("Undo performed", new
            {
                currentGroup = Undo.GetCurrentGroup()
            });
        }

        private static object PerformRedo()
        {
            Undo.PerformRedo();

            return new SuccessResponse("Redo performed", new
            {
                currentGroup = Undo.GetCurrentGroup()
            });
        }

        private static object CreateUndoGroup(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "MCP Operation";

            int groupIndex = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);

            return new SuccessResponse($"Created undo group: {name}", new
            {
                groupIndex = groupIndex,
                name = name
            });
        }

        private static object RecordObject(JObject @params)
        {
            return new ErrorResponse("record_object requires direct object reference - use from Unity scripts, not MCP");
        }

        private static object GetHistoryStatus()
        {
            return new SuccessResponse("Retrieved undo history status", new
            {
                currentGroup = Undo.GetCurrentGroup(),
                undoRedoPerformed = Undo.undoRedoPerformed != null
            });
        }

        private static object CollapseUndoGroup(JObject @params)
        {
            int groupIndex = @params["groupIndex"]?.ToObject<int>() ?? Undo.GetCurrentGroup();

            Undo.CollapseUndoOperations(groupIndex);

            return new SuccessResponse($"Collapsed undo operations for group {groupIndex}", new
            {
                groupIndex = groupIndex
            });
        }

        private static object ClearAll()
        {
            Undo.ClearAll();

            return new SuccessResponse("Cleared all undo history", new
            {
                currentGroup = Undo.GetCurrentGroup()
            });
        }
    }
}
