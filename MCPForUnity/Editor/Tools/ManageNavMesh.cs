using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Manages Unity NavMesh for AI navigation support.
    /// Requires the com.unity.ai.navigation package for full functionality.
    /// </summary>
    [McpForUnityTool("manage_navmesh", AutoRegister = false,
        Description = "Manage Unity NavMesh for AI navigation. Actions: bake (bake NavMesh with agent settings), get_status (check NavMesh coverage), query_path (find path between points), check_reachable (verify position on NavMesh), add_obstacle (add NavMeshObstacle component), add_link (create NavMeshLink), set_area (mark surface as walkable/non-walkable), get_agent_settings (list agent types). Requires com.unity.ai.navigation package for baking.")]
    public static class ManageNavMesh
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: bake, get_status, query_path, check_reachable, add_obstacle, add_link, set_area, get_agent_settings, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_navmesh" }),
                    "bake" => BakeNavMesh(@params),
                    "get_status" => GetNavMeshStatus(@params),
                    "query_path" => QueryPath(@params),
                    "check_reachable" => CheckReachable(@params),
                    "add_obstacle" => AddObstacle(@params),
                    "add_link" => AddLink(@params),
                    "set_area" => SetArea(@params),
                    "get_agent_settings" => GetAgentSettings(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: bake, get_status, query_path, check_reachable, add_obstacle, add_link, set_area, get_agent_settings, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Bakes the NavMesh with configurable agent settings.
        /// </summary>
        private static object BakeNavMesh(JObject @params)
        {
            // Check if NavMeshSurface is available (requires com.unity.ai.navigation)
            Type navMeshSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");

            if (navMeshSurfaceType == null)
            {
                // Fall back to legacy NavMesh building
                return BakeNavMeshLegacy(@params);
            }

            float agentRadius = @params["agentRadius"]?.ToObject<float>() ?? 0.5f;
            float agentHeight = @params["agentHeight"]?.ToObject<float>() ?? 2.0f;
            float maxSlope = @params["maxSlope"]?.ToObject<float>() ?? 45f;
            float stepHeight = @params["stepHeight"]?.ToObject<float>() ?? 0.4f;
            int agentTypeId = @params["agentTypeId"]?.ToObject<int>() ?? 0;

            // Find or create NavMeshSurface
            var surfaces = UnityEngine.Object.FindObjectsByType(navMeshSurfaceType, FindObjectsSortMode.None);
            UnityEngine.Object surface = null;

            if (surfaces.Length == 0)
            {
                // Create a new NavMeshSurface on a new GameObject
                GameObject surfaceObj = new GameObject("NavMeshSurface");
                surface = surfaceObj.AddComponent(navMeshSurfaceType) as UnityEngine.Object;
                Undo.RegisterCreatedObjectUndo(surfaceObj, "Create NavMeshSurface");
            }
            else
            {
                surface = surfaces[0];
            }

            // Configure and build using reflection
            var agentTypeIdProp = navMeshSurfaceType.GetProperty("agentTypeID");
            if (agentTypeIdProp != null)
            {
                agentTypeIdProp.SetValue(surface, agentTypeId);
            }

            var buildMethod = navMeshSurfaceType.GetMethod("BuildNavMesh");
            if (buildMethod != null)
            {
                Undo.RecordObject(surface, "Bake NavMesh");
                buildMethod.Invoke(surface, null);
            }

            return new SuccessResponse("NavMesh baked successfully",
                new
                {
                    agentRadius = agentRadius,
                    agentHeight = agentHeight,
                    maxSlope = maxSlope,
                    stepHeight = stepHeight,
                    agentTypeId = agentTypeId,
                    method = "NavMeshSurface"
                });
        }

        private static object BakeNavMeshLegacy(JObject @params)
        {
            // Legacy NavMesh building (Unity's built-in)
            float agentRadius = @params["agentRadius"]?.ToObject<float>() ?? 0.5f;
            float agentHeight = @params["agentHeight"]?.ToObject<float>() ?? 2.0f;
            float maxSlope = @params["maxSlope"]?.ToObject<float>() ?? 45f;
            float stepHeight = @params["stepHeight"]?.ToObject<float>() ?? 0.4f;

            // Get NavMeshBuildSettings
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = agentHeight;
            settings.agentSlope = maxSlope;
            settings.agentClimb = stepHeight;

            // Collect all static geometry
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();

            // Find all MeshFilters on static objects
            var meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null && mf.gameObject.isStatic)
                {
                    var source = new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = mf.sharedMesh,
                        transform = mf.transform.localToWorldMatrix,
                        area = 0 // Walkable
                    };
                    sources.Add(source);
                }
            }

            // Find all Terrains
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                var source = new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Terrain,
                    sourceObject = terrain.terrainData,
                    transform = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one),
                    area = 0 // Walkable
                };
                sources.Add(source);
            }

            if (sources.Count == 0)
            {
                return new ErrorResponse("No static geometry found for NavMesh baking. Mark objects as Static in the Inspector.");
            }

            // Calculate bounds
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            // Build NavMesh
            NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);

            if (navMeshData != null)
            {
                NavMesh.RemoveAllNavMeshData();
                NavMesh.AddNavMeshData(navMeshData);

                return new SuccessResponse("NavMesh baked successfully (legacy method)",
                    new
                    {
                        agentRadius = agentRadius,
                        agentHeight = agentHeight,
                        maxSlope = maxSlope,
                        stepHeight = stepHeight,
                        sourceCount = sources.Count,
                        method = "Legacy"
                    });
            }

            return new ErrorResponse("Failed to build NavMesh");
        }

        /// <summary>
        /// Gets NavMesh status and coverage information.
        /// </summary>
        private static object GetNavMeshStatus(JObject @params)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            bool hasNavMesh = triangulation.vertices.Length > 0;

            if (!hasNavMesh)
            {
                return new SuccessResponse("No NavMesh data",
                    new
                    {
                        hasNavMesh = false,
                        triangleCount = 0,
                        vertexCount = 0
                    });
            }

            // Calculate bounds
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (var vertex in triangulation.vertices)
            {
                bounds.Encapsulate(vertex);
            }

            // Count areas
            var areaCounts = new Dictionary<int, int>();
            foreach (var area in triangulation.areas)
            {
                if (!areaCounts.ContainsKey(area))
                    areaCounts[area] = 0;
                areaCounts[area]++;
            }

            return new SuccessResponse("NavMesh status retrieved",
                new
                {
                    hasNavMesh = true,
                    triangleCount = triangulation.indices.Length / 3,
                    vertexCount = triangulation.vertices.Length,
                    bounds = new
                    {
                        center = new { x = bounds.center.x, y = bounds.center.y, z = bounds.center.z },
                        size = new { x = bounds.size.x, y = bounds.size.y, z = bounds.size.z }
                    },
                    areaCounts = areaCounts
                });
        }

        /// <summary>
        /// Queries a path between two points.
        /// </summary>
        private static object QueryPath(JObject @params)
        {
            Vector3 start = VectorParsing.ParseVector3OrDefault(@params["start"]);
            Vector3 end = VectorParsing.ParseVector3OrDefault(@params["end"]);
            int areaMask = @params["areaMask"]?.ToObject<int>() ?? NavMesh.AllAreas;

            NavMeshPath path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(start, end, areaMask, path);

            if (!found || path.status == NavMeshPathStatus.PathInvalid)
            {
                return new SuccessResponse("No path found",
                    new
                    {
                        found = false,
                        status = path.status.ToString(),
                        start = new { x = start.x, y = start.y, z = start.z },
                        end = new { x = end.x, y = end.y, z = end.z }
                    });
            }

            // Calculate total distance
            float totalDistance = 0f;
            var corners = path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                totalDistance += Vector3.Distance(corners[i - 1], corners[i]);
            }

            var waypoints = corners.Select(c => new { x = c.x, y = c.y, z = c.z }).ToList();

            return new SuccessResponse($"Path found with {corners.Length} waypoints",
                new
                {
                    found = true,
                    status = path.status.ToString(),
                    waypointCount = corners.Length,
                    totalDistance = totalDistance,
                    waypoints = waypoints
                });
        }

        /// <summary>
        /// Checks if a position is reachable (on NavMesh).
        /// </summary>
        private static object CheckReachable(JObject @params)
        {
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            float maxDistance = @params["maxDistance"]?.ToObject<float>() ?? 1f;
            int areaMask = @params["areaMask"]?.ToObject<int>() ?? NavMesh.AllAreas;

            bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, areaMask);

            return new SuccessResponse(found ? "Position is reachable" : "Position is not reachable",
                new
                {
                    reachable = found,
                    queriedPosition = new { x = position.x, y = position.y, z = position.z },
                    nearestPosition = found ? new { x = hit.position.x, y = hit.position.y, z = hit.position.z } : null,
                    distance = found ? hit.distance : -1f,
                    area = found ? hit.mask : -1
                });
        }

        /// <summary>
        /// Adds a NavMeshObstacle to a GameObject.
        /// </summary>
        private static object AddObstacle(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("Target is required. Provide GameObject name or instance ID.");
            }

            GameObject go = ResolveGameObject(target);
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            string shapeStr = @params["shape"]?.ToString()?.ToLowerInvariant() ?? "box";
            Vector3 size = VectorParsing.ParseVector3OrDefault(@params["size"], Vector3.one);
            float radius = @params["radius"]?.ToObject<float>() ?? 0.5f;
            float height = @params["height"]?.ToObject<float>() ?? 1f;
            bool carve = @params["carve"]?.ToObject<bool>() ?? true;

            NavMeshObstacle obstacle = go.GetComponent<NavMeshObstacle>();
            bool created = false;

            if (obstacle == null)
            {
                obstacle = Undo.AddComponent<NavMeshObstacle>(go);
                created = true;
            }

            Undo.RecordObject(obstacle, "Configure NavMeshObstacle");

            if (shapeStr == "capsule")
            {
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.radius = radius;
                obstacle.height = height;
            }
            else
            {
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = size;
            }

            obstacle.carving = carve;

            return new SuccessResponse($"{(created ? "Added" : "Configured")} NavMeshObstacle on '{go.name}'",
                new
                {
                    gameObject = go.name,
                    instanceId = go.GetInstanceID(),
                    shape = obstacle.shape.ToString(),
                    carving = obstacle.carving,
                    created = created
                });
        }

        /// <summary>
        /// Creates a NavMeshLink between two points.
        /// </summary>
        private static object AddLink(JObject @params)
        {
            Vector3 start = VectorParsing.ParseVector3OrDefault(@params["start"]);
            Vector3 end = VectorParsing.ParseVector3OrDefault(@params["end"]);
            float width = @params["width"]?.ToObject<float>() ?? 1f;
            bool bidirectional = @params["bidirectional"]?.ToObject<bool>() ?? true;
            int area = @params["area"]?.ToObject<int>() ?? 0;
            string name = @params["name"]?.ToString() ?? "NavMeshLink";

            // Check for NavMeshLink component (requires com.unity.ai.navigation)
            Type navMeshLinkType = Type.GetType("Unity.AI.Navigation.NavMeshLink, Unity.AI.Navigation");

            if (navMeshLinkType != null)
            {
                GameObject linkObj = new GameObject(name);
                linkObj.transform.position = (start + end) / 2f;

                var link = linkObj.AddComponent(navMeshLinkType);

                // Set properties using reflection
                var startPointProp = navMeshLinkType.GetProperty("startPoint");
                var endPointProp = navMeshLinkType.GetProperty("endPoint");
                var widthProp = navMeshLinkType.GetProperty("width");
                var bidirectionalProp = navMeshLinkType.GetProperty("bidirectional");
                var areaProp = navMeshLinkType.GetProperty("area");

                if (startPointProp != null) startPointProp.SetValue(link, start - linkObj.transform.position);
                if (endPointProp != null) endPointProp.SetValue(link, end - linkObj.transform.position);
                if (widthProp != null) widthProp.SetValue(link, width);
                if (bidirectionalProp != null) bidirectionalProp.SetValue(link, bidirectional);
                if (areaProp != null) areaProp.SetValue(link, area);

                Undo.RegisterCreatedObjectUndo(linkObj, $"Create NavMeshLink '{name}'");
                Selection.activeGameObject = linkObj;

                return new SuccessResponse($"Created NavMeshLink '{name}'",
                    new
                    {
                        name = name,
                        instanceId = linkObj.GetInstanceID(),
                        start = new { x = start.x, y = start.y, z = start.z },
                        end = new { x = end.x, y = end.y, z = end.z },
                        width = width,
                        bidirectional = bidirectional,
                        area = area
                    });
            }

            // Fall back to OffMeshLink if NavMeshLink not available
            GameObject legacyLinkObj = new GameObject(name);
            legacyLinkObj.transform.position = start;

            OffMeshLink offMeshLink = legacyLinkObj.AddComponent<OffMeshLink>();
            offMeshLink.startTransform = legacyLinkObj.transform;

            GameObject endObj = new GameObject($"{name}_End");
            endObj.transform.position = end;
            endObj.transform.SetParent(legacyLinkObj.transform, true);
            offMeshLink.endTransform = endObj.transform;

            offMeshLink.biDirectional = bidirectional;
            offMeshLink.area = area;

            Undo.RegisterCreatedObjectUndo(legacyLinkObj, $"Create OffMeshLink '{name}'");
            Selection.activeGameObject = legacyLinkObj;

            return new SuccessResponse($"Created OffMeshLink '{name}' (legacy)",
                new
                {
                    name = name,
                    instanceId = legacyLinkObj.GetInstanceID(),
                    start = new { x = start.x, y = start.y, z = start.z },
                    end = new { x = end.x, y = end.y, z = end.z },
                    bidirectional = bidirectional,
                    area = area,
                    method = "Legacy"
                });
        }

        /// <summary>
        /// Sets the NavMesh area for a surface.
        /// </summary>
        private static object SetArea(JObject @params)
        {
            string target = @params["target"]?.ToString();
            if (string.IsNullOrEmpty(target))
            {
                return new ErrorResponse("Target is required. Provide GameObject name or instance ID.");
            }

            GameObject go = ResolveGameObject(target);
            if (go == null)
            {
                return new ErrorResponse($"GameObject not found: {target}");
            }

            int area = @params["area"]?.ToObject<int>() ?? 0;
            string areaName = @params["areaName"]?.ToString();

            // If area name provided, look up the area index
            if (!string.IsNullOrEmpty(areaName))
            {
                int namedArea = NavMesh.GetAreaFromName(areaName);
                if (namedArea != -1)
                {
                    area = namedArea;
                }
                else
                {
                    return new ErrorResponse($"NavMesh area not found: {areaName}");
                }
            }

            // Check for NavMeshModifier (requires com.unity.ai.navigation)
            Type modifierType = Type.GetType("Unity.AI.Navigation.NavMeshModifier, Unity.AI.Navigation");

            if (modifierType != null)
            {
                var modifier = go.GetComponent(modifierType);
                bool created = false;

                if (modifier == null)
                {
                    modifier = go.AddComponent(modifierType);
                    created = true;
                }

                Undo.RecordObject(modifier as UnityEngine.Object, "Set NavMesh Area");

                var overrideAreaProp = modifierType.GetProperty("overrideArea");
                var areaProp = modifierType.GetProperty("area");

                if (overrideAreaProp != null) overrideAreaProp.SetValue(modifier, true);
                if (areaProp != null) areaProp.SetValue(modifier, area);

                return new SuccessResponse($"{(created ? "Added" : "Configured")} NavMeshModifier on '{go.name}'",
                    new
                    {
                        gameObject = go.name,
                        instanceId = go.GetInstanceID(),
                        area = area,
                        created = created
                    });
            }

            // If NavMeshModifier not available, mark object as navigation static
            GameObjectUtility.SetStaticEditorFlags(go,
                GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.NavigationStatic);

            return new SuccessResponse($"Marked '{go.name}' as Navigation Static (install com.unity.ai.navigation for area control)",
                new
                {
                    gameObject = go.name,
                    instanceId = go.GetInstanceID(),
                    navigationStatic = true
                });
        }

        /// <summary>
        /// Gets available NavMesh agent settings.
        /// </summary>
        private static object GetAgentSettings(JObject @params)
        {
            var agents = new List<object>();
            int count = NavMesh.GetSettingsCount();

            for (int i = 0; i < count; i++)
            {
                NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
                string agentName = NavMesh.GetSettingsNameFromID(settings.agentTypeID);

                agents.Add(new
                {
                    index = i,
                    agentTypeId = settings.agentTypeID,
                    name = agentName,
                    agentRadius = settings.agentRadius,
                    agentHeight = settings.agentHeight,
                    agentSlope = settings.agentSlope,
                    agentClimb = settings.agentClimb
                });
            }

            // Get area names
            var areas = new List<object>();
            for (int i = 0; i < 32; i++)
            {
                string areaName = NavMesh.GetAreaFromName(GetAreaNameByIndex(i)) >= 0 ? GetAreaNameByIndex(i) : null;
                int cost = NavMesh.GetAreaCost(i) > 0 ? (int)(NavMesh.GetAreaCost(i) * 100) : 0;

                if (!string.IsNullOrEmpty(areaName) || cost > 0)
                {
                    areas.Add(new
                    {
                        index = i,
                        name = areaName ?? $"Area {i}",
                        cost = cost
                    });
                }
            }

            return new SuccessResponse($"Found {count} agent types",
                new
                {
                    agentCount = count,
                    agents = agents,
                    areas = areas
                });
        }

        private static string GetAreaNameByIndex(int index)
        {
            // Built-in area names
            return index switch
            {
                0 => "Walkable",
                1 => "Not Walkable",
                2 => "Jump",
                _ => $"Area {index}"
            };
        }

        #region Helper Methods

        private static GameObject ResolveGameObject(string target)
        {
            // Try as instance ID
            if (int.TryParse(target, out int instanceId))
            {
                return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            }

            // Try as name
            return GameObject.Find(target);
        }

        #endregion
    }
}
