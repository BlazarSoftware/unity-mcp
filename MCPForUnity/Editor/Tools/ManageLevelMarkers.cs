using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Runtime.Components;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages level design markers including spawn points, waypoints, triggers, checkpoints, and objectives.
    /// </summary>
    [McpForUnityTool("manage_level_markers", AutoRegister = false,
        Description = "Manage level design markers for gameplay logic. Actions: create_spawn_point (player/enemy spawn with team/priority), create_waypoint (AI navigation waypoint), create_trigger_volume (box/sphere trigger with event name), create_checkpoint (save/respawn point), create_objective_marker (goal/quest marker), get_markers (list markers by type), connect_waypoints (link waypoints into path), validate_markers (check marker placement). Markers are organized under '_LevelMarkers' parent and include editor gizmos.")]
    public static class ManageLevelMarkers
    {
        private const string MarkersParentName = "_LevelMarkers";

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: create_spawn_point, create_waypoint, create_trigger_volume, create_checkpoint, create_objective_marker, get_markers, connect_waypoints, validate_markers, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_level_markers" }),
                    "create_spawn_point" => CreateSpawnPoint(@params),
                    "create_waypoint" => CreateWaypoint(@params),
                    "create_trigger_volume" => CreateTriggerVolume(@params),
                    "create_checkpoint" => CreateCheckpoint(@params),
                    "create_objective_marker" => CreateObjectiveMarker(@params),
                    "get_markers" => GetMarkers(@params),
                    "connect_waypoints" => ConnectWaypoints(@params),
                    "validate_markers" => ValidateMarkers(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: create_spawn_point, create_waypoint, create_trigger_volume, create_checkpoint, create_objective_marker, get_markers, connect_waypoints, validate_markers, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Creates a spawn point marker.
        /// </summary>
        private static object CreateSpawnPoint(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "SpawnPoint";
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string spawnTypeStr = @params["spawnType"]?.ToString()?.ToLowerInvariant() ?? "player";
            int team = @params["team"]?.ToObject<int>() ?? 0;
            int priority = @params["priority"]?.ToObject<int>() ?? 0;
            float respawnDelay = @params["respawnDelay"]?.ToObject<float>() ?? 0f;
            string spawnId = @params["spawnId"]?.ToString();
            string spawnGroup = @params["spawnGroup"]?.ToString();

            SpawnPoint.SpawnType spawnType = spawnTypeStr switch
            {
                "player" => SpawnPoint.SpawnType.Player,
                "enemy" => SpawnPoint.SpawnType.Enemy,
                "item" => SpawnPoint.SpawnType.Item,
                "vehicle" => SpawnPoint.SpawnType.Vehicle,
                "npc" => SpawnPoint.SpawnType.NPC,
                "projectile" => SpawnPoint.SpawnType.Projectile,
                _ => SpawnPoint.SpawnType.Player
            };

            GameObject markerObj = CreateMarkerObject(name, position, rotation, "SpawnPoints");
            SpawnPoint spawnPoint = markerObj.AddComponent<SpawnPoint>();

            spawnPoint.Type = spawnType;
            spawnPoint.Team = team;
            spawnPoint.Priority = priority;
            spawnPoint.RespawnDelay = respawnDelay;
            spawnPoint.SpawnId = string.IsNullOrEmpty(spawnId) ? markerObj.GetInstanceID().ToString() : spawnId;
            spawnPoint.SpawnGroup = spawnGroup;

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Spawn Point '{name}'");
            Selection.activeGameObject = markerObj;

            return new SuccessResponse($"Created spawn point '{name}' ({spawnType}, Team {team})",
                GetMarkerData(markerObj, "SpawnPoint"));
        }

        /// <summary>
        /// Creates a waypoint marker.
        /// </summary>
        private static object CreateWaypoint(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "Waypoint";
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string actionStr = @params["waypointAction"]?.ToString()?.ToLowerInvariant() ?? "none";
            float waitTime = @params["waitTime"]?.ToObject<float>() ?? 0f;
            float speedModifier = @params["speedModifier"]?.ToObject<float>() ?? 1f;
            string waypointId = @params["waypointId"]?.ToString();
            string waypointGroup = @params["waypointGroup"]?.ToString();

            Waypoint.WaypointAction waypointAction = actionStr switch
            {
                "patrol" => Waypoint.WaypointAction.Patrol,
                "guard" => Waypoint.WaypointAction.Guard,
                "investigate" => Waypoint.WaypointAction.Investigate,
                "wait" => Waypoint.WaypointAction.Wait,
                "lookaround" => Waypoint.WaypointAction.LookAround,
                "crouch" => Waypoint.WaypointAction.Crouch,
                "sprint" => Waypoint.WaypointAction.Sprint,
                "custom" => Waypoint.WaypointAction.Custom,
                _ => Waypoint.WaypointAction.None
            };

            GameObject markerObj = CreateMarkerObject(name, position, rotation, "Waypoints");
            Waypoint waypoint = markerObj.AddComponent<Waypoint>();

            waypoint.Action = waypointAction;
            waypoint.WaitTime = waitTime;
            waypoint.SpeedModifier = speedModifier;
            waypoint.WaypointId = string.IsNullOrEmpty(waypointId) ? markerObj.GetInstanceID().ToString() : waypointId;
            waypoint.WaypointGroup = waypointGroup;

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Waypoint '{name}'");
            Selection.activeGameObject = markerObj;

            return new SuccessResponse($"Created waypoint '{name}' ({waypointAction})",
                GetMarkerData(markerObj, "Waypoint"));
        }

        /// <summary>
        /// Creates a trigger volume.
        /// </summary>
        private static object CreateTriggerVolume(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "TriggerVolume";
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string eventName = @params["eventName"]?.ToString() ?? "OnTrigger";
            string shape = @params["shape"]?.ToString()?.ToLowerInvariant() ?? "box";
            Vector3 size = VectorParsing.ParseVector3OrDefault(@params["size"], Vector3.one * 2);
            float radius = @params["radius"]?.ToObject<float>() ?? 2f;
            string modeStr = @params["mode"]?.ToString()?.ToLowerInvariant() ?? "onenter";
            JArray activatorTagsArray = @params["activatorTags"] as JArray;

            TriggerVolume.TriggerMode mode = modeStr switch
            {
                "onexit" => TriggerVolume.TriggerMode.OnExit,
                "onstay" => TriggerVolume.TriggerMode.OnStay,
                "onenterandexit" => TriggerVolume.TriggerMode.OnEnterAndExit,
                _ => TriggerVolume.TriggerMode.OnEnter
            };

            GameObject markerObj = CreateMarkerObject(name, position, rotation, "Triggers");

            // Add collider based on shape
            if (shape == "sphere")
            {
                SphereCollider collider = markerObj.AddComponent<SphereCollider>();
                collider.radius = radius;
                collider.isTrigger = true;
            }
            else
            {
                BoxCollider collider = markerObj.AddComponent<BoxCollider>();
                collider.size = size;
                collider.isTrigger = true;
            }

            TriggerVolume trigger = markerObj.AddComponent<TriggerVolume>();
            trigger.EventName = eventName;
            trigger.Mode = mode;

            if (activatorTagsArray != null)
            {
                trigger.ActivatorTags = activatorTagsArray.Select(t => t.ToString()).ToArray();
            }

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Trigger Volume '{name}'");
            Selection.activeGameObject = markerObj;

            return new SuccessResponse($"Created trigger volume '{name}' ({eventName}, {mode})",
                GetMarkerData(markerObj, "TriggerVolume"));
        }

        /// <summary>
        /// Creates a checkpoint marker.
        /// </summary>
        private static object CreateCheckpoint(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "Checkpoint";
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string typeStr = @params["checkpointType"]?.ToString()?.ToLowerInvariant() ?? "auto";
            int sequenceOrder = @params["sequenceOrder"]?.ToObject<int>() ?? 0;
            string displayName = @params["displayName"]?.ToString();
            string checkpointId = @params["checkpointId"]?.ToString();

            Checkpoint.CheckpointType checkpointType = typeStr switch
            {
                "manual" => Checkpoint.CheckpointType.Manual,
                "onetime" => Checkpoint.CheckpointType.OneTime,
                "persistent" => Checkpoint.CheckpointType.Persistent,
                _ => Checkpoint.CheckpointType.Auto
            };

            GameObject markerObj = CreateMarkerObject(name, position, rotation, "Checkpoints");

            // Add trigger collider for auto checkpoints
            if (checkpointType == Checkpoint.CheckpointType.Auto || checkpointType == Checkpoint.CheckpointType.OneTime)
            {
                BoxCollider collider = markerObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 3f, 2f);
                collider.isTrigger = true;
            }

            Checkpoint checkpoint = markerObj.AddComponent<Checkpoint>();
            checkpoint.Type = checkpointType;
            checkpoint.SequenceOrder = sequenceOrder;
            checkpoint.DisplayName = displayName ?? name;
            checkpoint.CheckpointId = string.IsNullOrEmpty(checkpointId) ? markerObj.GetInstanceID().ToString() : checkpointId;

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Checkpoint '{name}'");
            Selection.activeGameObject = markerObj;

            return new SuccessResponse($"Created checkpoint '{name}' (#{sequenceOrder}, {checkpointType})",
                GetMarkerData(markerObj, "Checkpoint"));
        }

        /// <summary>
        /// Creates an objective marker.
        /// </summary>
        private static object CreateObjectiveMarker(JObject @params)
        {
            string name = @params["name"]?.ToString() ?? "Objective";
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string displayName = @params["displayName"]?.ToString() ?? "Objective";
            string description = @params["description"]?.ToString();
            string typeStr = @params["objectiveType"]?.ToString()?.ToLowerInvariant() ?? "primary";
            int order = @params["order"]?.ToObject<int>() ?? 0;
            int pointValue = @params["pointValue"]?.ToObject<int>() ?? 100;
            bool isOptional = @params["isOptional"]?.ToObject<bool>() ?? false;
            float completionRadius = @params["completionRadius"]?.ToObject<float>() ?? 2f;
            string objectiveId = @params["objectiveId"]?.ToString();

            ObjectiveMarker.ObjectiveType objectiveType = typeStr switch
            {
                "secondary" => ObjectiveMarker.ObjectiveType.Secondary,
                "bonus" => ObjectiveMarker.ObjectiveType.Bonus,
                "collectible" => ObjectiveMarker.ObjectiveType.Collectible,
                "interaction" => ObjectiveMarker.ObjectiveType.Interaction,
                "escort" => ObjectiveMarker.ObjectiveType.Escort,
                "elimination" => ObjectiveMarker.ObjectiveType.Elimination,
                "survival" => ObjectiveMarker.ObjectiveType.Survival,
                "custom" => ObjectiveMarker.ObjectiveType.Custom,
                _ => ObjectiveMarker.ObjectiveType.Primary
            };

            GameObject markerObj = CreateMarkerObject(name, position, rotation, "Objectives");

            // Add trigger for proximity completion
            SphereCollider collider = markerObj.AddComponent<SphereCollider>();
            collider.radius = completionRadius;
            collider.isTrigger = true;

            ObjectiveMarker objective = markerObj.AddComponent<ObjectiveMarker>();
            objective.Type = objectiveType;
            objective.DisplayName = displayName;
            objective.Description = description;
            objective.Order = order;
            objective.PointValue = pointValue;
            objective.IsOptional = isOptional;
            objective.CompletionRadius = completionRadius;
            objective.ObjectiveId = string.IsNullOrEmpty(objectiveId) ? markerObj.GetInstanceID().ToString() : objectiveId;

            Undo.RegisterCreatedObjectUndo(markerObj, $"Create Objective Marker '{name}'");
            Selection.activeGameObject = markerObj;

            return new SuccessResponse($"Created objective marker '{displayName}' ({objectiveType})",
                GetMarkerData(markerObj, "ObjectiveMarker"));
        }

        /// <summary>
        /// Gets all markers of a specific type.
        /// </summary>
        private static object GetMarkers(JObject @params)
        {
            string markerType = @params["markerType"]?.ToString()?.ToLowerInvariant() ?? "all";
            string group = @params["group"]?.ToString();

            var results = new List<object>();

            switch (markerType)
            {
                case "spawn":
                case "spawnpoint":
                    var spawns = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                    foreach (var spawn in spawns)
                    {
                        if (string.IsNullOrEmpty(group) || spawn.SpawnGroup == group)
                        {
                            results.Add(GetMarkerData(spawn.gameObject, "SpawnPoint"));
                        }
                    }
                    break;

                case "waypoint":
                    var waypoints = UnityEngine.Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
                    foreach (var wp in waypoints)
                    {
                        if (string.IsNullOrEmpty(group) || wp.WaypointGroup == group)
                        {
                            results.Add(GetMarkerData(wp.gameObject, "Waypoint"));
                        }
                    }
                    break;

                case "trigger":
                case "triggervolume":
                    var triggers = UnityEngine.Object.FindObjectsByType<TriggerVolume>(FindObjectsSortMode.None);
                    foreach (var trigger in triggers)
                    {
                        if (string.IsNullOrEmpty(group) || trigger.TriggerGroup == group)
                        {
                            results.Add(GetMarkerData(trigger.gameObject, "TriggerVolume"));
                        }
                    }
                    break;

                case "checkpoint":
                    var checkpoints = UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                    foreach (var cp in checkpoints)
                    {
                        results.Add(GetMarkerData(cp.gameObject, "Checkpoint"));
                    }
                    break;

                case "objective":
                case "objectivemarker":
                    var objectives = UnityEngine.Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None);
                    foreach (var obj in objectives)
                    {
                        if (string.IsNullOrEmpty(group) || obj.ObjectiveGroup == group)
                        {
                            results.Add(GetMarkerData(obj.gameObject, "ObjectiveMarker"));
                        }
                    }
                    break;

                case "all":
                default:
                    // Get all marker types
                    foreach (var spawn in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                        results.Add(GetMarkerData(spawn.gameObject, "SpawnPoint"));
                    foreach (var wp in UnityEngine.Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
                        results.Add(GetMarkerData(wp.gameObject, "Waypoint"));
                    foreach (var trigger in UnityEngine.Object.FindObjectsByType<TriggerVolume>(FindObjectsSortMode.None))
                        results.Add(GetMarkerData(trigger.gameObject, "TriggerVolume"));
                    foreach (var cp in UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                        results.Add(GetMarkerData(cp.gameObject, "Checkpoint"));
                    foreach (var obj in UnityEngine.Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None))
                        results.Add(GetMarkerData(obj.gameObject, "ObjectiveMarker"));
                    break;
            }

            return new SuccessResponse($"Found {results.Count} markers of type '{markerType}'",
                new { count = results.Count, markers = results });
        }

        /// <summary>
        /// Connects waypoints into a path.
        /// </summary>
        private static object ConnectWaypoints(JObject @params)
        {
            JArray waypointsArray = @params["waypoints"] as JArray;
            if (waypointsArray == null || waypointsArray.Count < 2)
            {
                return new ErrorResponse("At least 2 waypoints required. Provide 'waypoints' as array of names/IDs.");
            }

            bool loop = @params["loop"]?.ToObject<bool>() ?? false;
            bool bidirectional = @params["bidirectional"]?.ToObject<bool>() ?? true;

            var waypoints = new List<Waypoint>();

            foreach (var wpToken in waypointsArray)
            {
                string wpStr = wpToken.ToString();
                Waypoint wp = null;

                // Try as instance ID
                if (int.TryParse(wpStr, out int instanceId))
                {
                    var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                    if (obj != null)
                        wp = obj.GetComponent<Waypoint>();
                }

                // Try as name
                if (wp == null)
                {
                    var obj = GameObject.Find(wpStr);
                    if (obj != null)
                        wp = obj.GetComponent<Waypoint>();
                }

                if (wp != null)
                    waypoints.Add(wp);
            }

            if (waypoints.Count < 2)
            {
                return new ErrorResponse($"Could not resolve at least 2 valid waypoints. Found {waypoints.Count}.");
            }

            // Connect waypoints in sequence
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Undo.RecordObject(waypoints[i], "Connect Waypoints");
                waypoints[i].Bidirectional = bidirectional;
                waypoints[i].AddConnection(waypoints[i + 1]);
            }

            // Close the loop if requested
            if (loop)
            {
                Undo.RecordObject(waypoints[waypoints.Count - 1], "Connect Waypoints Loop");
                waypoints[waypoints.Count - 1].AddConnection(waypoints[0]);
            }

            return new SuccessResponse($"Connected {waypoints.Count} waypoints into path (loop: {loop})",
                new
                {
                    count = waypoints.Count,
                    loop = loop,
                    bidirectional = bidirectional,
                    waypoints = waypoints.Select(wp => wp.name).ToList()
                });
        }

        /// <summary>
        /// Validates marker placement in the scene.
        /// </summary>
        private static object ValidateMarkers(JObject @params)
        {
            var issues = new List<object>();

            // Check spawn points
            var spawns = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            var playerSpawns = spawns.Where(s => s.Type == SpawnPoint.SpawnType.Player).ToList();

            if (playerSpawns.Count == 0)
            {
                issues.Add(new { type = "warning", message = "No player spawn points found in scene" });
            }

            // Check for duplicate spawn IDs
            var spawnIds = spawns.Select(s => s.SpawnId).Where(id => !string.IsNullOrEmpty(id));
            var duplicateSpawnIds = spawnIds.GroupBy(id => id).Where(g => g.Count() > 1);
            foreach (var dup in duplicateSpawnIds)
            {
                issues.Add(new { type = "error", message = $"Duplicate spawn ID: {dup.Key}" });
            }

            // Check waypoints for disconnected paths
            var waypoints = UnityEngine.Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
            foreach (var wp in waypoints)
            {
                if (wp.Connections.Count == 0)
                {
                    issues.Add(new { type = "warning", message = $"Waypoint '{wp.name}' has no connections", objectName = wp.name });
                }

                // Check for null connections
                if (wp.Connections.Any(c => c == null))
                {
                    issues.Add(new { type = "error", message = $"Waypoint '{wp.name}' has null connections", objectName = wp.name });
                }
            }

            // Check checkpoints for duplicate sequence orders
            var checkpoints = UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            var duplicateOrders = checkpoints.GroupBy(c => c.SequenceOrder).Where(g => g.Count() > 1);
            foreach (var dup in duplicateOrders)
            {
                issues.Add(new { type = "warning", message = $"Multiple checkpoints with sequence order {dup.Key}" });
            }

            // Check objectives for missing prerequisites
            var objectives = UnityEngine.Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None);
            var objectiveIds = new HashSet<string>(objectives.Select(o => o.ObjectiveId).Where(id => !string.IsNullOrEmpty(id)));

            foreach (var obj in objectives)
            {
                if (obj.Prerequisites != null)
                {
                    foreach (var prereq in obj.Prerequisites)
                    {
                        if (!objectiveIds.Contains(prereq))
                        {
                            issues.Add(new { type = "error", message = $"Objective '{obj.name}' has unknown prerequisite: {prereq}", objectName = obj.name });
                        }
                    }
                }
            }

            // Check triggers for missing event handlers (warning only)
            var triggers = UnityEngine.Object.FindObjectsByType<TriggerVolume>(FindObjectsSortMode.None);
            foreach (var trigger in triggers)
            {
                if (trigger.OnTriggerActivated.GetPersistentEventCount() == 0 &&
                    trigger.OnEntityEntered.GetPersistentEventCount() == 0 &&
                    trigger.OnEntityExited.GetPersistentEventCount() == 0)
                {
                    issues.Add(new { type = "info", message = $"Trigger '{trigger.name}' has no event handlers assigned", objectName = trigger.name });
                }
            }

            string status = issues.Count == 0 ? "Valid" : issues.Any(i => ((dynamic)i).type == "error") ? "Invalid" : "Warnings";

            return new SuccessResponse($"Marker validation complete: {status}",
                new
                {
                    status = status,
                    totalMarkers = spawns.Length + waypoints.Length + checkpoints.Length + objectives.Length + triggers.Length,
                    issueCount = issues.Count,
                    issues = issues
                });
        }

        #region Helper Methods

        private static GameObject GetOrCreateMarkersParent()
        {
            GameObject parent = GameObject.Find(MarkersParentName);
            if (parent == null)
            {
                parent = new GameObject(MarkersParentName);
                Undo.RegisterCreatedObjectUndo(parent, "Create Markers Parent");
            }
            return parent;
        }

        private static Transform GetOrCreateCategory(string categoryName)
        {
            GameObject parent = GetOrCreateMarkersParent();
            Transform category = parent.transform.Find(categoryName);

            if (category == null)
            {
                GameObject categoryObj = new GameObject(categoryName);
                categoryObj.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(categoryObj, $"Create Category '{categoryName}'");
                category = categoryObj.transform;
            }

            return category;
        }

        private static GameObject CreateMarkerObject(string name, Vector3 position, Vector3 rotation, string category)
        {
            GameObject markerObj = new GameObject(name);
            markerObj.transform.position = position;
            markerObj.transform.eulerAngles = rotation;
            markerObj.transform.SetParent(GetOrCreateCategory(category), true);
            return markerObj;
        }

        private static object GetMarkerData(GameObject go, string markerType)
        {
            var data = new Dictionary<string, object>
            {
                { "name", go.name },
                { "instanceId", go.GetInstanceID() },
                { "type", markerType },
                { "position", new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z } },
                { "rotation", new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z } }
            };

            // Add type-specific data
            switch (markerType)
            {
                case "SpawnPoint":
                    var spawn = go.GetComponent<SpawnPoint>();
                    if (spawn != null)
                    {
                        data["spawnType"] = spawn.Type.ToString();
                        data["team"] = spawn.Team;
                        data["priority"] = spawn.Priority;
                        data["spawnId"] = spawn.SpawnId;
                        data["spawnGroup"] = spawn.SpawnGroup;
                    }
                    break;

                case "Waypoint":
                    var wp = go.GetComponent<Waypoint>();
                    if (wp != null)
                    {
                        data["action"] = wp.Action.ToString();
                        data["waitTime"] = wp.WaitTime;
                        data["connectionCount"] = wp.Connections.Count(c => c != null);
                        data["waypointId"] = wp.WaypointId;
                        data["waypointGroup"] = wp.WaypointGroup;
                    }
                    break;

                case "TriggerVolume":
                    var trigger = go.GetComponent<TriggerVolume>();
                    if (trigger != null)
                    {
                        data["eventName"] = trigger.EventName;
                        data["mode"] = trigger.Mode.ToString();
                        data["isEnabled"] = trigger.IsEnabled;
                        data["triggerId"] = trigger.TriggerId;
                    }
                    break;

                case "Checkpoint":
                    var cp = go.GetComponent<Checkpoint>();
                    if (cp != null)
                    {
                        data["checkpointType"] = cp.Type.ToString();
                        data["sequenceOrder"] = cp.SequenceOrder;
                        data["isActive"] = cp.IsActive;
                        data["checkpointId"] = cp.CheckpointId;
                    }
                    break;

                case "ObjectiveMarker":
                    var obj = go.GetComponent<ObjectiveMarker>();
                    if (obj != null)
                    {
                        data["objectiveType"] = obj.Type.ToString();
                        data["displayName"] = obj.DisplayName;
                        data["state"] = obj.State.ToString();
                        data["pointValue"] = obj.PointValue;
                        data["objectiveId"] = obj.ObjectiveId;
                    }
                    break;
            }

            return data;
        }

        #endregion
    }
}
