using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Handles animation system operations including animator parameters, states, and playback control.
    /// </summary>
    [McpForUnityTool("manage_animator", AutoRegister = false, Description = "Controls Animator components: play states, set parameters (bool, float, int, trigger), and reset triggers.")]
    public static class ManageAnimator
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
                        return new SuccessResponse("pong", new { tool = "manage_animator" });

                    case "get_parameters":
                        return GetParameters(@params);

                    case "set_parameter":
                        return SetParameter(@params);

                    case "get_state":
                        return GetState(@params);

                    case "play_state":
                        return PlayState(@params);

                    case "crossfade":
                        return Crossfade(@params);

                    case "get_controller_info":
                        return GetControllerInfo(@params);

                    case "set_speed":
                        return SetSpeed(@params);

                    case "get_layer_info":
                        return GetLayerInfo(@params);

                    default:
                        return new ErrorResponse($"Unknown action: {action}. Valid actions: get_parameters, set_parameter, get_state, play_state, crossfade, get_controller_info, set_speed, get_layer_info");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static Animator FindAnimator(JObject @params)
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
            return go?.GetComponent<Animator>();
        }

        private static object GetParameters(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            if (animator.runtimeAnimatorController == null)
            {
                return new ErrorResponse("Animator has no controller assigned");
            }

            var parameters = new List<object>();
            foreach (var param in animator.parameters)
            {
                object currentValue = null;
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        currentValue = animator.GetFloat(param.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        currentValue = animator.GetInteger(param.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        currentValue = animator.GetBool(param.name);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        currentValue = null; // Triggers don't have a readable value
                        break;
                }

                parameters.Add(new
                {
                    name = param.name,
                    type = param.type.ToString(),
                    defaultFloat = param.defaultFloat,
                    defaultInt = param.defaultInt,
                    defaultBool = param.defaultBool,
                    currentValue = currentValue
                });
            }

            return new SuccessResponse($"Retrieved {parameters.Count} animator parameters", new
            {
                gameObject = animator.gameObject.name,
                controller = animator.runtimeAnimatorController.name,
                parameters = parameters
            });
        }

        private static object SetParameter(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            string paramName = @params["parameterName"]?.ToString() ?? @params["parameter"]?.ToString();
            if (string.IsNullOrEmpty(paramName))
            {
                return new ErrorResponse("parameterName is required");
            }

            JToken valueToken = @params["value"];
            string paramType = @params["parameterType"]?.ToString()?.ToLowerInvariant();

            // Find the parameter to determine its type if not specified
            AnimatorControllerParameter targetParam = null;
            foreach (var p in animator.parameters)
            {
                if (p.name == paramName)
                {
                    targetParam = p;
                    break;
                }
            }

            if (targetParam == null)
            {
                return new ErrorResponse($"Parameter '{paramName}' not found in animator");
            }

            Undo.RecordObject(animator, "Set Animator Parameter");

            switch (targetParam.type)
            {
                case AnimatorControllerParameterType.Float:
                    float floatValue = valueToken?.ToObject<float>() ?? 0f;
                    animator.SetFloat(paramName, floatValue);
                    return new SuccessResponse($"Set float parameter '{paramName}' to {floatValue}");

                case AnimatorControllerParameterType.Int:
                    int intValue = valueToken?.ToObject<int>() ?? 0;
                    animator.SetInteger(paramName, intValue);
                    return new SuccessResponse($"Set int parameter '{paramName}' to {intValue}");

                case AnimatorControllerParameterType.Bool:
                    bool boolValue = valueToken?.ToObject<bool>() ?? false;
                    animator.SetBool(paramName, boolValue);
                    return new SuccessResponse($"Set bool parameter '{paramName}' to {boolValue}");

                case AnimatorControllerParameterType.Trigger:
                    bool reset = @params["reset"]?.ToObject<bool>() ?? false;
                    if (reset)
                    {
                        animator.ResetTrigger(paramName);
                        return new SuccessResponse($"Reset trigger '{paramName}'");
                    }
                    else
                    {
                        animator.SetTrigger(paramName);
                        return new SuccessResponse($"Set trigger '{paramName}'");
                    }

                default:
                    return new ErrorResponse($"Unknown parameter type: {targetParam.type}");
            }
        }

        private static object GetState(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            int layerIndex = @params["layerIndex"]?.ToObject<int>() ?? 0;

            if (layerIndex < 0 || layerIndex >= animator.layerCount)
            {
                return new ErrorResponse($"Invalid layer index: {layerIndex}. Animator has {animator.layerCount} layers.");
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);

            var clips = clipInfos.Select(c => new
            {
                clipName = c.clip?.name,
                weight = c.weight
            }).ToList();

            return new SuccessResponse("Retrieved animator state info", new
            {
                gameObject = animator.gameObject.name,
                layerIndex = layerIndex,
                layerName = animator.GetLayerName(layerIndex),
                fullPathHash = stateInfo.fullPathHash,
                shortNameHash = stateInfo.shortNameHash,
                normalizedTime = stateInfo.normalizedTime,
                length = stateInfo.length,
                speed = stateInfo.speed,
                speedMultiplier = stateInfo.speedMultiplier,
                isLooping = stateInfo.loop,
                clips = clips
            });
        }

        private static object PlayState(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            string stateName = @params["stateName"]?.ToString() ?? @params["state"]?.ToString();
            if (string.IsNullOrEmpty(stateName))
            {
                return new ErrorResponse("stateName is required");
            }

            int layerIndex = @params["layerIndex"]?.ToObject<int>() ?? -1;
            float normalizedTime = @params["normalizedTime"]?.ToObject<float>() ?? 0f;

            Undo.RecordObject(animator, "Play Animator State");

            if (layerIndex >= 0)
            {
                animator.Play(stateName, layerIndex, normalizedTime);
            }
            else
            {
                animator.Play(stateName);
            }

            return new SuccessResponse($"Playing state '{stateName}'", new
            {
                stateName = stateName,
                layerIndex = layerIndex,
                normalizedTime = normalizedTime
            });
        }

        private static object Crossfade(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            string stateName = @params["stateName"]?.ToString() ?? @params["state"]?.ToString();
            if (string.IsNullOrEmpty(stateName))
            {
                return new ErrorResponse("stateName is required");
            }

            float transitionDuration = @params["transitionDuration"]?.ToObject<float>() ?? @params["duration"]?.ToObject<float>() ?? 0.25f;
            int layerIndex = @params["layerIndex"]?.ToObject<int>() ?? -1;
            float normalizedTransitionTime = @params["normalizedTransitionTime"]?.ToObject<float>() ?? 0f;
            float normalizedTimeOffset = @params["normalizedTimeOffset"]?.ToObject<float>() ?? float.NegativeInfinity;

            Undo.RecordObject(animator, "Crossfade Animator State");

            if (layerIndex >= 0)
            {
                animator.CrossFade(stateName, transitionDuration, layerIndex, normalizedTimeOffset, normalizedTransitionTime);
            }
            else
            {
                animator.CrossFade(stateName, transitionDuration);
            }

            return new SuccessResponse($"Crossfading to state '{stateName}'", new
            {
                stateName = stateName,
                transitionDuration = transitionDuration,
                layerIndex = layerIndex
            });
        }

        private static object GetControllerInfo(JObject @params)
        {
            // Can work with either an animator on a GameObject or a controller asset path
            string controllerPath = @params["controllerPath"]?.ToString();
            AnimatorController controller = null;

            if (!string.IsNullOrEmpty(controllerPath))
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null)
                {
                    return new ErrorResponse($"Controller not found at path: {controllerPath}");
                }
            }
            else
            {
                Animator animator = FindAnimator(@params);
                if (animator == null)
                {
                    return new ErrorResponse("Either target GameObject with Animator or controllerPath is required");
                }

                controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null)
                {
                    // Could be an AnimatorOverrideController
                    var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                    if (overrideController != null)
                    {
                        controller = overrideController.runtimeAnimatorController as AnimatorController;
                    }
                }

                if (controller == null)
                {
                    return new ErrorResponse("Could not retrieve AnimatorController from animator");
                }
            }

            var layers = new List<object>();
            foreach (var layer in controller.layers)
            {
                var states = new List<object>();
                if (layer.stateMachine != null)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        var transitions = state.state.transitions.Select(t => new
                        {
                            destinationState = t.destinationState?.name,
                            hasExitTime = t.hasExitTime,
                            exitTime = t.exitTime,
                            duration = t.duration,
                            hasFixedDuration = t.hasFixedDuration,
                            conditionCount = t.conditions.Length
                        }).ToList();

                        states.Add(new
                        {
                            name = state.state.name,
                            nameHash = state.state.nameHash,
                            speed = state.state.speed,
                            speedParameterActive = state.state.speedParameterActive,
                            speedParameter = state.state.speedParameter,
                            motion = state.state.motion?.name,
                            position = new { x = state.position.x, y = state.position.y },
                            transitionCount = transitions.Count,
                            transitions = transitions
                        });
                    }
                }

                layers.Add(new
                {
                    name = layer.name,
                    defaultWeight = layer.defaultWeight,
                    blendingMode = layer.blendingMode.ToString(),
                    syncedLayerIndex = layer.syncedLayerIndex,
                    iKPass = layer.iKPass,
                    stateCount = states.Count,
                    states = states
                });
            }

            var parameters = controller.parameters.Select(p => new
            {
                name = p.name,
                type = p.type.ToString(),
                defaultFloat = p.defaultFloat,
                defaultInt = p.defaultInt,
                defaultBool = p.defaultBool
            }).ToList();

            return new SuccessResponse($"Retrieved controller info for '{controller.name}'", new
            {
                name = controller.name,
                layerCount = layers.Count,
                parameterCount = parameters.Count,
                layers = layers,
                parameters = parameters
            });
        }

        private static object SetSpeed(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            float speed = @params["speed"]?.ToObject<float>() ?? 1f;

            Undo.RecordObject(animator, "Set Animator Speed");
            animator.speed = speed;

            return new SuccessResponse($"Set animator speed to {speed}", new
            {
                gameObject = animator.gameObject.name,
                speed = speed
            });
        }

        private static object GetLayerInfo(JObject @params)
        {
            Animator animator = FindAnimator(@params);
            if (animator == null)
            {
                return new ErrorResponse("Animator not found on target GameObject");
            }

            var layers = new List<object>();
            for (int i = 0; i < animator.layerCount; i++)
            {
                layers.Add(new
                {
                    index = i,
                    name = animator.GetLayerName(i),
                    weight = animator.GetLayerWeight(i)
                });
            }

            return new SuccessResponse($"Retrieved {layers.Count} animator layers", new
            {
                gameObject = animator.gameObject.name,
                layerCount = animator.layerCount,
                layers = layers
            });
        }
    }
}
