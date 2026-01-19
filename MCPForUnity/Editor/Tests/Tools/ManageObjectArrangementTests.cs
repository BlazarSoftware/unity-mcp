using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageObjectArrangementTests
    {
        private GameObject _sourceObject;

        [SetUp]
        public void SetUp()
        {
            _sourceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _sourceObject.name = "TestSource";
            _sourceObject.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all test objects
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Test") || go.name.StartsWith("Array") ||
                    go.name.StartsWith("Grid") || go.name.StartsWith("Circle"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void ArrayLinear_DefaultParams_CreatesLinearArray()
        {
            var @params = new JObject
            {
                ["action"] = "array_linear",
                ["source"] = _sourceObject.name,
                ["count"] = 5,
                ["spacing"] = 2f
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var copies = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(g => g.name.StartsWith("TestSource_"))
                .ToArray();
            Assert.AreEqual(4, copies.Length); // 4 copies (not including original)
        }

        [Test]
        public void ArrayLinear_WithDirection_SetsCorrectPositions()
        {
            var @params = new JObject
            {
                ["action"] = "array_linear",
                ["source"] = _sourceObject.name,
                ["count"] = 3,
                ["spacing"] = 5f,
                ["direction"] = new JArray { 1, 0, 0 }
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var copy1 = GameObject.Find("TestSource_1");
            var copy2 = GameObject.Find("TestSource_2");
            Assert.IsNotNull(copy1);
            Assert.IsNotNull(copy2);
            Assert.AreEqual(5f, copy1.transform.position.x, 0.01f);
            Assert.AreEqual(10f, copy2.transform.position.x, 0.01f);
        }

        [Test]
        public void ArrayLinear_WithParent_SetsParent()
        {
            var @params = new JObject
            {
                ["action"] = "array_linear",
                ["source"] = _sourceObject.name,
                ["count"] = 3,
                ["spacing"] = 2f,
                ["parent"] = "ArrayParent"
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var parent = GameObject.Find("ArrayParent");
            Assert.IsNotNull(parent);
            Assert.GreaterOrEqual(parent.transform.childCount, 2);
        }

        [Test]
        public void ArrayGrid_DefaultParams_CreatesGrid()
        {
            var @params = new JObject
            {
                ["action"] = "array_grid",
                ["source"] = _sourceObject.name,
                ["countX"] = 2,
                ["countY"] = 1,
                ["countZ"] = 2
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            // Grid creates countX * countY * countZ - 1 copies (minus center)
        }

        [Test]
        public void ArrayCircular_DefaultParams_CreatesCircularArray()
        {
            var @params = new JObject
            {
                ["action"] = "array_circular",
                ["source"] = _sourceObject.name,
                ["count"] = 8,
                ["radius"] = 5f
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var copies = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(g => g.name.StartsWith("TestSource_"))
                .ToArray();
            Assert.AreEqual(8, copies.Length);
        }

        [Test]
        public void ArrayCircular_WithArc_CreatesArcArray()
        {
            var @params = new JObject
            {
                ["action"] = "array_circular",
                ["source"] = _sourceObject.name,
                ["count"] = 4,
                ["radius"] = 5f,
                ["startAngle"] = 0f,
                ["endAngle"] = 180f
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void AlignObjects_MinAlignment_AlignsToMinimum()
        {
            // Create test objects at different positions
            var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj1.name = "TestAlign1";
            obj1.transform.position = new Vector3(5, 0, 0);

            var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj2.name = "TestAlign2";
            obj2.transform.position = new Vector3(10, 0, 0);

            var obj3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj3.name = "TestAlign3";
            obj3.transform.position = new Vector3(15, 0, 0);

            var @params = new JObject
            {
                ["action"] = "align_objects",
                ["targets"] = new JArray { "TestAlign1", "TestAlign2", "TestAlign3" },
                ["axis"] = "x",
                ["alignTo"] = "min"
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(5f, obj2.transform.position.x, 0.01f);
            Assert.AreEqual(5f, obj3.transform.position.x, 0.01f);
        }

        [Test]
        public void DistributeObjects_EvenSpacing_DistributesEvenly()
        {
            // Create test objects
            var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj1.name = "TestDist1";
            obj1.transform.position = new Vector3(0, 0, 0);

            var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj2.name = "TestDist2";
            obj2.transform.position = new Vector3(1, 0, 0); // Will be repositioned

            var obj3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj3.name = "TestDist3";
            obj3.transform.position = new Vector3(10, 0, 0);

            var @params = new JObject
            {
                ["action"] = "distribute_objects",
                ["targets"] = new JArray { "TestDist1", "TestDist2", "TestDist3" },
                ["axis"] = "x"
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(5f, obj2.transform.position.x, 0.01f); // Should be at midpoint
        }

        [Test]
        public void SnapToGround_NoGround_ReturnsNotSnapped()
        {
            _sourceObject.transform.position = new Vector3(0, 100, 0);

            var @params = new JObject
            {
                ["action"] = "snap_to_ground",
                ["targets"] = new JArray { _sourceObject.name },
                ["maxDistance"] = 10f
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void RandomizeTransform_WithPositionRange_ModifiesPositions()
        {
            var originalPos = _sourceObject.transform.position;

            var @params = new JObject
            {
                ["action"] = "randomize_transform",
                ["targets"] = new JArray { _sourceObject.name },
                ["positionRange"] = new JArray { 5f, 0f, 5f },
                ["seed"] = 12345 // Fixed seed for reproducibility
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            // Position should have changed (within range)
            Assert.AreNotEqual(originalPos, _sourceObject.transform.position);
        }

        [Test]
        public void RandomizeTransform_WithRotationRange_ModifiesRotation()
        {
            var originalRot = _sourceObject.transform.eulerAngles;

            var @params = new JObject
            {
                ["action"] = "randomize_transform",
                ["targets"] = new JArray { _sourceObject.name },
                ["rotationRange"] = new JArray { 0f, 45f, 0f },
                ["seed"] = 12345
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void ArrayLinear_InvalidSource_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "array_linear",
                ["source"] = "NonExistentObject",
                ["count"] = 5
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void AlignObjects_InsufficientTargets_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "align_objects",
                ["targets"] = new JArray { _sourceObject.name },
                ["axis"] = "x"
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void DistributeObjects_InsufficientTargets_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "distribute_objects",
                ["targets"] = new JArray { _sourceObject.name },
                ["axis"] = "x"
            };

            var result = ManageObjectArrangement.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }
    }
}
