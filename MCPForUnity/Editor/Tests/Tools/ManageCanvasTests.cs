using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageCanvasTests
    {
        private GameObject _canvasObject;
        private Canvas _canvas;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas");
            _canvas = _canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasObject.AddComponent<CanvasScaler>();
            _canvasObject.AddComponent<GraphicRaycaster>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_canvasObject);
            }

            // Clean up any created canvases
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.name.StartsWith("Test"))
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
            }

            // Clean up EventSystem
            var eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                UnityEngine.Object.DestroyImmediate(es.gameObject);
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void CreateCanvas_ScreenSpaceOverlay_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create_canvas",
                ["name"] = "TestNewCanvas",
                ["renderMode"] = "screenspaceoverlay"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdCanvas = GameObject.Find("TestNewCanvas");
            Assert.IsNotNull(createdCanvas);
            var canvas = createdCanvas.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);

            UnityEngine.Object.DestroyImmediate(createdCanvas);
        }

        [Test]
        public void CreateCanvas_WorldSpace_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "create_canvas",
                ["name"] = "TestWorldCanvas",
                ["renderMode"] = "worldspace"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var createdCanvas = GameObject.Find("TestWorldCanvas");
            Assert.IsNotNull(createdCanvas);
            var canvas = createdCanvas.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);

            UnityEngine.Object.DestroyImmediate(createdCanvas);
        }

        [Test]
        public void AddElement_Image_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "add_element",
                ["elementType"] = "image",
                ["name"] = "TestImage",
                ["parent"] = _canvasObject.name
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var imageGo = GameObject.Find("TestImage");
            Assert.IsNotNull(imageGo);
            Assert.IsNotNull(imageGo.GetComponent<Image>());
        }

        [Test]
        public void AddElement_Text_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "add_element",
                ["elementType"] = "text",
                ["name"] = "TestText",
                ["parent"] = _canvasObject.name,
                ["text"] = "Hello World",
                ["fontSize"] = 24
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var textGo = GameObject.Find("TestText");
            Assert.IsNotNull(textGo);
            var text = textGo.GetComponent<Text>();
            Assert.IsNotNull(text);
            Assert.AreEqual("Hello World", text.text);
            Assert.AreEqual(24, text.fontSize);
        }

        [Test]
        public void AddElement_Button_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "add_element",
                ["elementType"] = "button",
                ["name"] = "TestButton",
                ["parent"] = _canvasObject.name,
                ["text"] = "Click Me"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var buttonGo = GameObject.Find("TestButton");
            Assert.IsNotNull(buttonGo);
            Assert.IsNotNull(buttonGo.GetComponent<Button>());
        }

        [Test]
        public void AddElement_Panel_CreatesSuccessfully()
        {
            var @params = new JObject
            {
                ["action"] = "add_element",
                ["elementType"] = "panel",
                ["name"] = "TestPanel",
                ["parent"] = _canvasObject.name
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var panelGo = GameObject.Find("TestPanel");
            Assert.IsNotNull(panelGo);
            Assert.IsNotNull(panelGo.GetComponent<Image>());
        }

        [Test]
        public void AddElement_InvalidType_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "add_element",
                ["elementType"] = "invalidtype",
                ["parent"] = _canvasObject.name
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetText_ValidText_UpdatesContent()
        {
            // Create a text element first
            var textGo = new GameObject("TextElement");
            textGo.transform.SetParent(_canvasObject.transform);
            var text = textGo.AddComponent<Text>();
            text.text = "Original";

            var @params = new JObject
            {
                ["action"] = "set_text",
                ["target"] = "TextElement",
                ["text"] = "Updated Text",
                ["fontSize"] = 30
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual("Updated Text", text.text);
            Assert.AreEqual(30, text.fontSize);
        }

        [Test]
        public void SetText_NoTextComponent_ReturnsError()
        {
            var noTextGo = new GameObject("NoText");
            noTextGo.transform.SetParent(_canvasObject.transform);

            var @params = new JObject
            {
                ["action"] = "set_text",
                ["target"] = "NoText",
                ["text"] = "Test"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetImage_ValidImage_UpdatesColor()
        {
            var imageGo = new GameObject("ImageElement");
            imageGo.transform.SetParent(_canvasObject.transform);
            var image = imageGo.AddComponent<Image>();

            var @params = new JObject
            {
                ["action"] = "set_image",
                ["target"] = "ImageElement",
                ["color"] = new JObject { ["r"] = 1.0f, ["g"] = 0.0f, ["b"] = 0.0f, ["a"] = 1.0f }
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(1.0f, image.color.r, 0.001f);
            Assert.AreEqual(0.0f, image.color.g, 0.001f);
            Assert.AreEqual(0.0f, image.color.b, 0.001f);
        }

        [Test]
        public void GetHierarchy_ValidCanvas_ReturnsHierarchy()
        {
            var @params = new JObject
            {
                ["action"] = "get_hierarchy",
                ["canvas"] = _canvasObject.name
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetRectTransform_ValidElement_UpdatesTransform()
        {
            var rectGo = new GameObject("RectElement");
            rectGo.transform.SetParent(_canvasObject.transform);
            rectGo.AddComponent<RectTransform>();

            var @params = new JObject
            {
                ["action"] = "set_rect_transform",
                ["target"] = "RectElement",
                ["anchoredPosition"] = new JArray { 100, 50 },
                ["sizeDelta"] = new JArray { 200, 100 }
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var rt = rectGo.GetComponent<RectTransform>();
            Assert.AreEqual(100f, rt.anchoredPosition.x, 0.001f);
            Assert.AreEqual(50f, rt.anchoredPosition.y, 0.001f);
            Assert.AreEqual(200f, rt.sizeDelta.x, 0.001f);
            Assert.AreEqual(100f, rt.sizeDelta.y, 0.001f);
        }

        [Test]
        public void GetCanvasInfo_ValidCanvas_ReturnsInfo()
        {
            var @params = new JObject
            {
                ["action"] = "get_canvas_info",
                ["target"] = _canvasObject.name
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void SetCanvasProperties_RenderMode_Updates()
        {
            var @params = new JObject
            {
                ["action"] = "set_canvas_properties",
                ["target"] = _canvasObject.name,
                ["sortingOrder"] = 10
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(10, _canvas.sortingOrder);
        }

        [Test]
        public void AddLayoutGroup_Vertical_AddsSuccessfully()
        {
            var layoutGo = new GameObject("LayoutElement");
            layoutGo.transform.SetParent(_canvasObject.transform);
            layoutGo.AddComponent<RectTransform>();

            var @params = new JObject
            {
                ["action"] = "add_layout_group",
                ["target"] = "LayoutElement",
                ["layoutType"] = "vertical",
                ["spacing"] = 10.0f
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var vlg = layoutGo.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(vlg);
            Assert.AreEqual(10.0f, vlg.spacing, 0.001f);
        }

        [Test]
        public void AddLayoutGroup_Horizontal_AddsSuccessfully()
        {
            var layoutGo = new GameObject("HLayoutElement");
            layoutGo.transform.SetParent(_canvasObject.transform);
            layoutGo.AddComponent<RectTransform>();

            var @params = new JObject
            {
                ["action"] = "add_layout_group",
                ["target"] = "HLayoutElement",
                ["layoutType"] = "horizontal"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsNotNull(layoutGo.GetComponent<HorizontalLayoutGroup>());
        }

        [Test]
        public void AddLayoutGroup_Grid_AddsSuccessfully()
        {
            var layoutGo = new GameObject("GridElement");
            layoutGo.transform.SetParent(_canvasObject.transform);
            layoutGo.AddComponent<RectTransform>();

            var @params = new JObject
            {
                ["action"] = "add_layout_group",
                ["target"] = "GridElement",
                ["layoutType"] = "grid",
                ["cellSize"] = new JArray { 50, 50 }
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var glg = layoutGo.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(glg);
            Assert.AreEqual(50f, glg.cellSize.x, 0.001f);
            Assert.AreEqual(50f, glg.cellSize.y, 0.001f);
        }

        [Test]
        public void SetLayout_ValidElement_SetsLayoutElement()
        {
            var layoutGo = new GameObject("LayoutTarget");
            layoutGo.transform.SetParent(_canvasObject.transform);
            layoutGo.AddComponent<RectTransform>();

            var @params = new JObject
            {
                ["action"] = "set_layout",
                ["target"] = "LayoutTarget",
                ["minWidth"] = 100.0f,
                ["preferredHeight"] = 50.0f
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var le = layoutGo.GetComponent<LayoutElement>();
            Assert.IsNotNull(le);
            Assert.AreEqual(100.0f, le.minWidth, 0.001f);
            Assert.AreEqual(50.0f, le.preferredHeight, 0.001f);
        }

        [Test]
        public void EnsureEventSystem_CreatesIfMissing()
        {
            // Clean up any existing event systems
            var existing = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            foreach (var es in existing)
            {
                UnityEngine.Object.DestroyImmediate(es.gameObject);
            }

            var @params = new JObject
            {
                ["action"] = "ensure_event_system"
            };

            var result = ManageCanvas.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);

            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            Assert.IsNotNull(eventSystem);
        }
    }
}
