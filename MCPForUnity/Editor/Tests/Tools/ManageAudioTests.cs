using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MCPForUnity.Editor.Tests.Tools
{
    [TestFixture]
    public class ManageAudioTests
    {
        private GameObject _testObject;
        private AudioSource _audioSource;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestAudioObject");
            _audioSource = _testObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void HandleCommand_Ping_ReturnsSuccess()
        {
            var @params = new JObject { ["action"] = "ping" };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            Assert.AreEqual("pong", success.Message);
        }

        [Test]
        public void HandleCommand_MissingAction_ReturnsError()
        {
            var @params = new JObject();

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void HandleCommand_UnknownAction_ReturnsError()
        {
            var @params = new JObject { ["action"] = "unknown_action" };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void SetProperties_Volume_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["volume"] = 0.5f
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(0.5f, _audioSource.volume, 0.001f);
        }

        [Test]
        public void SetProperties_Pitch_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["pitch"] = 1.5f
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(1.5f, _audioSource.pitch, 0.001f);
        }

        [Test]
        public void SetProperties_Loop_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["loop"] = true
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.IsTrue(_audioSource.loop);
        }

        [Test]
        public void SetProperties_SpatialBlend_SetsValue()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["spatialBlend"] = 1.0f
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(1.0f, _audioSource.spatialBlend, 0.001f);
        }

        [Test]
        public void SetProperties_MultipleProperties_SetsAllValues()
        {
            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = _testObject.name,
                ["volume"] = 0.7f,
                ["pitch"] = 0.8f,
                ["loop"] = true,
                ["mute"] = false
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            Assert.AreEqual(0.7f, _audioSource.volume, 0.001f);
            Assert.AreEqual(0.8f, _audioSource.pitch, 0.001f);
            Assert.IsTrue(_audioSource.loop);
            Assert.IsFalse(_audioSource.mute);
        }

        [Test]
        public void SetProperties_NoAudioSource_ReturnsError()
        {
            var emptyObject = new GameObject("EmptyObject");

            var @params = new JObject
            {
                ["action"] = "set_properties",
                ["target"] = emptyObject.name,
                ["volume"] = 0.5f
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);

            UnityEngine.Object.DestroyImmediate(emptyObject);
        }

        [Test]
        public void GetSourceInfo_ValidSource_ReturnsInfo()
        {
            _audioSource.volume = 0.8f;
            _audioSource.pitch = 1.2f;

            var @params = new JObject
            {
                ["action"] = "get_source_info",
                ["target"] = _testObject.name
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void CreateSource_NewSource_CreatesSuccessfully()
        {
            var newObject = new GameObject("NewAudioObject");

            var @params = new JObject
            {
                ["action"] = "create_source",
                ["target"] = newObject.name,
                ["playOnAwake"] = false,
                ["volume"] = 0.6f
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var source = newObject.GetComponent<AudioSource>();
            Assert.IsNotNull(source);
            Assert.IsFalse(source.playOnAwake);
            Assert.AreEqual(0.6f, source.volume, 0.001f);

            UnityEngine.Object.DestroyImmediate(newObject);
        }

        [Test]
        public void CreateSource_ExistingSource_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "create_source",
                ["target"] = _testObject.name,
                ["allowMultiple"] = false
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void CreateSource_ExistingSourceWithAllowMultiple_CreatesNew()
        {
            var @params = new JObject
            {
                ["action"] = "create_source",
                ["target"] = _testObject.name,
                ["allowMultiple"] = true
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var sources = _testObject.GetComponents<AudioSource>();
            Assert.AreEqual(2, sources.Length);
        }

        [Test]
        public void GetAudioSettings_ReturnsSettings()
        {
            var @params = new JObject
            {
                ["action"] = "get_audio_settings"
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Play_NoClip_ReturnsError()
        {
            var @params = new JObject
            {
                ["action"] = "play",
                ["target"] = _testObject.name
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<ErrorResponse>(result);
        }

        [Test]
        public void Stop_ValidSource_StopsPlayback()
        {
            var @params = new JObject
            {
                ["action"] = "stop",
                ["target"] = _testObject.name
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void Pause_ValidSource_PausesPlayback()
        {
            var @params = new JObject
            {
                ["action"] = "pause",
                ["target"] = _testObject.name
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }

        [Test]
        public void UnPause_ValidSource_UnpausesPlayback()
        {
            var @params = new JObject
            {
                ["action"] = "unpause",
                ["target"] = _testObject.name
            };

            var result = ManageAudio.HandleCommand(@params);

            Assert.IsInstanceOf<SuccessResponse>(result);
        }
    }
}
