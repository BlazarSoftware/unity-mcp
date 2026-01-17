using MCPForUnity.Editor.Setup;
using MCPForUnity.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.MenuItems
{
    public static class MCPForUnityMenu
    {
        [MenuItem("Window/Cranky MCP/Toggle MCP Window %#m", priority = 1)]
        public static void ToggleMCPWindow()
        {
            if (MCPForUnityEditorWindow.HasAnyOpenWindow())
            {
                MCPForUnityEditorWindow.CloseAllOpenWindows();
            }
            else
            {
                MCPForUnityEditorWindow.ShowWindow();
            }
        }

        [MenuItem("Window/Cranky MCP/Local Setup Window", priority = 2)]
        public static void ShowSetupWindow()
        {
            SetupWindowService.ShowSetupWindow();
        }


        [MenuItem("Window/Cranky MCP/Edit EditorPrefs", priority = 3)]
        public static void ShowEditorPrefsWindow()
        {
            EditorPrefsWindow.ShowWindow();
        }
    }
}
