using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles uGUI Canvas and UI element operations including creating canvases, adding UI elements, and configuring layouts.
    /// </summary>
    [McpForUnityTool("manage_canvas", AutoRegister = false)]
    public static class ManageCanvas
    {
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
                        return new SuccessResponse("pong", new { tool = "manage_canvas" });

                    case "create_canvas":
                        return CreateCanvas(@params);

                    case "add_element":
                        return AddElement(@params);

                    case "set_layout":
                        return SetLayout(@params);

                    case "set_text":
                        return SetText(@params);

                    case "set_image":
                        return SetImage(@params);

                    case "get_hierarchy":
                        return GetHierarchy(@params);

                    case "set_rect_transform":
                        return SetRectTransform(@params);

                    case "get_canvas_info":
                        return GetCanvasInfo(@params);

                    case "set_canvas_properties":
                        return SetCanvasProperties(@params);

                    case "add_layout_group":
                        return AddLayoutGroup(@params);

                    case "ensure_event_system":
                        return EnsureEventSystem();

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: create_canvas, add_element, set_layout, set_text, set_image, get_hierarchy, set_rect_transform, get_canvas_info, set_canvas_properties, add_layout_group, ensure_event_system");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static GameObject FindUIElement(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            var goInstruction = new JObject { ["find"] = target };
            string searchMethod = @params["searchMethod"]?.ToString();
            if (!string.IsNullOrEmpty(searchMethod))
            {
                goInstruction["method"] = searchMethod;
            }

            return ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
        }

        private static Color ParseColor(JToken token, Color defaultColor)
        {
            if (token == null) return defaultColor;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Color(
                    arr[0].ToObject<float>(),
                    arr[1].ToObject<float>(),
                    arr[2].ToObject<float>(),
                    arr.Count > 3 ? arr[3].ToObject<float>() : 1f
                );
            }

            if (token is JObject obj)
            {
                return new Color(
                    obj["r"]?.ToObject<float>() ?? defaultColor.r,
                    obj["g"]?.ToObject<float>() ?? defaultColor.g,
                    obj["b"]?.ToObject<float>() ?? defaultColor.b,
                    obj["a"]?.ToObject<float>() ?? 1f
                );
            }

            if (token.Type == JTokenType.String)
            {
                string hex = token.ToString();
                if (ColorUtility.TryParseHtmlString(hex, out Color parsed))
                {
                    return parsed;
                }
            }

            return defaultColor;
        }

        private static Vector2 ParseVector2(JToken token, Vector2 defaultValue = default)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 2)
            {
                return new Vector2(
                    arr[0].ToObject<float>(),
                    arr[1].ToObject<float>()
                );
            }

            if (token is JObject obj)
            {
                return new Vector2(
                    obj["x"]?.ToObject<float>() ?? defaultValue.x,
                    obj["y"]?.ToObject<float>() ?? defaultValue.y
                );
            }

            return defaultValue;
        }

        private static object CreateCanvas(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "Canvas";
            string renderModeStr = @params["renderMode"]?.ToString()?.ToLowerInvariant() ?? "screenspaceoverlay";

            RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
            switch (renderModeStr)
            {
                case "screenspaceoverlay":
                case "overlay":
                    renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
                case "screenspacecamera":
                case "camera":
                    renderMode = RenderMode.ScreenSpaceCamera;
                    break;
                case "worldspace":
                case "world":
                    renderMode = RenderMode.WorldSpace;
                    break;
            }

            // Create Canvas GameObject
            GameObject canvasGo = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = renderMode;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = canvasGo.AddComponent<GraphicRaycaster>();

            // Configure scaler
            if (@params["scaleMode"] != null)
            {
                string scaleMode = @params["scaleMode"].ToString().ToLowerInvariant();
                switch (scaleMode)
                {
                    case "constantpixelsize":
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                        break;
                    case "scalewithscreensize":
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        break;
                    case "constantphysicalsize":
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
                        break;
                }
            }

            if (@params["referenceResolution"] != null)
            {
                scaler.referenceResolution = ParseVector2(@params["referenceResolution"], new Vector2(1920, 1080));
            }

            if (@params["matchWidthOrHeight"] != null)
            {
                scaler.matchWidthOrHeight = @params["matchWidthOrHeight"].ToObject<float>();
            }

            // Set camera for ScreenSpaceCamera mode
            if (renderMode == RenderMode.ScreenSpaceCamera && @params["camera"] != null)
            {
                string cameraTarget = @params["camera"].ToString();
                var camInstruction = new JObject { ["find"] = cameraTarget };
                GameObject camGo = ObjectResolver.Resolve(camInstruction, typeof(GameObject)) as GameObject;
                if (camGo != null)
                {
                    Camera cam = camGo.GetComponent<Camera>();
                    if (cam != null)
                    {
                        canvas.worldCamera = cam;
                    }
                }
            }

            // Ensure EventSystem exists
            EnsureEventSystemExists();

            EditorUtility.SetDirty(canvas);

            return new SuccessResponse($"Created canvas '{name}'", new
            {
                gameObject = canvasGo.name,
                instanceID = canvasGo.GetInstanceID(),
                renderMode = renderMode.ToString(),
                scaleMode = scaler.uiScaleMode.ToString(),
                referenceResolution = new { x = scaler.referenceResolution.x, y = scaler.referenceResolution.y }
            });
        }

        private static void EnsureEventSystemExists()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static object AddElement(JObject @params)
        {
            string elementType = @params["elementType"]?.ToString()?.ToLowerInvariant() ?? "image";
            string name = @params["name"]?.ToString();

            // Find parent (canvas or other UI element)
            GameObject parent = null;
            if (@params["parent"] != null)
            {
                var parentInstruction = new JObject { ["find"] = @params["parent"].ToString() };
                parent = ObjectResolver.Resolve(parentInstruction, typeof(GameObject)) as GameObject;
            }

            if (parent == null)
            {
                // Find first canvas
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    return new ErrorResponse("No canvas found. Create a canvas first.");
                }
                parent = canvas.gameObject;
            }

            GameObject element = null;

            switch (elementType)
            {
                case "image":
                    element = CreateImage(@params, parent);
                    break;

                case "text":
                case "textmeshpro":
                case "tmp":
                    element = CreateText(@params, parent);
                    break;

                case "button":
                    element = CreateButton(@params, parent);
                    break;

                case "toggle":
                    element = CreateToggle(@params, parent);
                    break;

                case "slider":
                    element = CreateSlider(@params, parent);
                    break;

                case "inputfield":
                case "input":
                    element = CreateInputField(@params, parent);
                    break;

                case "dropdown":
                    element = CreateDropdown(@params, parent);
                    break;

                case "scrollview":
                case "scroll":
                    element = CreateScrollView(@params, parent);
                    break;

                case "panel":
                    element = CreatePanel(@params, parent);
                    break;

                case "rawimage":
                    element = CreateRawImage(@params, parent);
                    break;

                default:
                    return new ErrorResponse($"Unknown element type: {elementType}. Valid types: image, text, button, toggle, slider, inputfield, dropdown, scrollview, panel, rawimage");
            }

            if (element == null)
            {
                return new ErrorResponse($"Failed to create {elementType} element");
            }

            if (!string.IsNullOrEmpty(name))
            {
                element.name = name;
            }

            // Apply common RectTransform settings
            RectTransform rt = element.GetComponent<RectTransform>();
            if (rt != null)
            {
                ApplyRectTransformSettings(rt, @params);
            }

            EditorUtility.SetDirty(element);

            return new SuccessResponse($"Created {elementType} element '{element.name}'", new
            {
                gameObject = element.name,
                instanceID = element.GetInstanceID(),
                elementType = elementType,
                parent = parent.name
            });
        }

        private static GameObject CreateImage(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Image");
            Undo.RegisterCreatedObjectUndo(go, "Create Image");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();

            if (@params["color"] != null)
            {
                image.color = ParseColor(@params["color"], Color.white);
            }

            if (@params["sprite"] != null)
            {
                string spritePath = @params["sprite"].ToString();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }

            if (@params["raycastTarget"] != null)
            {
                image.raycastTarget = @params["raycastTarget"].ToObject<bool>();
            }

            return go;
        }

        private static GameObject CreateText(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Text");
            Undo.RegisterCreatedObjectUndo(go, "Create Text");
            go.transform.SetParent(parent.transform, false);

            // Use legacy Text component (TMPro requires package check)
            Text text = go.AddComponent<Text>();

            text.text = @params["text"]?.ToString() ?? "New Text";

            if (@params["fontSize"] != null)
            {
                text.fontSize = @params["fontSize"].ToObject<int>();
            }

            if (@params["color"] != null)
            {
                text.color = ParseColor(@params["color"], Color.black);
            }

            if (@params["alignment"] != null)
            {
                string align = @params["alignment"].ToString().ToLowerInvariant();
                switch (align)
                {
                    case "upperleft":
                        text.alignment = TextAnchor.UpperLeft;
                        break;
                    case "uppercenter":
                        text.alignment = TextAnchor.UpperCenter;
                        break;
                    case "upperright":
                        text.alignment = TextAnchor.UpperRight;
                        break;
                    case "middleleft":
                        text.alignment = TextAnchor.MiddleLeft;
                        break;
                    case "middlecenter":
                        text.alignment = TextAnchor.MiddleCenter;
                        break;
                    case "middleright":
                        text.alignment = TextAnchor.MiddleRight;
                        break;
                    case "lowerleft":
                        text.alignment = TextAnchor.LowerLeft;
                        break;
                    case "lowercenter":
                        text.alignment = TextAnchor.LowerCenter;
                        break;
                    case "lowerright":
                        text.alignment = TextAnchor.LowerRight;
                        break;
                }
            }

            if (@params["font"] != null)
            {
                string fontPath = @params["font"].ToString();
                Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                if (font != null)
                {
                    text.font = font;
                }
            }
            else
            {
                text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            return go;
        }

        private static GameObject CreateButton(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Button");
            Undo.RegisterCreatedObjectUndo(go, "Create Button");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            Button button = go.AddComponent<Button>();

            if (@params["color"] != null)
            {
                image.color = ParseColor(@params["color"], Color.white);
            }

            // Create text child
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.text = @params["text"]?.ToString() ?? "Button";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Set default size
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);

            return go;
        }

        private static GameObject CreateToggle(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Toggle");
            Undo.RegisterCreatedObjectUndo(go, "Create Toggle");
            go.transform.SetParent(parent.transform, false);

            Toggle toggle = go.AddComponent<Toggle>();

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.color = Color.white;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.sizeDelta = new Vector2(20, 20);
            bgRt.anchoredPosition = new Vector2(10, 0);

            // Checkmark
            GameObject checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            Image checkImage = checkGo.AddComponent<Image>();
            checkImage.color = Color.black;
            RectTransform checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.sizeDelta = new Vector2(14, 14);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            // Label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = @params["text"]?.ToString() ?? "Toggle";
            label.color = Color.black;
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(25, 0);
            labelRt.offsetMax = Vector2.zero;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 20);

            if (@params["isOn"] != null)
            {
                toggle.isOn = @params["isOn"].ToObject<bool>();
            }

            return go;
        }

        private static GameObject CreateSlider(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Slider");
            Undo.RegisterCreatedObjectUndo(go, "Create Slider");
            go.transform.SetParent(parent.transform, false);

            Slider slider = go.AddComponent<Slider>();

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill Area
            GameObject fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(go.transform, false);
            RectTransform fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-15, 0);

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = new Vector2(10, 0);

            // Handle Slide Area
            GameObject handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(go.transform, false);
            RectTransform handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0, 0);
            handleRt.anchorMax = new Vector2(0, 1);
            handleRt.sizeDelta = new Vector2(20, 0);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 20);

            if (@params["minValue"] != null)
            {
                slider.minValue = @params["minValue"].ToObject<float>();
            }
            if (@params["maxValue"] != null)
            {
                slider.maxValue = @params["maxValue"].ToObject<float>();
            }
            if (@params["value"] != null)
            {
                slider.value = @params["value"].ToObject<float>();
            }
            if (@params["wholeNumbers"] != null)
            {
                slider.wholeNumbers = @params["wholeNumbers"].ToObject<bool>();
            }

            return go;
        }

        private static GameObject CreateInputField(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("InputField");
            Undo.RegisterCreatedObjectUndo(go, "Create InputField");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = Color.white;
            InputField inputField = go.AddComponent<InputField>();

            // Placeholder
            GameObject placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            Text placeholder = placeholderGo.AddComponent<Text>();
            placeholder.text = @params["placeholder"]?.ToString() ?? "Enter text...";
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(0, 0, 0, 0.5f);
            placeholder.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.alignment = TextAnchor.MiddleLeft;
            RectTransform phRt = placeholderGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 6);
            phRt.offsetMax = new Vector2(-10, -7);

            // Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.text = @params["text"]?.ToString() ?? "";
            text.color = Color.black;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 6);
            textRt.offsetMax = new Vector2(-10, -7);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);

            return go;
        }

        private static GameObject CreateDropdown(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Dropdown");
            Undo.RegisterCreatedObjectUndo(go, "Create Dropdown");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = Color.white;
            Dropdown dropdown = go.AddComponent<Dropdown>();

            // Label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = "Option A";
            label.color = Color.black;
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10, 6);
            labelRt.offsetMax = new Vector2(-25, -7);

            dropdown.captionText = label;

            // Add options
            if (@params["options"] != null && @params["options"] is JArray optionsArray)
            {
                foreach (var opt in optionsArray)
                {
                    dropdown.options.Add(new Dropdown.OptionData(opt.ToString()));
                }
            }
            else
            {
                dropdown.options.Add(new Dropdown.OptionData("Option A"));
                dropdown.options.Add(new Dropdown.OptionData("Option B"));
                dropdown.options.Add(new Dropdown.OptionData("Option C"));
            }

            dropdown.RefreshShownValue();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);

            return go;
        }

        private static GameObject CreateScrollView(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Scroll View");
            Undo.RegisterCreatedObjectUndo(go, "Create Scroll View");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.4f);
            ScrollRect scrollRect = go.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(go.transform, false);
            Image viewportMask = viewportGo.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-17, 0);
            viewportRt.pivot = new Vector2(0, 1);

            // Content
            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.sizeDelta = new Vector2(0, 300);

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);

            return go;
        }

        private static GameObject CreatePanel(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("Panel");
            Undo.RegisterCreatedObjectUndo(go, "Create Panel");
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = ParseColor(@params["color"], new Color(1, 1, 1, 0.4f));

            if (@params["raycastTarget"] != null)
            {
                image.raycastTarget = @params["raycastTarget"].ToObject<bool>();
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateRawImage(JObject @params, GameObject parent)
        {
            GameObject go = new GameObject("RawImage");
            Undo.RegisterCreatedObjectUndo(go, "Create RawImage");
            go.transform.SetParent(parent.transform, false);

            RawImage rawImage = go.AddComponent<RawImage>();

            if (@params["color"] != null)
            {
                rawImage.color = ParseColor(@params["color"], Color.white);
            }

            if (@params["texture"] != null)
            {
                string texturePath = @params["texture"].ToString();
                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                if (texture != null)
                {
                    rawImage.texture = texture;
                }
            }

            return go;
        }

        private static void ApplyRectTransformSettings(RectTransform rt, JObject @params)
        {
            if (@params["anchorMin"] != null)
            {
                rt.anchorMin = ParseVector2(@params["anchorMin"]);
            }
            if (@params["anchorMax"] != null)
            {
                rt.anchorMax = ParseVector2(@params["anchorMax"]);
            }
            if (@params["pivot"] != null)
            {
                rt.pivot = ParseVector2(@params["pivot"], new Vector2(0.5f, 0.5f));
            }
            if (@params["anchoredPosition"] != null)
            {
                rt.anchoredPosition = ParseVector2(@params["anchoredPosition"]);
            }
            if (@params["sizeDelta"] != null)
            {
                rt.sizeDelta = ParseVector2(@params["sizeDelta"]);
            }
            if (@params["offsetMin"] != null)
            {
                rt.offsetMin = ParseVector2(@params["offsetMin"]);
            }
            if (@params["offsetMax"] != null)
            {
                rt.offsetMax = ParseVector2(@params["offsetMax"]);
            }
            if (@params["width"] != null)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, @params["width"].ToObject<float>());
            }
            if (@params["height"] != null)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, @params["height"].ToObject<float>());
            }
        }

        private static object SetLayout(JObject @params)
        {
            GameObject target = FindUIElement(@params);
            if (target == null)
            {
                return new ErrorResponse("Target UI element not found");
            }

            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            Undo.RecordObject(layout, "Set Layout Element");

            var changes = new List<string>();

            if (@params["minWidth"] != null)
            {
                layout.minWidth = @params["minWidth"].ToObject<float>();
                changes.Add($"minWidth={layout.minWidth}");
            }
            if (@params["minHeight"] != null)
            {
                layout.minHeight = @params["minHeight"].ToObject<float>();
                changes.Add($"minHeight={layout.minHeight}");
            }
            if (@params["preferredWidth"] != null)
            {
                layout.preferredWidth = @params["preferredWidth"].ToObject<float>();
                changes.Add($"preferredWidth={layout.preferredWidth}");
            }
            if (@params["preferredHeight"] != null)
            {
                layout.preferredHeight = @params["preferredHeight"].ToObject<float>();
                changes.Add($"preferredHeight={layout.preferredHeight}");
            }
            if (@params["flexibleWidth"] != null)
            {
                layout.flexibleWidth = @params["flexibleWidth"].ToObject<float>();
                changes.Add($"flexibleWidth={layout.flexibleWidth}");
            }
            if (@params["flexibleHeight"] != null)
            {
                layout.flexibleHeight = @params["flexibleHeight"].ToObject<float>();
                changes.Add($"flexibleHeight={layout.flexibleHeight}");
            }
            if (@params["ignoreLayout"] != null)
            {
                layout.ignoreLayout = @params["ignoreLayout"].ToObject<bool>();
                changes.Add($"ignoreLayout={layout.ignoreLayout}");
            }

            EditorUtility.SetDirty(layout);

            return new SuccessResponse($"Set layout on '{target.name}'", new
            {
                gameObject = target.name,
                changes = changes
            });
        }

        private static object SetText(JObject @params)
        {
            GameObject target = FindUIElement(@params);
            if (target == null)
            {
                return new ErrorResponse("Target UI element not found");
            }

            Text text = target.GetComponent<Text>();
            if (text == null)
            {
                return new ErrorResponse("Target does not have a Text component");
            }

            Undo.RecordObject(text, "Set Text");

            if (@params["text"] != null)
            {
                text.text = @params["text"].ToString();
            }
            if (@params["fontSize"] != null)
            {
                text.fontSize = @params["fontSize"].ToObject<int>();
            }
            if (@params["color"] != null)
            {
                text.color = ParseColor(@params["color"], text.color);
            }

            EditorUtility.SetDirty(text);

            return new SuccessResponse($"Updated text on '{target.name}'", new
            {
                gameObject = target.name,
                text = text.text
            });
        }

        private static object SetImage(JObject @params)
        {
            GameObject target = FindUIElement(@params);
            if (target == null)
            {
                return new ErrorResponse("Target UI element not found");
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                return new ErrorResponse("Target does not have an Image component");
            }

            Undo.RecordObject(image, "Set Image");

            if (@params["color"] != null)
            {
                image.color = ParseColor(@params["color"], image.color);
            }
            if (@params["sprite"] != null)
            {
                string spritePath = @params["sprite"].ToString();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }
            if (@params["fillAmount"] != null)
            {
                image.fillAmount = @params["fillAmount"].ToObject<float>();
            }

            EditorUtility.SetDirty(image);

            return new SuccessResponse($"Updated image on '{target.name}'", new
            {
                gameObject = target.name
            });
        }

        private static object GetHierarchy(JObject @params)
        {
            Canvas canvas = null;

            if (@params["canvas"] != null)
            {
                var canvasInstruction = new JObject { ["find"] = @params["canvas"].ToString() };
                GameObject canvasGo = ObjectResolver.Resolve(canvasInstruction, typeof(GameObject)) as GameObject;
                if (canvasGo != null)
                {
                    canvas = canvasGo.GetComponent<Canvas>();
                }
            }
            else
            {
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return new ErrorResponse("No canvas found");
            }

            object BuildHierarchy(Transform t)
            {
                var children = new List<object>();
                foreach (Transform child in t)
                {
                    children.Add(BuildHierarchy(child));
                }

                var components = new List<string>();
                foreach (var comp in t.GetComponents<Component>())
                {
                    if (comp != null)
                    {
                        components.Add(comp.GetType().Name);
                    }
                }

                RectTransform rt = t.GetComponent<RectTransform>();

                return new
                {
                    name = t.name,
                    instanceID = t.gameObject.GetInstanceID(),
                    components = components,
                    rect = rt != null ? new
                    {
                        anchoredPosition = new { x = rt.anchoredPosition.x, y = rt.anchoredPosition.y },
                        sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y }
                    } : null,
                    children = children
                };
            }

            return new SuccessResponse($"Retrieved hierarchy for canvas '{canvas.name}'", new
            {
                canvas = canvas.name,
                hierarchy = BuildHierarchy(canvas.transform)
            });
        }

        private static object SetRectTransform(JObject @params)
        {
            GameObject target = FindUIElement(@params);
            if (target == null)
            {
                return new ErrorResponse("Target UI element not found");
            }

            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt == null)
            {
                return new ErrorResponse("Target does not have a RectTransform");
            }

            Undo.RecordObject(rt, "Set RectTransform");
            ApplyRectTransformSettings(rt, @params);
            EditorUtility.SetDirty(rt);

            return new SuccessResponse($"Updated RectTransform on '{target.name}'", new
            {
                gameObject = target.name,
                anchoredPosition = new { x = rt.anchoredPosition.x, y = rt.anchoredPosition.y },
                sizeDelta = new { x = rt.sizeDelta.x, y = rt.sizeDelta.y },
                anchorMin = new { x = rt.anchorMin.x, y = rt.anchorMin.y },
                anchorMax = new { x = rt.anchorMax.x, y = rt.anchorMax.y },
                pivot = new { x = rt.pivot.x, y = rt.pivot.y }
            });
        }

        private static object GetCanvasInfo(JObject @params)
        {
            Canvas canvas = null;

            if (@params["target"] != null)
            {
                var canvasInstruction = new JObject { ["find"] = @params["target"].ToString() };
                GameObject canvasGo = ObjectResolver.Resolve(canvasInstruction, typeof(GameObject)) as GameObject;
                if (canvasGo != null)
                {
                    canvas = canvasGo.GetComponent<Canvas>();
                }
            }
            else
            {
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return new ErrorResponse("Canvas not found");
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

            return new SuccessResponse($"Retrieved canvas info for '{canvas.name}'", new
            {
                gameObject = canvas.gameObject.name,
                instanceID = canvas.gameObject.GetInstanceID(),
                renderMode = canvas.renderMode.ToString(),
                pixelPerfect = canvas.pixelPerfect,
                sortingOrder = canvas.sortingOrder,
                targetDisplay = canvas.targetDisplay,
                worldCamera = canvas.worldCamera?.name,
                scaler = scaler != null ? new
                {
                    uiScaleMode = scaler.uiScaleMode.ToString(),
                    referenceResolution = new { x = scaler.referenceResolution.x, y = scaler.referenceResolution.y },
                    matchWidthOrHeight = scaler.matchWidthOrHeight,
                    scaleFactor = scaler.scaleFactor,
                    referencePixelsPerUnit = scaler.referencePixelsPerUnit
                } : null
            });
        }

        private static object SetCanvasProperties(JObject @params)
        {
            Canvas canvas = null;

            if (@params["target"] != null)
            {
                var canvasInstruction = new JObject { ["find"] = @params["target"].ToString() };
                GameObject canvasGo = ObjectResolver.Resolve(canvasInstruction, typeof(GameObject)) as GameObject;
                if (canvasGo != null)
                {
                    canvas = canvasGo.GetComponent<Canvas>();
                }
            }

            if (canvas == null)
            {
                return new ErrorResponse("Canvas not found");
            }

            Undo.RecordObject(canvas, "Set Canvas Properties");

            var changes = new List<string>();

            if (@params["renderMode"] != null)
            {
                string rm = @params["renderMode"].ToString().ToLowerInvariant();
                switch (rm)
                {
                    case "screenspaceoverlay":
                    case "overlay":
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        break;
                    case "screenspacecamera":
                    case "camera":
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        break;
                    case "worldspace":
                    case "world":
                        canvas.renderMode = RenderMode.WorldSpace;
                        break;
                }
                changes.Add($"renderMode={canvas.renderMode}");
            }

            if (@params["sortingOrder"] != null)
            {
                canvas.sortingOrder = @params["sortingOrder"].ToObject<int>();
                changes.Add($"sortingOrder={canvas.sortingOrder}");
            }

            if (@params["pixelPerfect"] != null)
            {
                canvas.pixelPerfect = @params["pixelPerfect"].ToObject<bool>();
                changes.Add($"pixelPerfect={canvas.pixelPerfect}");
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Set Canvas Scaler Properties");

                if (@params["scaleMode"] != null)
                {
                    string sm = @params["scaleMode"].ToString().ToLowerInvariant();
                    switch (sm)
                    {
                        case "constantpixelsize":
                            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                            break;
                        case "scalewithscreensize":
                            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                            break;
                        case "constantphysicalsize":
                            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
                            break;
                    }
                    changes.Add($"scaleMode={scaler.uiScaleMode}");
                }

                if (@params["referenceResolution"] != null)
                {
                    scaler.referenceResolution = ParseVector2(@params["referenceResolution"], scaler.referenceResolution);
                    changes.Add($"referenceResolution set");
                }

                if (@params["matchWidthOrHeight"] != null)
                {
                    scaler.matchWidthOrHeight = @params["matchWidthOrHeight"].ToObject<float>();
                    changes.Add($"matchWidthOrHeight={scaler.matchWidthOrHeight}");
                }

                EditorUtility.SetDirty(scaler);
            }

            EditorUtility.SetDirty(canvas);

            return new SuccessResponse($"Updated canvas '{canvas.name}'", new
            {
                gameObject = canvas.name,
                changes = changes
            });
        }

        private static object AddLayoutGroup(JObject @params)
        {
            GameObject target = FindUIElement(@params);
            if (target == null)
            {
                return new ErrorResponse("Target UI element not found");
            }

            string layoutType = @params["layoutType"]?.ToString()?.ToLowerInvariant() ?? "vertical";

            // Remove existing layout groups
            HorizontalLayoutGroup hlg = target.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup vlg = target.GetComponent<VerticalLayoutGroup>();
            GridLayoutGroup glg = target.GetComponent<GridLayoutGroup>();

            if (hlg != null) Undo.DestroyObjectImmediate(hlg);
            if (vlg != null) Undo.DestroyObjectImmediate(vlg);
            if (glg != null) Undo.DestroyObjectImmediate(glg);

            HorizontalOrVerticalLayoutGroup layoutGroup = null;
            GridLayoutGroup gridLayout = null;

            switch (layoutType)
            {
                case "horizontal":
                    Undo.RecordObject(target, "Add Horizontal Layout Group");
                    layoutGroup = target.AddComponent<HorizontalLayoutGroup>();
                    break;

                case "vertical":
                    Undo.RecordObject(target, "Add Vertical Layout Group");
                    layoutGroup = target.AddComponent<VerticalLayoutGroup>();
                    break;

                case "grid":
                    Undo.RecordObject(target, "Add Grid Layout Group");
                    gridLayout = target.AddComponent<GridLayoutGroup>();
                    break;

                default:
                    return new ErrorResponse($"Unknown layout type: {layoutType}. Valid types: horizontal, vertical, grid");
            }

            if (layoutGroup != null)
            {
                if (@params["spacing"] != null)
                {
                    layoutGroup.spacing = @params["spacing"].ToObject<float>();
                }
                if (@params["childAlignment"] != null)
                {
                    string align = @params["childAlignment"].ToString().ToLowerInvariant();
                    layoutGroup.childAlignment = ParseTextAnchor(align);
                }
                if (@params["reverseArrangement"] != null)
                {
                    layoutGroup.reverseArrangement = @params["reverseArrangement"].ToObject<bool>();
                }
                if (@params["childControlWidth"] != null)
                {
                    layoutGroup.childControlWidth = @params["childControlWidth"].ToObject<bool>();
                }
                if (@params["childControlHeight"] != null)
                {
                    layoutGroup.childControlHeight = @params["childControlHeight"].ToObject<bool>();
                }
                if (@params["childForceExpandWidth"] != null)
                {
                    layoutGroup.childForceExpandWidth = @params["childForceExpandWidth"].ToObject<bool>();
                }
                if (@params["childForceExpandHeight"] != null)
                {
                    layoutGroup.childForceExpandHeight = @params["childForceExpandHeight"].ToObject<bool>();
                }
                if (@params["padding"] != null && @params["padding"] is JObject padObj)
                {
                    layoutGroup.padding = new RectOffset(
                        padObj["left"]?.ToObject<int>() ?? 0,
                        padObj["right"]?.ToObject<int>() ?? 0,
                        padObj["top"]?.ToObject<int>() ?? 0,
                        padObj["bottom"]?.ToObject<int>() ?? 0
                    );
                }

                EditorUtility.SetDirty(layoutGroup);
            }

            if (gridLayout != null)
            {
                if (@params["cellSize"] != null)
                {
                    gridLayout.cellSize = ParseVector2(@params["cellSize"], new Vector2(100, 100));
                }
                if (@params["spacing"] != null)
                {
                    gridLayout.spacing = ParseVector2(@params["spacing"]);
                }
                if (@params["startCorner"] != null)
                {
                    string corner = @params["startCorner"].ToString().ToLowerInvariant();
                    switch (corner)
                    {
                        case "upperleft":
                            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                            break;
                        case "upperright":
                            gridLayout.startCorner = GridLayoutGroup.Corner.UpperRight;
                            break;
                        case "lowerleft":
                            gridLayout.startCorner = GridLayoutGroup.Corner.LowerLeft;
                            break;
                        case "lowerright":
                            gridLayout.startCorner = GridLayoutGroup.Corner.LowerRight;
                            break;
                    }
                }
                if (@params["startAxis"] != null)
                {
                    string axis = @params["startAxis"].ToString().ToLowerInvariant();
                    gridLayout.startAxis = axis == "vertical" ? GridLayoutGroup.Axis.Vertical : GridLayoutGroup.Axis.Horizontal;
                }
                if (@params["childAlignment"] != null)
                {
                    string align = @params["childAlignment"].ToString().ToLowerInvariant();
                    gridLayout.childAlignment = ParseTextAnchor(align);
                }
                if (@params["constraint"] != null)
                {
                    string constraint = @params["constraint"].ToString().ToLowerInvariant();
                    switch (constraint)
                    {
                        case "flexible":
                            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
                            break;
                        case "fixedcolumncount":
                            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                            break;
                        case "fixedrowcount":
                            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                            break;
                    }
                }
                if (@params["constraintCount"] != null)
                {
                    gridLayout.constraintCount = @params["constraintCount"].ToObject<int>();
                }

                EditorUtility.SetDirty(gridLayout);
            }

            return new SuccessResponse($"Added {layoutType} layout group to '{target.name}'", new
            {
                gameObject = target.name,
                layoutType = layoutType
            });
        }

        private static TextAnchor ParseTextAnchor(string align)
        {
            switch (align)
            {
                case "upperleft":
                    return TextAnchor.UpperLeft;
                case "uppercenter":
                    return TextAnchor.UpperCenter;
                case "upperright":
                    return TextAnchor.UpperRight;
                case "middleleft":
                    return TextAnchor.MiddleLeft;
                case "middlecenter":
                    return TextAnchor.MiddleCenter;
                case "middleright":
                    return TextAnchor.MiddleRight;
                case "lowerleft":
                    return TextAnchor.LowerLeft;
                case "lowercenter":
                    return TextAnchor.LowerCenter;
                case "lowerright":
                    return TextAnchor.LowerRight;
                default:
                    return TextAnchor.UpperLeft;
            }
        }

        private static object EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            bool created = false;

            if (eventSystem == null)
            {
                EnsureEventSystemExists();
                eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                created = true;
            }

            return new SuccessResponse(created ? "Created EventSystem" : "EventSystem already exists", new
            {
                gameObject = eventSystem?.gameObject.name,
                created = created
            });
        }
    }
}
