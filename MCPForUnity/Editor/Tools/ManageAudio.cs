using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles audio system operations including AudioSource control, clip info, and audio configuration.
    /// </summary>
    [McpForUnityTool("manage_audio", AutoRegister = false, Description = "Manage AudioSource components: play, pause, stop, and set properties like volume and clip.")]
    public static class ManageAudio
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
                        return new SuccessResponse("pong", new { tool = "manage_audio" });

                    case "play":
                        return Play(@params);

                    case "stop":
                        return Stop(@params);

                    case "pause":
                        return Pause(@params);

                    case "unpause":
                        return UnPause(@params);

                    case "set_properties":
                        return SetProperties(@params);

                    case "get_clip_info":
                        return GetClipInfo(@params);

                    case "create_source":
                        return CreateSource(@params);

                    case "get_source_info":
                        return GetSourceInfo(@params);

                    case "set_clip":
                        return SetClip(@params);

                    case "get_audio_settings":
                        return GetAudioSettings();

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: play, stop, pause, unpause, set_properties, get_clip_info, create_source, get_source_info, set_clip, get_audio_settings");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static AudioSource FindAudioSource(JObject @params)
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

            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            return go?.GetComponent<AudioSource>();
        }

        private static object Play(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            if (source.clip == null)
            {
                return new ErrorResponse("AudioSource has no clip assigned");
            }

            float delay = @params["delay"]?.ToObject<float>() ?? 0f;

            if (delay > 0f)
            {
                source.PlayDelayed(delay);
                return new SuccessResponse($"Playing audio '{source.clip.name}' with {delay}s delay", new
                {
                    gameObject = source.gameObject.name,
                    clipName = source.clip.name,
                    delay = delay
                });
            }
            else
            {
                source.Play();
                return new SuccessResponse($"Playing audio '{source.clip.name}'", new
                {
                    gameObject = source.gameObject.name,
                    clipName = source.clip.name
                });
            }
        }

        private static object Stop(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            source.Stop();
            return new SuccessResponse($"Stopped audio on '{source.gameObject.name}'", new
            {
                gameObject = source.gameObject.name
            });
        }

        private static object Pause(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            source.Pause();
            return new SuccessResponse($"Paused audio on '{source.gameObject.name}'", new
            {
                gameObject = source.gameObject.name
            });
        }

        private static object UnPause(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            source.UnPause();
            return new SuccessResponse($"Unpaused audio on '{source.gameObject.name}'", new
            {
                gameObject = source.gameObject.name
            });
        }

        private static object SetProperties(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            Undo.RecordObject(source, "Set AudioSource Properties");

            var changes = new List<string>();

            if (@params["volume"] != null)
            {
                source.volume = Mathf.Clamp01(@params["volume"].ToObject<float>());
                changes.Add($"volume={source.volume}");
            }

            if (@params["pitch"] != null)
            {
                source.pitch = @params["pitch"].ToObject<float>();
                changes.Add($"pitch={source.pitch}");
            }

            if (@params["loop"] != null)
            {
                source.loop = @params["loop"].ToObject<bool>();
                changes.Add($"loop={source.loop}");
            }

            if (@params["mute"] != null)
            {
                source.mute = @params["mute"].ToObject<bool>();
                changes.Add($"mute={source.mute}");
            }

            if (@params["spatialBlend"] != null)
            {
                source.spatialBlend = Mathf.Clamp01(@params["spatialBlend"].ToObject<float>());
                changes.Add($"spatialBlend={source.spatialBlend}");
            }

            if (@params["minDistance"] != null)
            {
                source.minDistance = @params["minDistance"].ToObject<float>();
                changes.Add($"minDistance={source.minDistance}");
            }

            if (@params["maxDistance"] != null)
            {
                source.maxDistance = @params["maxDistance"].ToObject<float>();
                changes.Add($"maxDistance={source.maxDistance}");
            }

            if (@params["playOnAwake"] != null)
            {
                source.playOnAwake = @params["playOnAwake"].ToObject<bool>();
                changes.Add($"playOnAwake={source.playOnAwake}");
            }

            if (@params["priority"] != null)
            {
                source.priority = Mathf.Clamp(@params["priority"].ToObject<int>(), 0, 256);
                changes.Add($"priority={source.priority}");
            }

            if (@params["stereoPan"] != null)
            {
                source.panStereo = Mathf.Clamp(@params["stereoPan"].ToObject<float>(), -1f, 1f);
                changes.Add($"stereoPan={source.panStereo}");
            }

            if (@params["reverbZoneMix"] != null)
            {
                source.reverbZoneMix = Mathf.Clamp01(@params["reverbZoneMix"].ToObject<float>());
                changes.Add($"reverbZoneMix={source.reverbZoneMix}");
            }

            if (@params["dopplerLevel"] != null)
            {
                source.dopplerLevel = Mathf.Clamp(@params["dopplerLevel"].ToObject<float>(), 0f, 5f);
                changes.Add($"dopplerLevel={source.dopplerLevel}");
            }

            if (@params["spread"] != null)
            {
                source.spread = Mathf.Clamp(@params["spread"].ToObject<float>(), 0f, 360f);
                changes.Add($"spread={source.spread}");
            }

            if (@params["rolloffMode"] != null)
            {
                string mode = @params["rolloffMode"].ToString().ToLowerInvariant();
                switch (mode)
                {
                    case "logarithmic":
                        source.rolloffMode = AudioRolloffMode.Logarithmic;
                        break;
                    case "linear":
                        source.rolloffMode = AudioRolloffMode.Linear;
                        break;
                    case "custom":
                        source.rolloffMode = AudioRolloffMode.Custom;
                        break;
                }
                changes.Add($"rolloffMode={source.rolloffMode}");
            }

            EditorUtility.SetDirty(source);

            if (changes.Count == 0)
            {
                return new SuccessResponse("No properties changed", new { gameObject = source.gameObject.name });
            }

            return new SuccessResponse($"Updated AudioSource properties: {string.Join(", ", changes)}", new
            {
                gameObject = source.gameObject.name,
                changes = changes
            });
        }

        private static object GetClipInfo(JObject @params)
        {
            string clipPath = @params["clipPath"]?.ToString();
            AudioClip clip = null;

            if (!string.IsNullOrEmpty(clipPath))
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                {
                    return new ErrorResponse($"AudioClip not found at path: {clipPath}");
                }
            }
            else
            {
                AudioSource source = FindAudioSource(@params);
                if (source == null || source.clip == null)
                {
                    return new ErrorResponse("Either clipPath or target with AudioSource containing a clip is required");
                }
                clip = source.clip;
            }

            return new SuccessResponse($"Retrieved clip info for '{clip.name}'", new
            {
                name = clip.name,
                length = clip.length,
                channels = clip.channels,
                frequency = clip.frequency,
                samples = clip.samples,
                loadState = clip.loadState.ToString(),
                loadType = clip.loadType.ToString(),
                ambisonic = clip.ambisonic,
                preloadAudioData = clip.preloadAudioData
            });
        }

        private static object CreateSource(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("target is required");
            }

            var goInstruction = new JObject { ["find"] = target };
            string searchMethod = @params["searchMethod"]?.ToString();
            if (!string.IsNullOrEmpty(searchMethod))
            {
                goInstruction["method"] = searchMethod;
            }

            GameObject go = ObjectResolver.Resolve(goInstruction, typeof(GameObject)) as GameObject;
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            AudioSource existingSource = go.GetComponent<AudioSource>();
            if (existingSource != null && !(@params["allowMultiple"]?.ToObject<bool>() ?? false))
            {
                return new ErrorResponse($"AudioSource already exists on '{go.name}'. Set allowMultiple=true to add another.");
            }

            Undo.RecordObject(go, "Add AudioSource");
            AudioSource newSource = Undo.AddComponent<AudioSource>(go);

            // Optionally set initial properties
            if (@params["clipPath"] != null)
            {
                string clipPath = @params["clipPath"].ToString();
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    newSource.clip = clip;
                }
            }

            if (@params["playOnAwake"] != null)
            {
                newSource.playOnAwake = @params["playOnAwake"].ToObject<bool>();
            }
            else
            {
                newSource.playOnAwake = false; // Default to false for manually created sources
            }

            if (@params["loop"] != null)
            {
                newSource.loop = @params["loop"].ToObject<bool>();
            }

            if (@params["volume"] != null)
            {
                newSource.volume = Mathf.Clamp01(@params["volume"].ToObject<float>());
            }

            if (@params["spatialBlend"] != null)
            {
                newSource.spatialBlend = Mathf.Clamp01(@params["spatialBlend"].ToObject<float>());
            }

            EditorUtility.SetDirty(go);

            return new SuccessResponse($"Created AudioSource on '{go.name}'", new
            {
                gameObject = go.name,
                clip = newSource.clip?.name,
                playOnAwake = newSource.playOnAwake,
                loop = newSource.loop,
                volume = newSource.volume,
                spatialBlend = newSource.spatialBlend
            });
        }

        private static object GetSourceInfo(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            return new SuccessResponse($"Retrieved AudioSource info for '{source.gameObject.name}'", new
            {
                gameObject = source.gameObject.name,
                clip = source.clip?.name,
                isPlaying = source.isPlaying,
                time = source.time,
                timeSamples = source.timeSamples,
                volume = source.volume,
                pitch = source.pitch,
                loop = source.loop,
                mute = source.mute,
                playOnAwake = source.playOnAwake,
                spatialBlend = source.spatialBlend,
                minDistance = source.minDistance,
                maxDistance = source.maxDistance,
                priority = source.priority,
                panStereo = source.panStereo,
                reverbZoneMix = source.reverbZoneMix,
                dopplerLevel = source.dopplerLevel,
                spread = source.spread,
                rolloffMode = source.rolloffMode.ToString()
            });
        }

        private static object SetClip(JObject @params)
        {
            AudioSource source = FindAudioSource(@params);
            if (source == null)
            {
                return new ErrorResponse("AudioSource not found on target GameObject");
            }

            string clipPath = @params["clipPath"]?.ToString();
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("clipPath is required");
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"AudioClip not found at path: {clipPath}");
            }

            Undo.RecordObject(source, "Set AudioSource Clip");
            source.clip = clip;
            EditorUtility.SetDirty(source);

            return new SuccessResponse($"Set clip '{clip.name}' on AudioSource", new
            {
                gameObject = source.gameObject.name,
                clipName = clip.name,
                clipPath = clipPath
            });
        }

        private static object GetAudioSettings()
        {
            var config = AudioSettings.GetConfiguration();

            return new SuccessResponse("Retrieved audio settings", new
            {
                sampleRate = AudioSettings.outputSampleRate,
                speakerMode = AudioSettings.speakerMode.ToString(),
                dspBufferSize = config.dspBufferSize,
                numRealVoices = config.numRealVoices,
                numVirtualVoices = config.numVirtualVoices
            });
        }
    }
}
