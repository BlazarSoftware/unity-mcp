using System;
using System.Collections.Generic;
using System.IO;
using MCPForUnity.Editor.Models;

namespace MCPForUnity.Editor.Clients.Configurators
{
    /// <summary>
    /// Configures the Gemini CLI (~/.gemini/settings.json) MCP settings.
    /// </summary>
    public class GeminiCliConfigurator : JsonFileMcpConfigurator
    {
        public GeminiCliConfigurator() : base(new McpClient
        {
            name = "Gemini CLI",
            windowsConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "settings.json"),
            macConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "settings.json"),
            linuxConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "settings.json"),
        })
        { }

        public override IList<string> GetInstallationSteps() => new List<string>
        {
            "Install Gemini CLI and ensure '~/.gemini/settings.json' exists",
            "Click Configure to add the UnityMCP entry (or manually edit the file above)",
            "Restart your CLI session if needed"
        };
    }
}
