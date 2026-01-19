using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageSceneViewTests
    {
        [Test]
        public void GetCameraPosition_ReturnsPosition()
        {
            // Ensure scene view exists
            if (SceneView.lastActiveSceneView == null)
            {
                EditorWindow.GetWindow<SceneView>();
            }

            // Act
            var result = ToJObject(ManageSceneView.HandleCommand(new JObject
            {
                ["action"] = "get_camera"
            }));

            // Assert - might fail if no scene view open
            if (SceneView.lastActiveSceneView != null)
            {
                Assert.IsTrue(result.Value<bool>("success"), result.ToString());
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.IsNotNull(data["position"]);
                Assert.IsNotNull(data["rotation"]);
            }
        }

        [Test]
        public void FocusOnObject_FocusesOnGameObject()
        {
            // Arrange
            GameObject testObj = new GameObject("FocusTestObject");

            try
            {
                // Ensure scene view exists
                if (SceneView.lastActiveSceneView == null)
                {
                    EditorWindow.GetWindow<SceneView>();
                }

                // Act
                var result = ToJObject(ManageSceneView.HandleCommand(new JObject
                {
                    ["action"] = "focus_object",
                    ["objectName"] = "FocusTestObject"
                }));

                // Assert
                if (SceneView.lastActiveSceneView != null)
                {
                    Assert.IsTrue(result.Value<bool>("success"), result.ToString());
                }
            }
            finally
            {
                Object.DestroyImmediate(testObj);
            }
        }

        [Test]
        public void GetViewMode_ReturnsViewMode()
        {
            // Ensure scene view exists
            if (SceneView.lastActiveSceneView == null)
            {
                EditorWindow.GetWindow<SceneView>();
            }

            // Act
            var result = ToJObject(ManageSceneView.HandleCommand(new JObject
            {
                ["action"] = "get_view_mode"
            }));

            // Assert
            if (SceneView.lastActiveSceneView != null)
            {
                Assert.IsTrue(result.Value<bool>("success"), result.ToString());
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.IsNotNull(data["mode"]);
            }
        }
    }
}
