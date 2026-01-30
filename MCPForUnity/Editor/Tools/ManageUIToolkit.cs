using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles CRUD operations for UI Toolkit UXML and USS files.
    /// </summary>
    [McpForUnityTool("manage_ui_toolkit", description: "Create, modify, and inspect UI Toolkit UXML/USS files.")]
    public static class ManageUIToolkit
    {
        public class Parameters
        {
            [ToolParameter("Action to perform: get_uxml_structure, add_visual_element, remove_visual_element, modify_visual_element, add_style_rule, modify_style_rule, get_uss_rules, get_element_styles")]
            public string action { get; set; }

            [ToolParameter("Path to the UXML or USS file (relative to Assets/)", Required = false)]
            public string path { get; set; }

            [ToolParameter("Name of the element to target or create", Required = false)]
            public string elementName { get; set; }

            [ToolParameter("Type of the VisualElement (e.g., Button, Label, VisualElement)", Required = false)]
            public string elementType { get; set; }

            [ToolParameter("Name of parent element to add child to", Required = false)]
            public string parentName { get; set; }

            [ToolParameter("Attributes to set on the element as JSON object", Required = false)]
            public string attributes { get; set; }

            [ToolParameter("USS class names to assign (comma-separated)", Required = false)]
            public string classes { get; set; }

            [ToolParameter("USS selector (e.g., .my-class, #my-id, VisualElement)", Required = false)]
            public string selector { get; set; }

            [ToolParameter("USS properties as JSON object", Required = false)]
            public string styleProperties { get; set; }

            [ToolParameter("Text content for elements like Label or Button", Required = false)]
            public string text { get; set; }

            [ToolParameter("Insert position index within parent (-1 for end)", Required = false)]
            public int? insertIndex { get; set; }
        }

        private static readonly List<string> ValidActions = new List<string>
        {
            "get_uxml_structure",
            "add_visual_element",
            "remove_visual_element",
            "modify_visual_element",
            "add_style_rule",
            "modify_style_rule",
            "get_uss_rules",
            "get_element_styles"
        };

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            string action = @params["action"]?.ToString()?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action parameter is required.");
            }

            if (!ValidActions.Contains(action))
            {
                string validActionsList = string.Join(", ", ValidActions);
                return new ErrorResponse($"Unknown action: '{action}'. Valid actions are: {validActionsList}");
            }

            string path = @params["path"]?.ToString();

            try
            {
                switch (action)
                {
                    case "get_uxml_structure":
                        return GetUxmlStructure(path);

                    case "add_visual_element":
                        return AddVisualElement(@params);

                    case "remove_visual_element":
                        return RemoveVisualElement(@params);

                    case "modify_visual_element":
                        return ModifyVisualElement(@params);

                    case "add_style_rule":
                        return AddStyleRule(@params);

                    case "modify_style_rule":
                        return ModifyStyleRule(@params);

                    case "get_uss_rules":
                        return GetUssRules(path);

                    case "get_element_styles":
                        return GetElementStyles(@params);

                    default:
                        return new ErrorResponse($"Action '{action}' is not implemented.");
                }
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageUIToolkit] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error executing '{action}': {e.Message}");
            }
        }

        #region Path Resolution

        private static bool TryResolveAssetPath(string inputPath, out string fullPath, out string assetPath)
        {
            string assetsRoot = Application.dataPath.Replace('\\', '/');
            string relative = (inputPath ?? string.Empty).Replace('\\', '/').Trim();

            if (relative.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                relative = string.Empty;
            }
            else if (relative.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring("Assets/".Length);
            }

            string combined = Path.Combine(assetsRoot, relative).Replace('\\', '/');
            string normalized = Path.GetFullPath(combined).Replace('\\', '/');

            bool underAssets = normalized.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
            if (!underAssets)
            {
                fullPath = null;
                assetPath = null;
                return false;
            }

            fullPath = normalized;
            string tail = normalized.Length > assetsRoot.Length ? normalized.Substring(assetsRoot.Length).TrimStart('/') : string.Empty;
            assetPath = string.IsNullOrEmpty(tail) ? "Assets" : ("Assets/" + tail);
            return true;
        }

        #endregion

        #region UXML Operations

        private static object GetUxmlStructure(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required for get_uxml_structure.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"UXML file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                XDocument doc = XDocument.Parse(content);

                var structure = ParseUxmlElement(doc.Root);

                return new SuccessResponse("UXML structure retrieved.", new
                {
                    path = assetPath,
                    structure
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to parse UXML: {e.Message}");
            }
        }

        private static object ParseUxmlElement(XElement element)
        {
            if (element == null) return null;

            var nameAttr = element.Attribute("name")?.Value;
            var classAttr = element.Attribute("class")?.Value;

            // Get the local name without namespace
            string elementType = element.Name.LocalName;

            // Build attributes dictionary excluding xmlns declarations
            var attributes = element.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .ToDictionary(a => a.Name.LocalName, a => a.Value);

            var children = element.Elements()
                .Select(ParseUxmlElement)
                .Where(c => c != null)
                .ToList();

            return new
            {
                type = elementType,
                name = nameAttr,
                classes = classAttr?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
                attributes,
                children
            };
        }

        private static object AddVisualElement(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string elementName = @params["elementName"]?.ToString();
            string elementType = @params["elementType"]?.ToString() ?? "VisualElement";
            string parentName = @params["parentName"]?.ToString();
            string attributesJson = @params["attributes"]?.ToString();
            string classes = @params["classes"]?.ToString();
            string text = @params["text"]?.ToString();
            int insertIndex = @params["insertIndex"]?.ToObject<int?>() ?? -1;

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"UXML file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                XDocument doc = XDocument.Parse(content, LoadOptions.PreserveWhitespace);

                // Find parent element
                XElement parent = null;
                if (!string.IsNullOrEmpty(parentName))
                {
                    parent = FindElementByName(doc.Root, parentName);
                    if (parent == null)
                    {
                        return new ErrorResponse($"Parent element '{parentName}' not found.");
                    }
                }
                else
                {
                    // Default to root or first VisualElement child
                    parent = doc.Root.Elements().FirstOrDefault() ?? doc.Root;
                }

                // Determine namespace
                XNamespace ui = "UnityEngine.UIElements";
                XNamespace uie = "UnityEditor.UIElements";

                // Check if element type is from editor namespace
                bool isEditorElement = elementType.StartsWith("ObjectField") ||
                                       elementType.StartsWith("PropertyField") ||
                                       elementType.StartsWith("InspectorElement");

                XNamespace ns = isEditorElement ? uie : ui;

                // Create new element
                XElement newElement = new XElement(ns + elementType);

                // Set name attribute
                if (!string.IsNullOrEmpty(elementName))
                {
                    newElement.SetAttributeValue("name", elementName);
                }

                // Set class attribute
                if (!string.IsNullOrEmpty(classes))
                {
                    newElement.SetAttributeValue("class", classes.Replace(",", " ").Trim());
                }

                // Set text attribute for appropriate elements
                if (!string.IsNullOrEmpty(text) && (elementType == "Label" || elementType == "Button" || elementType == "Toggle"))
                {
                    newElement.SetAttributeValue("text", text);
                }

                // Parse and set additional attributes
                if (!string.IsNullOrEmpty(attributesJson))
                {
                    try
                    {
                        var attrs = JObject.Parse(attributesJson);
                        foreach (var prop in attrs.Properties())
                        {
                            newElement.SetAttributeValue(prop.Name, prop.Value?.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        return new ErrorResponse($"Invalid attributes JSON: {e.Message}");
                    }
                }

                // Insert at specified position
                if (insertIndex >= 0 && insertIndex < parent.Elements().Count())
                {
                    var elements = parent.Elements().ToList();
                    elements[insertIndex].AddBeforeSelf(newElement);
                }
                else
                {
                    parent.Add(newElement);
                }

                // Save with proper formatting
                SaveUxml(doc, fullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return new SuccessResponse($"Added {elementType} element.", new
                {
                    path = assetPath,
                    elementType,
                    elementName,
                    parentName = parentName ?? "root"
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to add element: {e.Message}");
            }
        }

        private static object RemoveVisualElement(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string elementName = @params["elementName"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (string.IsNullOrEmpty(elementName))
            {
                return new ErrorResponse("'elementName' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"UXML file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                XDocument doc = XDocument.Parse(content, LoadOptions.PreserveWhitespace);

                XElement target = FindElementByName(doc.Root, elementName);
                if (target == null)
                {
                    return new ErrorResponse($"Element '{elementName}' not found.");
                }

                target.Remove();

                SaveUxml(doc, fullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return new SuccessResponse($"Removed element '{elementName}'.", new
                {
                    path = assetPath,
                    removedElement = elementName
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to remove element: {e.Message}");
            }
        }

        private static object ModifyVisualElement(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string elementName = @params["elementName"]?.ToString();
            string attributesJson = @params["attributes"]?.ToString();
            string classes = @params["classes"]?.ToString();
            string text = @params["text"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (string.IsNullOrEmpty(elementName))
            {
                return new ErrorResponse("'elementName' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"UXML file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                XDocument doc = XDocument.Parse(content, LoadOptions.PreserveWhitespace);

                XElement target = FindElementByName(doc.Root, elementName);
                if (target == null)
                {
                    return new ErrorResponse($"Element '{elementName}' not found.");
                }

                var modifiedAttributes = new List<string>();

                // Update class attribute
                if (classes != null)
                {
                    target.SetAttributeValue("class", classes.Replace(",", " ").Trim());
                    modifiedAttributes.Add("class");
                }

                // Update text attribute
                if (text != null)
                {
                    target.SetAttributeValue("text", text);
                    modifiedAttributes.Add("text");
                }

                // Parse and set additional attributes
                if (!string.IsNullOrEmpty(attributesJson))
                {
                    try
                    {
                        var attrs = JObject.Parse(attributesJson);
                        foreach (var prop in attrs.Properties())
                        {
                            if (prop.Value == null || prop.Value.Type == JTokenType.Null)
                            {
                                target.Attribute(prop.Name)?.Remove();
                            }
                            else
                            {
                                target.SetAttributeValue(prop.Name, prop.Value.ToString());
                            }
                            modifiedAttributes.Add(prop.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        return new ErrorResponse($"Invalid attributes JSON: {e.Message}");
                    }
                }

                SaveUxml(doc, fullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return new SuccessResponse($"Modified element '{elementName}'.", new
                {
                    path = assetPath,
                    elementName,
                    modifiedAttributes
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to modify element: {e.Message}");
            }
        }

        private static XElement FindElementByName(XElement root, string name)
        {
            if (root.Attribute("name")?.Value == name)
            {
                return root;
            }

            foreach (var child in root.Elements())
            {
                var found = FindElementByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SaveUxml(XDocument doc, string path)
        {
            var settings = new System.Xml.XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                OmitXmlDeclaration = true,
                NewLineOnAttributes = false
            };

            using (var writer = System.Xml.XmlWriter.Create(path, settings))
            {
                doc.Save(writer);
            }
        }

        #endregion

        #region USS Operations

        private static object GetUssRules(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required for get_uss_rules.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"USS file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                var rules = ParseUssRules(content);

                return new SuccessResponse("USS rules retrieved.", new
                {
                    path = assetPath,
                    rules,
                    ruleCount = rules.Count
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to parse USS: {e.Message}");
            }
        }

        private static List<object> ParseUssRules(string content)
        {
            var rules = new List<object>();

            // Simple USS parser - matches selector { properties }
            var rulePattern = new Regex(
                @"([^{}]+)\s*\{([^{}]*)\}",
                RegexOptions.Multiline | RegexOptions.Singleline,
                TimeSpan.FromSeconds(5)
            );

            var matches = rulePattern.Matches(content);
            foreach (Match match in matches)
            {
                string selector = match.Groups[1].Value.Trim();
                string propertiesBlock = match.Groups[2].Value.Trim();

                // Skip if selector is empty or contains only whitespace/comments
                if (string.IsNullOrWhiteSpace(selector) || selector.StartsWith("/*"))
                {
                    continue;
                }

                var properties = ParseUssProperties(propertiesBlock);

                rules.Add(new
                {
                    selector,
                    properties
                });
            }

            return rules;
        }

        private static Dictionary<string, string> ParseUssProperties(string block)
        {
            var properties = new Dictionary<string, string>();

            // Match property: value; pairs
            var propPattern = new Regex(
                @"([\w-]+)\s*:\s*([^;]+);?",
                RegexOptions.Multiline,
                TimeSpan.FromSeconds(2)
            );

            var matches = propPattern.Matches(block);
            foreach (Match match in matches)
            {
                string name = match.Groups[1].Value.Trim();
                string value = match.Groups[2].Value.Trim();
                properties[name] = value;
            }

            return properties;
        }

        private static object AddStyleRule(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string selector = @params["selector"]?.ToString();
            string stylePropertiesJson = @params["styleProperties"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (string.IsNullOrEmpty(selector))
            {
                return new ErrorResponse("'selector' parameter is required.");
            }

            if (string.IsNullOrEmpty(stylePropertiesJson))
            {
                return new ErrorResponse("'styleProperties' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            // Create file if it doesn't exist
            if (!File.Exists(fullPath))
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(fullPath, "/* Generated USS file */\n");
            }

            try
            {
                JObject props = JObject.Parse(stylePropertiesJson);
                var sb = new StringBuilder();

                // Build the rule
                sb.AppendLine();
                sb.AppendLine($"{selector} {{");
                foreach (var prop in props.Properties())
                {
                    sb.AppendLine($"    {prop.Name}: {prop.Value};");
                }
                sb.AppendLine("}");

                // Append to file
                File.AppendAllText(fullPath, sb.ToString());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return new SuccessResponse($"Added style rule '{selector}'.", new
                {
                    path = assetPath,
                    selector,
                    propertyCount = props.Count
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to add style rule: {e.Message}");
            }
        }

        private static object ModifyStyleRule(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string selector = @params["selector"]?.ToString();
            string stylePropertiesJson = @params["styleProperties"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (string.IsNullOrEmpty(selector))
            {
                return new ErrorResponse("'selector' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"USS file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                JObject props = null;

                if (!string.IsNullOrEmpty(stylePropertiesJson))
                {
                    props = JObject.Parse(stylePropertiesJson);
                }

                // Find and replace the rule
                string escapedSelector = Regex.Escape(selector);
                var rulePattern = new Regex(
                    $@"({escapedSelector})\s*\{{([^{{}}]*)\}}",
                    RegexOptions.Multiline | RegexOptions.Singleline,
                    TimeSpan.FromSeconds(5)
                );

                var match = rulePattern.Match(content);
                if (!match.Success)
                {
                    return new ErrorResponse($"Style rule '{selector}' not found in USS file.");
                }

                // Parse existing properties and merge with new ones
                var existingProps = ParseUssProperties(match.Groups[2].Value);

                if (props != null)
                {
                    foreach (var prop in props.Properties())
                    {
                        if (prop.Value == null || prop.Value.Type == JTokenType.Null)
                        {
                            existingProps.Remove(prop.Name);
                        }
                        else
                        {
                            existingProps[prop.Name] = prop.Value.ToString();
                        }
                    }
                }

                // Rebuild the rule
                var sb = new StringBuilder();
                sb.AppendLine($"{selector} {{");
                foreach (var kvp in existingProps)
                {
                    sb.AppendLine($"    {kvp.Key}: {kvp.Value};");
                }
                sb.Append("}");

                string newContent = rulePattern.Replace(content, sb.ToString(), 1);
                File.WriteAllText(fullPath, newContent);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return new SuccessResponse($"Modified style rule '{selector}'.", new
                {
                    path = assetPath,
                    selector,
                    propertyCount = existingProps.Count
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to modify style rule: {e.Message}");
            }
        }

        private static object GetElementStyles(JObject @params)
        {
            string path = @params["path"]?.ToString();
            string elementName = @params["elementName"]?.ToString();

            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'path' parameter is required.");
            }

            if (string.IsNullOrEmpty(elementName))
            {
                return new ErrorResponse("'elementName' parameter is required.");
            }

            if (!TryResolveAssetPath(path, out string fullPath, out string assetPath))
            {
                return new ErrorResponse($"Invalid path: '{path}'. Must be under Assets/.");
            }

            if (!File.Exists(fullPath))
            {
                return new ErrorResponse($"UXML file not found: '{assetPath}'");
            }

            try
            {
                string content = File.ReadAllText(fullPath);
                XDocument doc = XDocument.Parse(content);

                XElement target = FindElementByName(doc.Root, elementName);
                if (target == null)
                {
                    return new ErrorResponse($"Element '{elementName}' not found.");
                }

                // Get inline styles
                var styleAttr = target.Attribute("style")?.Value;
                var inlineStyles = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(styleAttr))
                {
                    inlineStyles = ParseUssProperties(styleAttr);
                }

                // Get assigned classes
                var classAttr = target.Attribute("class")?.Value;
                var classes = classAttr?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                return new SuccessResponse($"Retrieved styles for element '{elementName}'.", new
                {
                    path = assetPath,
                    elementName,
                    elementType = target.Name.LocalName,
                    classes,
                    inlineStyles,
                    selectors = new[]
                    {
                        $"#{elementName}",
                        target.Name.LocalName
                    }.Concat(classes.Select(c => $".{c}")).ToArray()
                });
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Failed to get element styles: {e.Message}");
            }
        }

        #endregion
    }
}
