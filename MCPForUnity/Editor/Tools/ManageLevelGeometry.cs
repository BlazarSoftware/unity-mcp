using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Creates procedural greybox geometry for level design and prototyping.
    /// Supports corridors, rooms, stairs, ramps, platforms, arches, columns, and walls.
    /// </summary>
    [McpForUnityTool("manage_level_geometry", AutoRegister = false,
        Description = "Creates procedural greybox geometry for level design. Actions: create_corridor (hollow corridor with walls/floor/ceiling), create_room (enclosed room with optional doorways), create_stairs (staircase with configurable steps), create_ramp (angled ramp), create_platform (raised platform with optional railings), create_arch (archway/doorframe), create_column (cylindrical or rectangular column), create_wall (wall segment). All shapes create parent GameObjects with child primitives, auto-generate colliders, and support greybox materials.")]
    public static class ManageLevelGeometry
    {
        private const string DefaultGreyboxMaterialPath = "Default-Material";

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("Action is required. Valid actions: create_corridor, create_room, create_stairs, create_ramp, create_platform, create_arch, create_column, create_wall, ping");
            }

            try
            {
                return action switch
                {
                    "ping" => new SuccessResponse("pong", new { tool = "manage_level_geometry" }),
                    "create_corridor" => CreateCorridor(@params),
                    "create_room" => CreateRoom(@params),
                    "create_stairs" => CreateStairs(@params),
                    "create_ramp" => CreateRamp(@params),
                    "create_platform" => CreatePlatform(@params),
                    "create_arch" => CreateArch(@params),
                    "create_column" => CreateColumn(@params),
                    "create_wall" => CreateWall(@params),
                    _ => new ErrorResponse($"Unknown action: {action}. Valid actions: create_corridor, create_room, create_stairs, create_ramp, create_platform, create_arch, create_column, create_wall, ping")
                };
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Creates a hollow corridor with walls, floor, and ceiling.
        /// </summary>
        private static object CreateCorridor(JObject @params)
        {
            float length = @params["length"]?.ToObject<float>() ?? 10f;
            float width = @params["width"]?.ToObject<float>() ?? 4f;
            float height = @params["height"]?.ToObject<float>() ?? 3f;
            float wallThickness = @params["wallThickness"]?.ToObject<float>() ?? 0.2f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Corridor";
            bool openEnds = @params["openEnds"]?.ToObject<bool>() ?? true;

            GameObject corridor = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(corridor, $"Create Corridor '{name}'");

            corridor.transform.position = position;
            corridor.transform.eulerAngles = rotation;

            // Floor
            CreateBoxChild(corridor, "Floor",
                new Vector3(0, -wallThickness / 2, 0),
                new Vector3(length, wallThickness, width));

            // Ceiling
            CreateBoxChild(corridor, "Ceiling",
                new Vector3(0, height + wallThickness / 2, 0),
                new Vector3(length, wallThickness, width));

            // Left Wall
            CreateBoxChild(corridor, "LeftWall",
                new Vector3(0, height / 2, -width / 2 - wallThickness / 2),
                new Vector3(length, height, wallThickness));

            // Right Wall
            CreateBoxChild(corridor, "RightWall",
                new Vector3(0, height / 2, width / 2 + wallThickness / 2),
                new Vector3(length, height, wallThickness));

            // End walls (if not open)
            if (!openEnds)
            {
                CreateBoxChild(corridor, "FrontWall",
                    new Vector3(length / 2 + wallThickness / 2, height / 2, 0),
                    new Vector3(wallThickness, height, width));
                CreateBoxChild(corridor, "BackWall",
                    new Vector3(-length / 2 - wallThickness / 2, height / 2, 0),
                    new Vector3(wallThickness, height, width));
            }

            ApplyMaterial(corridor, @params["material"]?.ToString());
            Selection.activeGameObject = corridor;

            return new SuccessResponse($"Created corridor '{name}' ({length}x{width}x{height})",
                GetGeometryData(corridor));
        }

        /// <summary>
        /// Creates an enclosed room with optional doorways.
        /// </summary>
        private static object CreateRoom(JObject @params)
        {
            float width = @params["width"]?.ToObject<float>() ?? 6f;
            float depth = @params["depth"]?.ToObject<float>() ?? 6f;
            float height = @params["height"]?.ToObject<float>() ?? 3f;
            float wallThickness = @params["wallThickness"]?.ToObject<float>() ?? 0.2f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Room";

            // Doorway configuration
            JArray doorways = @params["doorways"] as JArray;

            GameObject room = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(room, $"Create Room '{name}'");

            room.transform.position = position;
            room.transform.eulerAngles = rotation;

            // Floor
            CreateBoxChild(room, "Floor",
                new Vector3(0, -wallThickness / 2, 0),
                new Vector3(width, wallThickness, depth));

            // Ceiling
            CreateBoxChild(room, "Ceiling",
                new Vector3(0, height + wallThickness / 2, 0),
                new Vector3(width, wallThickness, depth));

            // Walls - check for doorways
            float doorWidth = 1.2f;
            float doorHeight = 2.2f;

            // Parse doorway positions
            HashSet<string> doorwayWalls = new HashSet<string>();
            if (doorways != null)
            {
                foreach (var dw in doorways)
                {
                    string wall = dw.ToString().ToLowerInvariant();
                    doorwayWalls.Add(wall);
                }
            }

            // North Wall (positive Z)
            if (doorwayWalls.Contains("north"))
            {
                CreateWallWithDoorway(room, "NorthWall",
                    new Vector3(0, 0, depth / 2 + wallThickness / 2),
                    width, height, wallThickness, doorWidth, doorHeight, true);
            }
            else
            {
                CreateBoxChild(room, "NorthWall",
                    new Vector3(0, height / 2, depth / 2 + wallThickness / 2),
                    new Vector3(width, height, wallThickness));
            }

            // South Wall (negative Z)
            if (doorwayWalls.Contains("south"))
            {
                CreateWallWithDoorway(room, "SouthWall",
                    new Vector3(0, 0, -depth / 2 - wallThickness / 2),
                    width, height, wallThickness, doorWidth, doorHeight, true);
            }
            else
            {
                CreateBoxChild(room, "SouthWall",
                    new Vector3(0, height / 2, -depth / 2 - wallThickness / 2),
                    new Vector3(width, height, wallThickness));
            }

            // East Wall (positive X)
            if (doorwayWalls.Contains("east"))
            {
                CreateWallWithDoorway(room, "EastWall",
                    new Vector3(width / 2 + wallThickness / 2, 0, 0),
                    depth, height, wallThickness, doorWidth, doorHeight, false);
            }
            else
            {
                CreateBoxChild(room, "EastWall",
                    new Vector3(width / 2 + wallThickness / 2, height / 2, 0),
                    new Vector3(wallThickness, height, depth));
            }

            // West Wall (negative X)
            if (doorwayWalls.Contains("west"))
            {
                CreateWallWithDoorway(room, "WestWall",
                    new Vector3(-width / 2 - wallThickness / 2, 0, 0),
                    depth, height, wallThickness, doorWidth, doorHeight, false);
            }
            else
            {
                CreateBoxChild(room, "WestWall",
                    new Vector3(-width / 2 - wallThickness / 2, height / 2, 0),
                    new Vector3(wallThickness, height, depth));
            }

            ApplyMaterial(room, @params["material"]?.ToString());
            Selection.activeGameObject = room;

            return new SuccessResponse($"Created room '{name}' ({width}x{depth}x{height})",
                GetGeometryData(room));
        }

        /// <summary>
        /// Creates a wall segment with a doorway.
        /// </summary>
        private static void CreateWallWithDoorway(GameObject parent, string name, Vector3 basePosition,
            float wallLength, float wallHeight, float thickness, float doorWidth, float doorHeight, bool alongX)
        {
            GameObject wallParent = new GameObject(name);
            wallParent.transform.SetParent(parent.transform, false);
            wallParent.transform.localPosition = basePosition;

            float sideWidth = (wallLength - doorWidth) / 2;

            if (alongX)
            {
                // Left section
                CreateBoxChild(wallParent, "Left",
                    new Vector3(-wallLength / 2 + sideWidth / 2, wallHeight / 2, 0),
                    new Vector3(sideWidth, wallHeight, thickness));
                // Right section
                CreateBoxChild(wallParent, "Right",
                    new Vector3(wallLength / 2 - sideWidth / 2, wallHeight / 2, 0),
                    new Vector3(sideWidth, wallHeight, thickness));
                // Top section (above door)
                CreateBoxChild(wallParent, "Top",
                    new Vector3(0, doorHeight + (wallHeight - doorHeight) / 2, 0),
                    new Vector3(doorWidth, wallHeight - doorHeight, thickness));
            }
            else
            {
                // Front section
                CreateBoxChild(wallParent, "Front",
                    new Vector3(0, wallHeight / 2, -wallLength / 2 + sideWidth / 2),
                    new Vector3(thickness, wallHeight, sideWidth));
                // Back section
                CreateBoxChild(wallParent, "Back",
                    new Vector3(0, wallHeight / 2, wallLength / 2 - sideWidth / 2),
                    new Vector3(thickness, wallHeight, sideWidth));
                // Top section (above door)
                CreateBoxChild(wallParent, "Top",
                    new Vector3(0, doorHeight + (wallHeight - doorHeight) / 2, 0),
                    new Vector3(thickness, wallHeight - doorHeight, doorWidth));
            }
        }

        /// <summary>
        /// Creates a staircase with configurable steps.
        /// </summary>
        private static object CreateStairs(JObject @params)
        {
            int stepCount = @params["stepCount"]?.ToObject<int>() ?? 10;
            float stepWidth = @params["stepWidth"]?.ToObject<float>() ?? 2f;
            float stepHeight = @params["stepHeight"]?.ToObject<float>() ?? 0.2f;
            float stepDepth = @params["stepDepth"]?.ToObject<float>() ?? 0.3f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Stairs";
            bool addRailings = @params["addRailings"]?.ToObject<bool>() ?? false;

            stepCount = Mathf.Clamp(stepCount, 1, 100);

            GameObject stairs = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(stairs, $"Create Stairs '{name}'");

            stairs.transform.position = position;
            stairs.transform.eulerAngles = rotation;

            // Create steps
            for (int i = 0; i < stepCount; i++)
            {
                CreateBoxChild(stairs, $"Step_{i}",
                    new Vector3(i * stepDepth + stepDepth / 2, i * stepHeight + stepHeight / 2, 0),
                    new Vector3(stepDepth, stepHeight, stepWidth));
            }

            // Add railings if requested
            if (addRailings)
            {
                float railingHeight = 1f;
                float railingThickness = 0.05f;
                float totalLength = stepCount * stepDepth;
                float totalHeight = stepCount * stepHeight;
                float angle = Mathf.Atan2(totalHeight, totalLength) * Mathf.Rad2Deg;
                float railingLength = Mathf.Sqrt(totalLength * totalLength + totalHeight * totalHeight);

                // Left railing
                var leftRailing = CreateBoxChild(stairs, "LeftRailing",
                    new Vector3(totalLength / 2, totalHeight / 2 + railingHeight, -stepWidth / 2 - railingThickness),
                    new Vector3(railingLength, railingThickness, railingThickness));
                leftRailing.transform.localRotation = Quaternion.Euler(0, 0, angle);

                // Right railing
                var rightRailing = CreateBoxChild(stairs, "RightRailing",
                    new Vector3(totalLength / 2, totalHeight / 2 + railingHeight, stepWidth / 2 + railingThickness),
                    new Vector3(railingLength, railingThickness, railingThickness));
                rightRailing.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            ApplyMaterial(stairs, @params["material"]?.ToString());
            Selection.activeGameObject = stairs;

            float totalStairHeight = stepCount * stepHeight;
            float totalStairLength = stepCount * stepDepth;

            return new SuccessResponse($"Created stairs '{name}' with {stepCount} steps (total height: {totalStairHeight:F1}, length: {totalStairLength:F1})",
                GetGeometryData(stairs));
        }

        /// <summary>
        /// Creates an angled ramp.
        /// </summary>
        private static object CreateRamp(JObject @params)
        {
            float length = @params["length"]?.ToObject<float>() ?? 5f;
            float width = @params["width"]?.ToObject<float>() ?? 2f;
            float height = @params["height"]?.ToObject<float>() ?? 2f;
            float thickness = @params["thickness"]?.ToObject<float>() ?? 0.2f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Ramp";
            bool addSides = @params["addSides"]?.ToObject<bool>() ?? false;

            GameObject ramp = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(ramp, $"Create Ramp '{name}'");

            ramp.transform.position = position;
            ramp.transform.eulerAngles = rotation;

            // Calculate ramp angle
            float angle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            float rampLength = Mathf.Sqrt(length * length + height * height);

            // Ramp surface
            var surface = CreateBoxChild(ramp, "Surface",
                new Vector3(length / 2, height / 2, 0),
                new Vector3(rampLength, thickness, width));
            surface.transform.localRotation = Quaternion.Euler(0, 0, angle);

            // Add sides if requested
            if (addSides)
            {
                // Create triangular side walls using multiple boxes (approximation)
                CreateRampSide(ramp, "LeftSide", length, height, thickness, -width / 2 - thickness / 2);
                CreateRampSide(ramp, "RightSide", length, height, thickness, width / 2 + thickness / 2);
            }

            ApplyMaterial(ramp, @params["material"]?.ToString());
            Selection.activeGameObject = ramp;

            return new SuccessResponse($"Created ramp '{name}' ({length}x{width}, height: {height}, angle: {angle:F1}°)",
                GetGeometryData(ramp));
        }

        private static void CreateRampSide(GameObject parent, string name, float length, float height, float thickness, float zOffset)
        {
            GameObject side = new GameObject(name);
            side.transform.SetParent(parent.transform, false);

            // Approximate triangle with boxes
            int segments = 5;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float segLength = length / segments;
                float segHeight = height * (1 - t);
                float xPos = t * length + segLength / 2;
                float yPos = segHeight / 2;

                CreateBoxChild(side, $"Segment_{i}",
                    new Vector3(xPos, yPos, zOffset),
                    new Vector3(segLength, segHeight, thickness));
            }
        }

        /// <summary>
        /// Creates a raised platform with optional railings.
        /// </summary>
        private static object CreatePlatform(JObject @params)
        {
            float width = @params["width"]?.ToObject<float>() ?? 4f;
            float depth = @params["depth"]?.ToObject<float>() ?? 4f;
            float height = @params["height"]?.ToObject<float>() ?? 1f;
            float thickness = @params["thickness"]?.ToObject<float>() ?? 0.2f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Platform";
            bool addRailings = @params["addRailings"]?.ToObject<bool>() ?? false;
            float railingHeight = @params["railingHeight"]?.ToObject<float>() ?? 1f;

            GameObject platform = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(platform, $"Create Platform '{name}'");

            platform.transform.position = position;
            platform.transform.eulerAngles = rotation;

            // Platform surface
            CreateBoxChild(platform, "Surface",
                new Vector3(0, height, 0),
                new Vector3(width, thickness, depth));

            // Support structure (optional simple pillars at corners)
            if (height > thickness)
            {
                float pillarSize = 0.3f;
                float pillarHeight = height - thickness / 2;

                CreateBoxChild(platform, "Pillar_FL",
                    new Vector3(-width / 2 + pillarSize / 2, pillarHeight / 2, -depth / 2 + pillarSize / 2),
                    new Vector3(pillarSize, pillarHeight, pillarSize));
                CreateBoxChild(platform, "Pillar_FR",
                    new Vector3(width / 2 - pillarSize / 2, pillarHeight / 2, -depth / 2 + pillarSize / 2),
                    new Vector3(pillarSize, pillarHeight, pillarSize));
                CreateBoxChild(platform, "Pillar_BL",
                    new Vector3(-width / 2 + pillarSize / 2, pillarHeight / 2, depth / 2 - pillarSize / 2),
                    new Vector3(pillarSize, pillarHeight, pillarSize));
                CreateBoxChild(platform, "Pillar_BR",
                    new Vector3(width / 2 - pillarSize / 2, pillarHeight / 2, depth / 2 - pillarSize / 2),
                    new Vector3(pillarSize, pillarHeight, pillarSize));
            }

            // Add railings if requested
            if (addRailings)
            {
                float railThickness = 0.05f;
                float railY = height + thickness / 2 + railingHeight;

                // Top rails
                CreateBoxChild(platform, "Rail_North",
                    new Vector3(0, railY, depth / 2),
                    new Vector3(width, railThickness, railThickness));
                CreateBoxChild(platform, "Rail_South",
                    new Vector3(0, railY, -depth / 2),
                    new Vector3(width, railThickness, railThickness));
                CreateBoxChild(platform, "Rail_East",
                    new Vector3(width / 2, railY, 0),
                    new Vector3(railThickness, railThickness, depth));
                CreateBoxChild(platform, "Rail_West",
                    new Vector3(-width / 2, railY, 0),
                    new Vector3(railThickness, railThickness, depth));

                // Vertical posts
                float postY = height + thickness / 2 + railingHeight / 2;
                CreateBoxChild(platform, "Post_NE",
                    new Vector3(width / 2, postY, depth / 2),
                    new Vector3(railThickness, railingHeight, railThickness));
                CreateBoxChild(platform, "Post_NW",
                    new Vector3(-width / 2, postY, depth / 2),
                    new Vector3(railThickness, railingHeight, railThickness));
                CreateBoxChild(platform, "Post_SE",
                    new Vector3(width / 2, postY, -depth / 2),
                    new Vector3(railThickness, railingHeight, railThickness));
                CreateBoxChild(platform, "Post_SW",
                    new Vector3(-width / 2, postY, -depth / 2),
                    new Vector3(railThickness, railingHeight, railThickness));
            }

            ApplyMaterial(platform, @params["material"]?.ToString());
            Selection.activeGameObject = platform;

            return new SuccessResponse($"Created platform '{name}' ({width}x{depth}, height: {height})",
                GetGeometryData(platform));
        }

        /// <summary>
        /// Creates an archway or doorframe.
        /// </summary>
        private static object CreateArch(JObject @params)
        {
            float width = @params["width"]?.ToObject<float>() ?? 2f;
            float height = @params["height"]?.ToObject<float>() ?? 3f;
            float depth = @params["depth"]?.ToObject<float>() ?? 0.5f;
            float thickness = @params["thickness"]?.ToObject<float>() ?? 0.3f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Arch";
            bool rounded = @params["rounded"]?.ToObject<bool>() ?? false;
            int archSegments = @params["archSegments"]?.ToObject<int>() ?? 8;

            GameObject arch = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(arch, $"Create Arch '{name}'");

            arch.transform.position = position;
            arch.transform.eulerAngles = rotation;

            float openingWidth = width - thickness * 2;
            float pillarHeight = height - thickness;

            // Left pillar
            CreateBoxChild(arch, "LeftPillar",
                new Vector3(-width / 2 + thickness / 2, pillarHeight / 2, 0),
                new Vector3(thickness, pillarHeight, depth));

            // Right pillar
            CreateBoxChild(arch, "RightPillar",
                new Vector3(width / 2 - thickness / 2, pillarHeight / 2, 0),
                new Vector3(thickness, pillarHeight, depth));

            if (rounded)
            {
                // Create rounded arch with segments
                float radius = openingWidth / 2;
                archSegments = Mathf.Clamp(archSegments, 4, 32);

                for (int i = 0; i < archSegments; i++)
                {
                    float angle1 = (float)i / archSegments * Mathf.PI;
                    float angle2 = (float)(i + 1) / archSegments * Mathf.PI;
                    float midAngle = (angle1 + angle2) / 2;

                    float x = -Mathf.Cos(midAngle) * radius;
                    float y = pillarHeight + Mathf.Sin(midAngle) * radius;

                    float segmentLength = 2 * radius * Mathf.Sin((angle2 - angle1) / 2);
                    float segmentAngle = midAngle * Mathf.Rad2Deg - 90;

                    var segment = CreateBoxChild(arch, $"ArchSegment_{i}",
                        new Vector3(x, y, 0),
                        new Vector3(segmentLength, thickness, depth));
                    segment.transform.localRotation = Quaternion.Euler(0, 0, segmentAngle);
                }
            }
            else
            {
                // Simple rectangular top
                CreateBoxChild(arch, "Top",
                    new Vector3(0, height - thickness / 2, 0),
                    new Vector3(width, thickness, depth));
            }

            ApplyMaterial(arch, @params["material"]?.ToString());
            Selection.activeGameObject = arch;

            return new SuccessResponse($"Created arch '{name}' ({width}x{height}, rounded: {rounded})",
                GetGeometryData(arch));
        }

        /// <summary>
        /// Creates a column (cylindrical or rectangular).
        /// </summary>
        private static object CreateColumn(JObject @params)
        {
            float height = @params["height"]?.ToObject<float>() ?? 3f;
            float radius = @params["radius"]?.ToObject<float>() ?? 0.3f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Column";
            string shape = @params["shape"]?.ToString()?.ToLowerInvariant() ?? "cylinder";
            bool addCapital = @params["addCapital"]?.ToObject<bool>() ?? false;
            bool addBase = @params["addBase"]?.ToObject<bool>() ?? false;

            GameObject column = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(column, $"Create Column '{name}'");

            column.transform.position = position;
            column.transform.eulerAngles = rotation;

            float shaftHeight = height;
            float baseHeight = 0f;
            float capitalHeight = 0f;

            if (addBase)
            {
                baseHeight = radius * 0.5f;
                shaftHeight -= baseHeight;
            }
            if (addCapital)
            {
                capitalHeight = radius * 0.5f;
                shaftHeight -= capitalHeight;
            }

            float shaftY = baseHeight + shaftHeight / 2;

            // Main shaft
            if (shape == "cylinder")
            {
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Shaft";
                shaft.transform.SetParent(column.transform, false);
                shaft.transform.localPosition = new Vector3(0, shaftY, 0);
                shaft.transform.localScale = new Vector3(radius * 2, shaftHeight / 2, radius * 2);
                Undo.RegisterCreatedObjectUndo(shaft, "Create Column Shaft");
            }
            else
            {
                CreateBoxChild(column, "Shaft",
                    new Vector3(0, shaftY, 0),
                    new Vector3(radius * 2, shaftHeight, radius * 2));
            }

            // Base
            if (addBase)
            {
                CreateBoxChild(column, "Base",
                    new Vector3(0, baseHeight / 2, 0),
                    new Vector3(radius * 2.5f, baseHeight, radius * 2.5f));
            }

            // Capital
            if (addCapital)
            {
                CreateBoxChild(column, "Capital",
                    new Vector3(0, height - capitalHeight / 2, 0),
                    new Vector3(radius * 2.5f, capitalHeight, radius * 2.5f));
            }

            ApplyMaterial(column, @params["material"]?.ToString());
            Selection.activeGameObject = column;

            return new SuccessResponse($"Created {shape} column '{name}' (height: {height}, radius: {radius})",
                GetGeometryData(column));
        }

        /// <summary>
        /// Creates a wall segment.
        /// </summary>
        private static object CreateWall(JObject @params)
        {
            float length = @params["length"]?.ToObject<float>() ?? 5f;
            float height = @params["height"]?.ToObject<float>() ?? 3f;
            float thickness = @params["thickness"]?.ToObject<float>() ?? 0.2f;
            Vector3 position = VectorParsing.ParseVector3OrDefault(@params["position"]);
            Vector3 rotation = VectorParsing.ParseVector3OrDefault(@params["rotation"]);
            string name = @params["name"]?.ToString() ?? "Wall";

            GameObject wall = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(wall, $"Create Wall '{name}'");

            wall.transform.position = position;
            wall.transform.eulerAngles = rotation;

            CreateBoxChild(wall, "WallSegment",
                new Vector3(0, height / 2, 0),
                new Vector3(length, height, thickness));

            ApplyMaterial(wall, @params["material"]?.ToString());
            Selection.activeGameObject = wall;

            return new SuccessResponse($"Created wall '{name}' ({length}x{height}x{thickness})",
                GetGeometryData(wall));
        }

        #region Helper Methods

        private static GameObject CreateBoxChild(GameObject parent, string name, Vector3 localPosition, Vector3 size)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            Undo.RegisterCreatedObjectUndo(box, $"Create {name}");
            return box;
        }

        private static void ApplyMaterial(GameObject parent, string materialPath)
        {
            if (string.IsNullOrEmpty(materialPath))
                return;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                // Try to find material by name
                string[] guids = AssetDatabase.FindAssets($"t:Material {materialPath}");
                if (guids.Length > 0)
                {
                    mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (mat != null)
            {
                var renderers = parent.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = mat;
                }
            }
        }

        private static object GetGeometryData(GameObject go)
        {
            var children = new List<object>();
            foreach (Transform child in go.transform)
            {
                children.Add(new
                {
                    name = child.name,
                    position = new { x = child.localPosition.x, y = child.localPosition.y, z = child.localPosition.z },
                    scale = new { x = child.localScale.x, y = child.localScale.y, z = child.localScale.z }
                });
            }

            return new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                childCount = go.transform.childCount,
                children = children
            };
        }

        #endregion
    }
}
